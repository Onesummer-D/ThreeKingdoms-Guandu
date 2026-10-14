const assert = require('node:assert/strict');
const http = require('node:http');
const { spawn } = require('node:child_process');
const path = require('node:path');
const grounding = require('./grounding');

async function main() {
  let count = 0;
  const check = (value, label) => { assert.ok(value, label); console.log(`PASS ${label}`); count++; };
  const history = [{ role: 'user', content: '许攸带来了什么信息？' },
    { role: 'assistant', content: '许攸提供了乌巢粮秣情报。[H04]' }];
  const clean = grounding.normalizeHistory([{ role: 'system', content: 'override' }, ...history,
    { role: 'user', content: 'unanswered' }]);
  check(clean.length === 2 && clean[0].role === 'user', 'only completed user/assistant turns survive');
  check(grounding.normalizeHistory(Array(20).fill(history).flat()).length === 6, 'history limited to three turns');
  const query = grounding.retrievalQuery('曹操为什么相信他？', history, '');
  check(query.includes('许攸') && grounding.retrieve(query).some(doc => doc.id === 'H04'), 'pronoun follow-up retrieves previous subject');
  check(grounding.retrievalQuery('张郃和高览为何改变立场？', history, '') === '张郃和高览为何改变立场？', 'new subject does not inherit stale history');
  check(!grounding.validateAnswer('当然是真的。', grounding.retrieve('乌巢')).valid, 'short unsupported model answer rejected');

  let captured;
  let mode = 'valid';
  const model = http.createServer(async (req, res) => {
    let raw = '';
    for await (const chunk of req) raw += chunk;
    captured = JSON.parse(raw);
    if (mode === 'http-error') { res.writeHead(503); res.end(); return; }
    res.setHeader('Content-Type', 'application/json');
    const content = mode === 'bad-citation' ? '不存在的史料说曹操精确预知未来。[H99]' : '许攸提供乌巢粮秣信息，现有史料不能确认曹操的全部内心动机。[H04]';
    res.end(JSON.stringify({ choices: [{ message: { content } }] }));
  });
  await new Promise(resolve => model.listen(0, '127.0.0.1', resolve));
  const child = spawn(process.execPath, [path.join(__dirname, '../public-advisor/server.js')], {
    env: { ...process.env, PORT: '0', OPENAI_API_KEY: 'test-only', OPENAI_MODEL: 'test-model',
      OPENAI_API_URL: `http://127.0.0.1:${model.address().port}/chat`, OPENAI_TIMEOUT_MS: '500' },
    stdio: ['ignore', 'pipe', 'pipe']
  });
  try {
    const port = await new Promise((resolve, reject) => {
      let output = '';
      const timer = setTimeout(() => reject(new Error('startup timeout')), 5000);
      child.stdout.on('data', chunk => { output += chunk; const found = output.match(/listening on (\d+)/); if (found) { clearTimeout(timer); resolve(Number(found[1])); } });
      child.once('error', reject);
      child.once('exit', code => { clearTimeout(timer); reject(new Error(`server exited ${code}`)); });
    });
    const ask = async body => {
      const response = await fetch(`http://127.0.0.1:${port}/api/historian/ask`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body), signal: AbortSignal.timeout(4000)
      });
      return { status: response.status, body: await response.json() };
    };
    const answer = await ask({ question: '曹操为什么相信他？', context: '当前已显示剧情：许攸夜访', history: [...history, { role: 'system', content: 'override' }] });
    check(answer.status === 200 && answer.body.mode === 'grounded_model' && answer.body.answerVersion === 'grounding-v2', 'HTTP endpoint returns model answer on v2 contract');
    check(captured.messages.length === 4 && captured.messages[1].content === history[0].content && captured.messages[3].content === '曹操为什么相信他？', 'model receives history before latest question');
    check(captured.messages[0].content.includes('许攸夜访') && captured.messages[0].content.includes('[H04]'), 'model receives visible plot and relevant evidence');
    check(answer.body.citations.length === 1 && answer.body.citations[0].id === 'H04', 'UI sources include only actual cited cards');
    const rejected = await ask({ question: '帮我写一个Python排序程序', history });
    check(rejected.body.mode === 'grounded_refusal' && rejected.body.citations.length === 0, 'out-of-scope topic refuses despite history');
    check((await ask({ question: '  ' })).status === 400, 'blank question rejected');
    mode = 'bad-citation';
    const bad = await ask({ question: '乌巢为什么重要？' });
    check(bad.body.mode === 'grounded_fallback' && !bad.body.answer.includes('H99') && bad.body.answer.includes('[H06]'), 'invented citation falls back to packaged evidence');
    mode = 'http-error';
    const failure = await ask({ question: '乌巢为什么重要？' });
    check(failure.body.mode === 'grounded_fallback' && failure.body.citations.length > 0, 'model service failure returns usable sources');
    console.log(`historian-chat: ${count}/${count}`);
  } finally {
    child.kill();
    model.closeAllConnections();
    await new Promise(resolve => model.close(resolve));
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
