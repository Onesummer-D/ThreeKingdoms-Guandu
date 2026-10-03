const fs = require('fs');
const path = require('path');
const grounding = require('./grounding');

async function main() {
  const lines = fs.readFileSync(path.join(__dirname, 'eval.jsonl'), 'utf8')
    .trim().split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line));
  let passed = 0;
  for (const item of lines) {
    const result = await grounding.answerQuestion(item.question, '');
    const text = result.answer || '';
    const citationsOk = item.mustCite.every(id => text.includes(`[${id}]`));
    const mentionOk = !item.mustMention || text.includes(item.mustMention);
    const passedCase = citationsOk && mentionOk;
    console.log(`${passedCase ? 'PASS' : 'FAIL'} ${item.question} mode=${result.mode}`);
    if (!passedCase) process.exitCode = 1;
    if (passedCase) passed++;
  }
  console.log(`historian-eval: ${passed}/${lines.length}`);
}

main().catch(error => { console.error(error); process.exitCode = 1; });
