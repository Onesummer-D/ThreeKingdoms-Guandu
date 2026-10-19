const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const http = require('node:http');
const { once } = require('node:events');

async function main() {
  // Headers arrive immediately but the model body never ends: timeout must cover both.
  const model = http.createServer((req, res) => {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.write('{');
  });
  model.listen(0, '127.0.0.1');
  await once(model, 'listening');
  const child = spawn(process.execPath, ['server.js'], {
    cwd: __dirname,
    env: { ...process.env, PORT: '0', OPENAI_API_KEY: 'test-only',
      OPENAI_API_URL: `http://127.0.0.1:${model.address().port}`, OPENAI_TIMEOUT_MS: '500' },
    stdio: ['ignore', 'pipe', 'pipe']
  });
  let output = '';
  let count = 0;
  const check = (value, message) => { assert.ok(value, message); count++; console.log(`PASS ${message}`); };
  try {
    const port = await new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error('startup timeout')), 5000);
      child.stdout.on('data', data => {
        output += data;
        const match = output.match(/listening on (\d+)/);
        if (match) { clearTimeout(timer); resolve(Number(match[1])); }
      });
      child.once('error', reject);
    });
    const base = `http://127.0.0.1:${port}`;
    async function request(path, method = 'GET', body, headers = {}) {
      const r = await fetch(base + path, { method, headers: { 'Content-Type': 'application/json', ...headers },
        body: body === undefined ? undefined : typeof body === 'string' ? body : JSON.stringify(body), signal: AbortSignal.timeout(4000) });
      return { status: r.status, headers: r.headers, body: await r.json() };
    }
    const health = await request('/healthz');
    check(health.status === 200, 'health');
    check(health.headers.get('x-content-type-options') === 'nosniff' &&
      health.headers.get('referrer-policy') === 'no-referrer' &&
      health.headers.has('content-security-policy'), 'security response headers');
    const created = await request('/api/sessions', 'POST', '\uFEFF' + JSON.stringify({ options: ['A', 'B'], question: 'test' }));
    check(created.status === 201, 'BOM JSON accepted');
    const session = created.body;
    const path = '/api/sessions/' + session.sessionId;
    const host = { 'X-Host-Token': session.hostToken };
    const guest = { 'X-Join-Token': new URL(session.joinUrl).searchParams.get('token') };
    const publicInvite = await fetch(base + '/join/' + encodeURIComponent(session.sessionId) +
      '?token=' + encodeURIComponent(guest['X-Join-Token']));
    const publicHtml = await publicInvite.text();
    check(publicInvite.status === 200 && publicHtml.includes('class="scroll-paper"') &&
      publicHtml.includes('献策已呈') && !publicHtml.includes('__GAME_ENTRY_URL__'),
      'scroll H5 page served with all game config placeholders replaced');
    check(!publicHtml.includes('const joinToken = __JOIN_TOKEN__'),
      'invited token is injected as a JavaScript literal');
    const maliciousToken = await fetch(base + '/join/' + encodeURIComponent(session.sessionId) +
      '?token=' + encodeURIComponent('</script><script>alert(1)</script>'));
    const maliciousHtml = await maliciousToken.text();
    check(maliciousHtml.includes('\\u003c/script>') &&
      !maliciousHtml.includes('</script><script>alert(1)</script>'),
      'HTML-breaking token is escaped in inline JavaScript');
    check((await request(path)).status === 403, 'unauthorized host rejected');
    const suggestion = { guestLabel: 'test', optionIndex: 0, clientRequestId: 'retry-1' };
    check((await request(path + '/suggestions', 'POST', suggestion, guest)).status === 201, 'guest suggestion');
    const retry = await request(path + '/suggestions', 'POST', suggestion, guest);
    check(retry.status === 200 && retry.body.duplicate === true, 'idempotent retry');
    check((await request(path + '/suggestions', 'POST', { ...suggestion, clientRequestId: 'other' }, guest)).status === 409, 'second suggestion rejected');
    const polled = await request(path + '/suggestions', 'GET', undefined, host);
    check(polled.body.suggestions.length === 1, 'host receives exactly one suggestion');
    check((await request(path + '/decision', 'POST', { accepted: true }, host)).body.state === 'accepted', 'host decision');
    await request(path, 'DELETE', {}, host);
    check((await request(path, 'GET', undefined, host)).body.error === 'session_expired', 'closed session rejected');
    const started = Date.now();
    const answer = await request('/api/historian/ask', 'POST', { question: '乌巢为什么重要？' });
    // Citation IDs remain in structured evidence; the player-facing answer
    // deliberately strips [Hxx]/[Gxx]/[Dxx] markers after validation.
    const sources = answer.body.citations || [];
    check(answer.body.mode === 'grounded_fallback' &&
      Date.now() - started < 3000 &&
      typeof answer.body.answer === 'string' && answer.body.answer.trim().length > 0 &&
      sources.length > 0 && sources.every(source => /^H[0-9]{2}$/.test(source.id) &&
        typeof source.title === 'string' && source.title.length > 0) &&
      !/\[[A-Z][0-9]{2}\]/.test(answer.body.answer),
      'stalled model body times out with structured citations and clean player prose');
    console.log(`integration: ${count}/${count}`);
  } finally {
    child.kill();
    model.closeAllConnections();
    model.close();
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });


