// Editorial P3 integration: guard carefully adopted UI copy and anti-spoiler locks.
// These are source-level regressions; Unity Play Mode and historian factual
// review remain separate release gates.
'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname,'../..');
const read = p=>fs.readFileSync(path.join(root,p),'utf8');
const brief=read('Assets/Scripts/UI/InviteCoCreationUI.cs');
const recap=read('Assets/Scripts/UI/CampaignMapUI.cs');
const home=read('Assets/Scripts/UI/HomeHubUI.cs');
const chat=read('Assets/Scripts/UI/HistorianChatUI.cs');
const report=read('Assets/Scripts/UI/BattleReportUI.cs');
const server=read('server/historian-ai/corpus.json');
const offline=read('Assets/Resources/Historian/corpus.json');
let n=0;
function check(cond,desc){assert.ok(cond,desc); console.log('PASS '+desc);n++;}
check(brief.includes('官渡两军相持，你要在兵力、粮草、计策与风险之间作出取舍。'),
    'Intro describes all four playable resources');
check(!brief.includes('你要在粮草、兵力与风险之间作出取舍'),
    'Obsolete three-resource intro is gone');
check(home.includes('每次决策都会改变兵力、粮草、计策与风险'),
    'Home rules still describe real resources');
check(recap.includes('new HistorianCard("游戏中的四项资源"') &&
    !recap.includes('new HistorianCard("游戏中的三项资源"'),
    'Historian game-mechanics card distinguishes four real bars');
check(recap.includes('两军相持，粮草见底；坚守还是撤退，一念之差，改变战局。'),
    'Second-act recap reflects approved copy');
check(recap.includes('InviteStoryRecapBuilder.Build('),
    'No static chapter summary replaces verified public invitation events');
check(recap.includes('private static bool IsHistorianCardUnlocked(') &&
    recap.includes('IsHistorianCardUnlocked(card, visited, currentNodeId)'),
    'Gallery gates cards through narrative progress');
const gating=recap.split('private static bool IsHistorianCardUnlocked(')[1].split('private void RebuildHistorianGallery(')[0];
check(gating.includes('visited.Contains(300309) && currentNodeId != 300309'),
    'Xu You and Wu Chao lore only unlock after intelligence scene');
check(gating.includes('visited.Contains(500208) && currentNodeId != 500208'),
    'Burning-grain lore only unlocks after the event occurs');
check(gating.includes('FindReachedEndingNodeId(visited) > 0'),
    'Historical outcome and after-war lore wait until a completed route');
check(['"许攸夜访"','"乌巢粮仓"','"曹操焚粮"','"官渡的史实结局"','"战后余波"']
      .every(x=>gating.includes('case '+x+':')),
    'All five spoiler-sensitive lore cards have explicit guards');
check((recap.match(/new HistorianCard\(/g)||[]).length===13,
    'Thirteen illustrated lore cards preserved');
check((chat.split('private static readonly string[][] ChapterSuggestedQuestions =')[1]||'')
      .split('private readonly List<HistorianChatMessage>')[0].match(/new\[\] \{/g)?.length===5,
    'Five chapters still provide exactly three guided questions');
check(chat.includes('RefreshSuggestionsForCurrentChapter()'),
    'Guided questions still refresh from actual chapter');
check(report.includes('架空推演：部分结局为游戏创作表达，不代表史实'),
    'Ending screen preserves historical-vs-IF disclaimer');
check(server===offline,
    'Online and offline historian evidence remain identical');
const cards=JSON.parse(server);
check((cards.cards||cards).length===16,
    'Fifteen historical evidence cards plus one design-only card remain');
console.log('editorial-player-copy-checks '+n+'/'+n+' passed');
