const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const historian = require('./grounding');

async function run() {
  const cases = JSON.parse(fs.readFileSync(path.join(__dirname,'editorial-quiz.json'),'utf8'));
  const oldKey = process.env.OPENAI_API_KEY;
  delete process.env.OPENAI_API_KEY;
  let passed=0, manual=0;
  try {
    for (const item of cases) {
      if (item.manualOnly) {
        console.log('HUMAN-REVIEW '+item.id+' '+item.question);
        manual++;
        continue;
      }
      const relevant = historian.retrieve(item.question);
      assert.ok(item.mustCite.every(id => relevant.some(card=>card.id===id)),
        item.id+' did not retrieve the needed source evidence');
      const result = await historian.answerQuestion(item.question, '');
      assert.ok(item.mustCite.every(id => result.citations.some(c=>c.id===id)),
        item.id+' used the wrong citation');
      assert.ok(item.mustMention.every(phrase=>result.answer.includes(phrase)),
        item.id+' omitted an answer keyword from its evidence');
      assert.ok(!result.answer.includes('[H99]') && !result.answer.includes('undefined'),
        item.id+' contains unsupported reference/placeholder');
      console.log('PASS '+item.id+' '+item.question);
      passed++;
    }
  } finally {
    if (oldKey===undefined) delete process.env.OPENAI_API_KEY;
    else process.env.OPENAI_API_KEY=oldKey;
  }
  console.log('editorial-quiz: '+passed+'/'+(cases.length-manual)+
    ' automatic, '+manual+' marked for independent human review');
}
run().catch(err=>{console.error(err);process.exitCode=1;});
