const fs=require("node:fs"),path=require("node:path"),assert=require("node:assert/strict");
const root=path.resolve(__dirname,"../..");
const read=p=>fs.readFileSync(path.join(root,p),"utf8");
const choices=[
  [100206,"RockBreak"],[100305,"RockFall"],[300401,"WoodThud"],
  [500207,"SwordClashA"],[500208,"FireIgnition"],[500417,"SwordDrop"]
];
const all=["FireIgnition","FireLoop","BattleShout","SwordClashA","SwordClashB",
  "RockBreak","RockFall","WoodBreak","WoodFall","SwordDrop","WoodThud"];
const dir="Assets/Audio/ImportedCC0/";
const ledger=read("Documentation/QA/CC0ImportedAudio.md");
const dialogue=read("Assets/Scripts/Data/GuanduDialogueData.asset");
let passed=0;
const check=(v,m)=>{assert.ok(v,m);console.log("PASS",m);passed++};
const guids=new Map();
for(const name of all){
  const file=path.join(root,dir+name+".ogg");
  check(fs.existsSync(file)&&fs.statSync(file).size>3000,"Imported "+name+" OGG exists");
  const meta=read(dir+name+".ogg.meta");
  const guid=meta.match(/^guid: ([0-9a-f]{32})$/m)?.[1];
  check(!!guid&&!guids.has(guid),"Unique Unity GUID for "+name);
  check(meta.includes("preloadAudioData: 1"),"Audio importer set to preload "+name);
  check(ledger.includes(name+".ogg")&&ledger.includes("CC0"),"Documented CC0 provenance for "+name);
  guids.set(guid,name);
}
for(const [id,name] of choices){
  const mark="  - nodeId: "+id+"\n";
  const start=dialogue.indexOf(mark);
  check(start>=0,"Found story node "+id);
  const next=dialogue.indexOf("\n  - nodeId: ",start+mark.length);
  const block=dialogue.slice(start,next<0?dialogue.length:next);
  const guid=block.match(/sfxOnEnter: \{fileID: 8300000, guid: ([a-f0-9]{32}), type: 3\}/)?.[1];
  check(guids.get(guid)===name,"Node "+id+" correctly references "+name);
}
check(/All|全部|CC0/.test(ledger),"License ledger present");
console.log("CC0 imported asset checks",passed,"passed");
