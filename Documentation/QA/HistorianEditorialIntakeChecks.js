// Imported Word files are intentionally NOT wired into runtime grounding.js.
// Validate the editorial handoff before changing authoritative Hxx meanings.
const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const dir=path.join(root,'Documentation/Editorial/Historian');
const corpus=JSON.parse(fs.readFileSync(path.join(dir,'historian-corpus-intake.json'),'utf8'));
const examples=JSON.parse(fs.readFileSync(path.join(dir,'historian-example-intake.json'),'utf8'));
const activeServer=JSON.parse(fs.readFileSync(path.join(root,'server/historian-ai/corpus.json'),'utf8'));
const activeUnity=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/Historian/corpus.json'),'utf8'));
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
check(activeServer.length===10 && activeUnity.length===10 &&
    activeServer.every((x,i)=>JSON.stringify(x)===JSON.stringify(activeUnity[i])),
    'existing production server and Unity knowledge packs remain synchronized');
check(activeServer[0].title.includes('官渡与许都') && activeServer[9].title.includes('复盘'),
    'unapproved editorial data has NOT silently replaced runtime citations');
check(fs.readFileSync(path.join(dir,'README-交稿审查与编号迁移.md'),'utf8').includes('生擒颜良'),
    'source inconsistency requiring human review remains documented');
console.log('historian-editorial-intake: '+checks+'/'+checks);
