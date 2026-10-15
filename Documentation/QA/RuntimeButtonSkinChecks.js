const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'../..');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const factories=['InviteCoCreationUI','CampaignMapUI','BattleReportUI',
  'BattleReplayUI','HistorianChatUI','HomeHubUI','SaveArchiveUI','GameplayExitUI'];
let passed=0;
function check(ok,label){assert.ok(ok,label);console.log('PASS '+label);passed++;}
for(const name of factories) {
  const text=read('Assets/Scripts/UI/'+name+'.cs');
  check(/GameButtonSkin\.Apply\(/.test(text),'Ornate runtime buttons: '+name);
}
const invite=read('Assets/Scripts/UI/InviteCoCreationUI.cs');
for(const verb of ['采纳建议','拒绝建议','撤销邀约','关闭','再邀一位参谋'])
  check(invite.includes('"'+verb+'"'),'Invite action retained: '+verb);
check(/accept\.onClick\.AddListener\(AcceptAdvisorSuggestion\)/.test(invite)&&
  /decline\.onClick\.AddListener\(DeclineAdvisorSuggestion\)/.test(invite)&&
  /withdraw\.onClick\.AddListener\(WithdrawAdvisorInvitation\)/.test(invite),
  'Review action handlers are unchanged');
const campaign=read('Assets/Scripts/UI/CampaignMapUI.cs');
check(campaign.includes('"史官卡片"')&&campaign.includes('GameButtonSkin.Apply(button, image, label)'),
  'Historian gallery button is styled');
check((campaign.match(/GameButtonSkin\.Apply\(/g)||[]).length>=3,
  'Recap entry, story notes, and utility buttons are all styled');
const common=read('Assets/Scripts/UI/GameButtonSkin.cs');
for(const word of ['"采纳建议"','"拒绝建议"','"撤销邀约"','"史官卡片"','"关闭"'])
  check(common.includes(word),'Central semantic style for '+word);
check(common.includes('StartButton')&&common.includes('FindSceneButtonSprite'),
  'The button art comes from the existing story/game main button');
check(common.includes('Image.Type.Sliced')&&common.includes('border.sqrMagnitude'),
  'Valid 9-slice guard');
check(common.includes('highlightedColor')&&common.includes('pressedColor')&&common.includes('Outline'),
  'Hover, press and edge emphasis enabled');
console.log('runtime-button-style-checks: '+passed+'/'+passed);
