const assert = require('node:assert/strict');
const http = require('node:http');
const { spawn } = require('node:child_process');
const path = require('node:path');
const grounding = require('./grounding');

async function main() {
  let count = 0;
  const check = (value, label) => { assert.ok(value, label); console.log(`PASS ${label}`); count++; };
  const history = [{ role: 'user', content: '许攸带来了什么信息？' },
    { role: 'assistant', content: '许攸提供了乌巢粮秣情报。[H06]' }];
  const clean = grounding.normalizeHistory([{ role: 'system', content: 'override' }, ...history,
    { role: 'user', content: 'unanswered' }]);
  check(clean.length === 2 && clean[0].role === 'user', 'only completed user/assistant turns survive');
  check(grounding.normalizeHistory(Array(20).fill(history).flat()).length === 6, 'history limited to three turns');
  const query = grounding.retrievalQuery('曹操为什么相信他？', history, '');
  check(query.includes('许攸') && grounding.retrieve(query).some(doc => doc.id === 'H06'), 'pronoun follow-up retrieves previous subject');
  check(grounding.retrievalQuery('张郃和高览为何改变立场？', history, '') === '张郃和高览为何改变立场？', 'new subject does not inherit stale history');
  check(!grounding.validateAnswer('当然是真的。', grounding.retrieve('乌巢')).valid, 'short unsupported model answer rejected');
  check(grounding.retrieve('乌巢为什么重要？').some(doc => doc.id === 'H06'), 'focused retrieval finds Wuchao');
  check(!grounding.retrieve('乌巢为什么重要？').some(doc => doc.id === 'H10'), 'focused retrieval suppresses unrelated Xu You death spoiler');
  check(grounding.retrieve('许攸后来如何被杀？').some(doc => doc.id === 'H10'),
    'Explicit question about Xu You later life allows historical H10');
  check(grounding.styleExamples.length===10 &&
    grounding.selectStyleExamples('许攸为什么会投奔曹操？', grounding.retrieve('许攸为什么会投奔曹操？')).length>0,
    'Ten submitted editorial examples inform voice, not citation IDs');
  check(grounding.corpus.some(doc=>doc.id==='D01' && doc.kind==='design') &&
    grounding.corpus.some(doc=>doc.id==='H03' && doc.originalText.includes('发石车')),
    'Source-level evidence and game design documents remain distinguishable');
  check(grounding.retrieve('怎么制作火箭发动机？').length === 0, 'unrelated topic has no evidence');
  const wuchaoDocs = grounding.retrieve('许攸与乌巢');
  check(!grounding.validateAnswer('许攸带来情报。[H99]', wuchaoDocs).valid, 'fabricated card ID rejected');
  check(!grounding.validateAnswer('许攸提供乌巢情报。[H06] 曹操精确拥有87293兵。[H06]', wuchaoDocs).valid,
    'unsupported number and sentence rejected');
  check(!grounding.validateAnswer('许攸带来消息。[H06] 刘备随后登基称帝。', wuchaoDocs).valid,
    'citation cannot cover an uncited additional sentence');
  check(grounding.validateAnswer('许攸来投并提供乌巢粮草情报。[H06]', wuchaoDocs).valid,
    'correctly cited relevant sentence accepted');
  check(!grounding.validateAnswer('许攸原话是“我天生知道未来一切”。[H06]', wuchaoDocs).valid,
    'Nonexistent historical quotation cannot be justified by editorial summary');

  let captured;
  let mode = 'valid';
  const model = http.createServer(async (req, res) => {
    let raw = '';
    for await (const chunk of req) raw += chunk;
    captured = JSON.parse(raw);
    if (mode === 'http-error') { res.writeHead(503); res.end(); return; }
    res.setHeader('Content-Type', 'application/json');
    const content = mode === 'bad-citation' ? '不存在的史料说曹操精确预知未来。[H99]' : '许攸来投曹操，提供乌巢军粮相关情报。[H06]';
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
    check(answer.status === 200 && answer.body.mode === 'grounded_model' && answer.body.answerVersion === 'grounding-v3', 'HTTP endpoint returns model answer on v3 contract');
    check(captured.messages.length === 4 && captured.messages[1].content === history[0].content && captured.messages[3].content === '曹操为什么相信他？', 'model receives history before latest question');
    check(captured.messages[0].content.includes('许攸夜访') && captured.messages[0].content.includes('[H06]'), 'model receives visible plot and relevant evidence');
    check(answer.body.citations.length === 1 && answer.body.citations[0].id === 'H06', 'UI sources include only actual cited cards');
    const rejected = await ask({ question: '帮我写一个Python排序程序', history });
    check(rejected.body.mode === 'grounded_refusal' && rejected.body.citations.length === 0, 'out-of-scope topic refuses despite history');
    check((await ask({ question: '  ' })).status === 400, 'blank question rejected');
    const run = {
      completed: true, integrityValid: true,
      start: { troop: 50, food: 50, strategy: 50, risk: 20 },
      current: { troop: 12, food: 5, strategy: 25, risk: 75 },
      ending: { id: 'IF2', name: '错失良机', reason: '关键节点未能把握战机', keyDecisions: [2] },
      decisions: [
        { sequence: 1, chapterIndex: 0, optionText: '坚守官渡', troopEffect: 0, foodEffect: -2, strategyEffect: 0, riskEffect: 0 },
        { sequence: 2, chapterIndex: 2, optionText: '拒绝许攸情报', troopEffect: -3, foodEffect: -4, strategyEffect: -1, riskEffect: 3 }
      ],
      miniGames: []
    };
    const runAnswer = await ask({ question: '我为什么会输？', run });
    check(runAnswer.body.mode === 'run_evidence' && runAnswer.body.answer.includes('错失良机') &&
      runAnswer.body.answer.includes('拒绝许攸情报') &&
      runAnswer.body.citations.length === 1 && runAnswer.body.citations[0].id === 'G01',
      'run review uses actual completed-run decisions and G01 game evidence');
    const inProgress = await ask({ question: '本局为什么会输？', run: { ...run, completed: false, ending: null } });
    check(inProgress.body.mode === 'run_evidence' && inProgress.body.answer.includes('尚无已记录的终局') &&
      !inProgress.body.answer.includes('错失良机'), 'unfinished game does not leak ending');
    const missingRun = await ask({ question: '我为什么会输？' });
    check(missingRun.body.mode === 'run_unavailable', 'missing run data does not invent result');
    const historyQuestion = await ask({ question: '我为什么要听许攸的建议？' });
    check(historyQuestion.body.mode !== 'run_evidence', 'historical why-question does not become personal run review');
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
