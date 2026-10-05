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
      return { status: r.status, body: await r.json() };
    }
    check((await request('/healthz')).status === 200, 'health');
    const created = await request('/api/sessions', 'POST', '\uFEFF' + JSON.stringify({ options: ['A', 'B'], question: 'test' }));
    check(created.status === 201, 'BOM JSON accepted');
    const session = created.body;
    const path = '/api/sessions/' + session.sessionId;
    const host = { 'X-Host-Token': session.hostToken };
    const guest = { 'X-Join-Token': new URL(session.joinUrl).searchParams.get('token') };
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
    check(answer.body.mode === 'grounded_fallback' && Date.now() - started < 3000 && answer.body.answer.includes('[H'), 'stalled model body times out with cited fallback');
    console.log(`integration: ${count}/${count}`);
  } finally {
    child.kill();
    model.closeAllConnections();
    model.close();
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
