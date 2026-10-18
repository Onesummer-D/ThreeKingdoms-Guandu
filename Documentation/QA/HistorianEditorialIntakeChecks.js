// Editorial Word originals remain separate; this checks the curated runtime
// H01-H10 migration and distinguishes stylistic exemplars from sources.
const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const dir=path.join(root,'Documentation/Editorial/Historian');
const corpus=JSON.parse(fs.readFileSync(path.join(dir,'historian-corpus-intake.json'),'utf8'));
const examples=JSON.parse(fs.readFileSync(path.join(dir,'historian-example-intake.json'),'utf8'));
const activeServer=JSON.parse(fs.readFileSync(path.join(root,'server/historian-ai/corpus.json'),'utf8'));
const activeUnity=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/Historian/corpus.json'),'utf8'));
const styles=JSON.parse(fs.readFileSync(path.join(root,'server/historian-ai/style-examples.json'),'utf8'));
const grounding=fs.readFileSync(path.join(root,'server/historian-ai/grounding.js'),'utf8');
const ui=fs.readFileSync(path.join(root,'Assets/Scripts/UI/CampaignMapUI.cs'),'utf8');
let checks=0;
function check(condition,message){assert.ok(condition,message);console.log('PASS '+message);checks++;}
const ids=Array.from({length:10},(_,i)=>'H'+String(i+1).padStart(2,'0'));
check(JSON.stringify(corpus.map(x=>x.id))===JSON.stringify(ids),'10 uniquely ordered historical cards');
check(JSON.stringify(examples.map(x=>x.id))===JSON.stringify(ids.map((_,i)=>'Q'+String(i+1).padStart(2,'0'))),
  '10 uniquely ordered historian style examples');
check(corpus.every(x=>x.source && x.originalText && x.originalText.length>15 &&
    x.summary.length>30 && x.boundary.length>30 && x.gameRelation && x.tags.length>2 &&
    x.reviewStatus==='pending_editorial_qa'),'every historical card has the six documented evidence fields');
check(examples.every(x=>x.question && x.referenceAnswer.length>50 &&
    x.sourceIds.length>0 && x.sourceIds.every(id=>ids.includes(id)) &&
    x.scenario && x.claimLayers && x.reviewStatus==='pending_editorial_qa'),
    'every example has source IDs and distinct historical/inferred/game layers');
check(!corpus.some(x=>x.referenceAnswer) && !examples.some(x=>x.originalText),
    'historical evidence and stylistic examples remain separate data types');
check(corpus.find(x=>x.id==='H01').title.includes('兵力') &&
    corpus.find(x=>x.id==='H10').title.includes('许攸的结局'),
    'new H01-H10 numbering is not confused with legacy H01 geography/H10 gameplay');
check(activeServer.length===16 && activeUnity.length===16 &&
    activeServer.every((x,i)=>JSON.stringify(x)===JSON.stringify(activeUnity[i])),
    'curated runtime server and Unity offline packs are the same sixteen cards');
check(activeServer.slice(0,10).every((x,i)=>x.id===ids[i] &&
    x.kind==='history' && x.originalText && x.originalText.length>20),
    'H01-H10 have original source passages and newly assigned stable meanings');
check(activeServer.find(x=>x.id==='H01').title.includes('兵力') &&
    activeServer.find(x=>x.id==='H10').title.includes('许攸') &&
    activeServer.some(x=>x.id==='H11' && x.title.includes('地理')) &&
    activeServer.some(x=>x.id==='D01' && x.kind==='design'),
    'old H numbering has been migrated, with geography preserved and game rules separated');
check(['H12','H13','H14','H15'].every(id=>
    activeServer.some(c=>c.id===id && c.kind==='history' && c.originalText.length>50)),
    'four supplemental scholar-reviewed historical topics retain original passages');
check(activeServer.find(x=>x.id==='H02').summary.includes('斩颜良') &&
    !activeServer.find(x=>x.id==='H02').summary.includes('生擒颜良') &&
    activeServer.find(x=>x.id==='H06').boundary.includes('裴注') &&
    activeServer.find(x=>x.id==='H08').boundary.includes('先降后溃'),
    'high-risk editorial conflicts are not asserted as settled historical facts');
check(styles.length===10 && styles.every(x=>x.kind==='style_example_not_evidence' &&
      !activeServer.some(c=>c.id===x.id) && x.referenceAnswer && x.sourceIds.every(id=>ids.includes(id))),
    'all ten examples are style-only and never become historical citations');
check(grounding.includes('selectStyleExamples(question, docs)') &&
    grounding.includes("doc.id !== 'H10' || asksLaterFate(question)") &&
    grounding.includes("doc.kind === 'design'"),
    'runtime uses editorial voice, spoiler gate, and design-vs-history labeling');
check(ui.includes('游戏中的四项资源') &&
  ui.includes('兵力、粮草、计策与风险四项资源'),
  'player-facing text now correctly names all four resources');
check(fs.readFileSync(path.join(dir,'README-交稿审查与编号迁移.md'),'utf8').includes('生擒颜良'),
    'source inconsistency requiring human review remains documented');
const chat=fs.readFileSync(path.join(root,'Assets/Scripts/UI/HistorianChatUI.cs'),'utf8');
check(chat.includes('RefreshSuggestionsForCurrentChapter()') &&
  chat.includes('CampaignChapterResolver.Resolve(nodeId)') &&
  chat.includes('activeSuggestedQuestions[capturedIndex]') &&
  chat.includes('GetComponentInChildren<TMP_Text>()'),
  'suggested historian questions update by reached chapter without rebuilding the buttons');
const groups = chat.split('private static readonly string[][] ChapterSuggestedQuestions =')[1]
  .split('};')[0].split('\n').filter(line => line.trim().startsWith('new[] {'));
check(groups.length===5 && groups.every(g => (g.match(/"/g)||[]).length===6) &&
  !groups[0].includes('张郃') && !groups[0].includes('结局'),
  '15 editorial prompts are restricted to three choices per chapter with no first-act ending spoilers');
console.log('historian-editorial-intake: '+checks+'/'+checks);
