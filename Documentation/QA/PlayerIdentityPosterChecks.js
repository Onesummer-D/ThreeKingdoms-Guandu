const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
const load=x=>fs.readFileSync(path.join(root,x),'utf8');
const manager=load('Assets/Scripts/Managers/FinalUIManager.cs');
const entry=load('Assets/Scripts/UI/PlayerIdentityUI.cs');
const report=load('Assets/Scripts/UI/BattleReportUI.cs');
const state=load('Assets/Scripts/UI/PlayerIdentityState.cs');
let pass=0;
function check(ok,label){assert.ok(ok,label);console.log('PASS '+label);pass++;}
check(/playerIdentityUI.Initialize\(gameInterfacePanel.transform.root, startMenuPanel.transform\)/.test(manager),
  'Identity gate initialized after existing menu');
check(/public void StartGame\(\)[\s\S]{0,160}!PlayerIdentityState.HasId/.test(manager),
  'Start Game blocked until player chooses ID');
check(/private void BeginNewRun\(\)[\s\S]{0,160}!PlayerIdentityState.HasId/.test(manager),
  'Replay/new run paths also require ID');
check(/public bool LoadRunFromSlot\([\s\S]{0,180}!PlayerIdentityState.HasId/.test(manager),
  'Loading an existing slot goes through the local identity gate');
check(entry.includes('GameButtonSkin.Apply('),'ID screen uses existing ornate UI button language');
check(entry.includes('idInput.textViewport')&&entry.includes('idInput.textComponent')&&
      entry.includes('idInput.caretColor')&&entry.includes('idInput.onValidateInput'),
      'TMP input is wired for actual typing and visible caret');
check(entry.includes('PlayerIdentityState.CurrentId')&&entry.includes('主将 ID：'),
  'Main menu offers the current ID and an edit action');
check(state.includes('PlayerPrefs.Save()')&&!state.includes('UnityWebRequest'),
  'Identity is saved locally only; not a fabricated account');
check(report.includes('CreatePosterSignature(canvasRect, bodyHeight, signatureHeight)'),
  'Export renderer appends poster footer');
check(report.includes('PlayerIdentityState.CurrentId')&&report.includes('"主将 ID："'),
  'Poster footer uses the same local ID');
check(report.includes('gameEntryQrSprite != null')&&report.includes('BattleReport/GameEntryQR'),
  'QR drawn only after actual final URL QR Sprite is installed');
check(!report.includes('https://example.com')&&!report.includes('placeholderQR'),
  'No dummy or unscannable QR artwork is ever exported');
check(report.includes('copy.sizeDelta = new Vector2(0f, bodyHeight)')&&
      report.includes('float height = bodyHeight + signatureHeight'),
  'Export copy is not stretched into new footer region');
console.log('player-id-poster-integration: '+pass+'/'+pass);
