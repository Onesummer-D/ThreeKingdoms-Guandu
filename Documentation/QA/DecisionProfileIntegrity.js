const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'../..');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const asset=read('Assets/Scripts/Data/GuanduDialogueData.asset');
const catalog=JSON.parse(read('Assets/Resources/BattleReport/DecisionBehaviors.json'));
const analyzer=read('Assets/Scripts/UI/BattleReportAnalyzer.cs');
const ui=read('Assets/Scripts/UI/BattleReportUI.cs');
const history=read('Assets/Scripts/Managers/RunHistoryData.cs');
const tracker=read('Assets/Scripts/Managers/RunHistoryTracker.cs');
let passed=0;
function check(truth,msg){assert.ok(truth,msg);passed++;console.log('PASS '+msg);}
check(catalog.version===1&&catalog.entries.length===13,'Stable version and 13 real selected choices');
const nodeRegex=/^  - nodeId: (\d+)\n([\s\S]*?)(?=^  - nodeId: |$)/gm;
const realChoices=new Map();
for(const block of asset.matchAll(nodeRegex)){
  const count=(block[2].match(/^    - optionText:/gm)||[]).length;
  if(count)realChoices.set(+block[1],count);
}
check(realChoices.size===5,'Script has exactly five strategic option sets');
let validKeys=new Set();
for(const row of catalog.entries){
  const key=row.nodeId+':'+row.optionIndex;
  check(realChoices.has(row.nodeId)&&row.optionIndex>=0&&row.optionIndex<realChoices.get(row.nodeId),
    'Mapped option actually exists: '+key);
  check(!validKeys.has(key),'Unique behavior mapping: '+key);
  const block=asset.match(new RegExp('^  - nodeId: '+row.nodeId+'\\n([\\s\\S]*?)(?=^  - nodeId: |$)','m'));
  const destinations=[...block[1].matchAll(/^      nextNodeId: (\d+)/gm)].map(x=>Number(x[1]));
  check(destinations[row.optionIndex]===row.nextNodeId,
    'Mapped choice still leads to the verified real route: '+key);
  validKeys.add(key);
}
check([...realChoices].every(([id,count])=>Array.from({length:count},(_,i)=>id+':'+i).every(key=>validKeys.has(key))),
  'Every key decision choice has a semantic record: no silent holes');
check(catalog.entries.every(row=>['稳慎','奇谋','果决',''].includes(row.strategy)&&
    ['none','adopt','review'].includes(row.counsel)&&row.action.length>5),
  'All action semantics explicit and text-independent');
check(!/ContainsAny\(|optionText\s*\?\?|IndexOf\(needles|decision\.optionText/.test(analyzer),
  'Analyzer cannot classify using dialogue text');
check(/DecisionBehaviorCatalog\.TryFind\(choice\.nodeId, choice\.optionIndex/.test(analyzer),
  'Analyzer uses stable node and selected option IDs');
check(/choice\.before\s*==\s*null \|\| choice\.after\s*==\s*null/.test(analyzer),
  'Resource delta printed only when actual before/after snapshots exist');
check(/advisor\.accepted && acted\.optionIndex == advisor\.optionIndex/.test(analyzer),
  'Guest advice must be both approved AND actually chosen to count');
check(analyzer.includes('审议自决')&&!analyzer.includes('不听劝')&&!analyzer.includes('固执'),
  'Rejected-but-reviewed advice is not a negative personality label');
check(/history\.miniGames/.test(analyzer)&&/history\.completed && history\.ending/.test(analyzer),
  'Real minigames and endings are outcome evidence, not strategy score');
check(/谋议方式 ·/.test(ui)&&/tendency\.consultationLabel/.test(ui)&&
  /tendency\.evidence/.test(ui)&&/tendency\.outcomeEvidence/.test(ui),
  'Battle report renders two dimensions and actual evidence');
check(!ui.includes('实际资源变化与战局记录可供追溯'),
  'No empty claim of traceability');
check(ui.includes('CreateResourceSummary(')&&ui.includes('OpenTrendDetail(timeline)'),
  'Actual resource plot already gives the detailed trend drilldown');
check(history.includes('public int nodeId;')&&history.includes('public int optionIndex;')&&
  history.includes('public ResourceSnapshotData before;')&&
  history.includes('public ResourceSnapshotData after;'),
  'Existing saves carry all decision identity and actual before/after values');
check(tracker.includes('optionIndex = optionIndex,')&&tracker.includes('before = before')&&
  tracker.includes('after = CloneSnapshot(after)'),
  'Recorder still preserves the authoritative choice and resource snapshots');
console.log('decision-profile-integrity: '+passed+'/'+passed);
