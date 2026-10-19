// Guard the player-facing voice without confusing text quality with fact checking.
// Real DeepSeek/Unity listening tests remain required before public deployment.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const ui = read('Assets/Scripts/UI/CampaignMapUI.cs');
const offline = read('Assets/Scripts/UI/HistorianChatUI.cs');
const grounding = require('../../server/historian-ai/grounding.js');
let count = 0;
const check = (ok, label) => { assert.ok(ok, label); count++; console.log('PASS ' + label); };
const notes = ui.split('private static readonly Dictionary<int, HistorianNote> HistorianNotes =')[1]
  .split('private static readonly HistorianCard[] HistorianCards =')[0];
check((notes.match(/new HistorianNote\(/g) || []).length === 5, 'Five active chapter notes kept');
check(notes.includes('裴松之注引《曹瞒传》') && notes.includes('先退者势屈') &&
  notes.includes('步骑五千'), 'Historian notes explain named sources and specific deeds');
check(!notes.includes('官渡开局的关键不是求快') &&
  !notes.includes('长期对峙考验的不是') &&
  !notes.includes('情报只有被信任并及时执行') &&
  !notes.includes('奇袭不是单纯押注速度') &&
  !notes.includes('决定胜负的往往不是'),
  'Five generic thesis-like chapter interpretations replaced');
const gallery = ui.split('private static readonly HistorianCard[] HistorianCards =')[1]
  .split('private ')[0];
check((gallery.match(/new HistorianCard\(/g) || []).length === 13,
  'All thirteen gallery cards remain');
check(!gallery.includes('真正的转折来自此前的判断积累') &&
  !gallery.includes('兵力规模带来压力的同时') &&
  !gallery.includes('结局不是终点标签'),
  'Gallery commentary no longer uses key template platitudes');
const wuchao = grounding.retrieve('曹操为什么因粮尽想退许都？');
const prompt = grounding.buildGroundedPrompt('曹操为什么因粮尽想退许都？',
  '玩家在第二幕官渡相持', wuchao);
check(prompt.includes('【先给知识】') && prompt.includes('【按问题改变讲法】') &&
  prompt.includes('不要每次都先否定'), 'Model instructed to lead with knowledge and vary by question');
check(prompt.includes('source') === false || prompt.includes('可引用的历史/项目证据卡片'),
  'Prompt still carries source evidence');
check(prompt.includes('史料原文：') && prompt.includes('边界：'),
  'Prompt retains historical source and uncertainty boundaries');
const style = grounding.selectStyleExamples('许攸为什么投曹？',
  grounding.retrieve('许攸为什么投曹？'));
check(style.length > 0 && style.every(x=>x.voiceSample.length <= 390),
  'Editorial writing examples used only as bounded voice samples');
const single = [grounding.corpus.find(x=>x.id==='H04')];
const fallback = grounding.fallbackAnswer('曹操为何议退许都', single);
check(fallback.includes('荀彧') && fallback.includes('[H04]') &&
  !fallback.startsWith('现有史料不足以确认'),
  'Fallback begins with a useful attributed detail, not a blanket denial');
check(!fallback.includes('史书记载与边界') && !fallback.includes('史实／推断／演绎'),
  'Fallback omits robotic category labels');
check(offline.includes('int limit = Math.Min(1, matches.Count)') &&
  !offline.includes('Append("（")'),
  'Unity offline historian no longer stacks generic abstracts');
check(!/精确\|准确\|每一天\|分钟\|伤亡\|逐日/.test(offline),
  'Broad keyword-based refusal no longer discards ordinary accuracy questions');
console.log('historian-answer-voice: ' + count + '/' + count);
