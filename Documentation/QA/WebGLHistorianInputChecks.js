// Run with Node 22. Cover the DOM IME bridge without Unity runtime.
'use strict';
const fs = require('fs');
const vm = require('vm');
const assert = require('assert');
const source = fs.readFileSync('Assets/Plugins/WebGL/GuanduHistorianChineseInput.jslib', 'utf8');
const ui = fs.readFileSync('Assets/Scripts/UI/HistorianChatUI.cs', 'utf8');
const finalUi = fs.readFileSync('Assets/Scripts/Managers/FinalUIManager.cs', 'utf8');
const agent = fs.readFileSync('Assets/Scripts/UI/HistorianAgentClient.cs', 'utf8');
const grounding = fs.readFileSync('server/historian-ai/grounding.js', 'utf8');
let now = 1000;
const events = {};
function fakeElement() {
  return {
    style: {}, listeners:{}, value:'', focusCalled:false,
    addEventListener(type, fn){this.listeners[type] = fn;},
    removeEventListener(type){delete this.listeners[type];},
    dispatch(type, event = {}){if(this.listeners[type])this.listeners[type](event);},
    setAttribute(){}, focus(){this.focusCalled=true;}, remove(){this.removed=true;}
  };
}
const canvas={getBoundingClientRect:()=>({left:200,top:100,width:960,height:540})};
const doc = {
  getElementById:()=>canvas,querySelector:()=>canvas,
  createElement:()=>fakeElement(),
  body:{appendChild(x){this.last=x;}},
  addEventListener(type,fn){events[type]=fn;},
  removeEventListener(type){delete events[type];}
};
const win = {
  addEventListener(type,fn){events[type]=fn;},
  removeEventListener(type){delete events[type];}
};
const library={};
vm.runInNewContext(source,{
  window:win,document:doc,LibraryManager:{library},
  mergeInto:(target,members)=>Object.assign(target,members),
  UTF8ToString:x=>x,stringToNewUTF8:x=>x,performance:{now:()=>now}
});
library.GuanduHistorianInputOpen('',100,120,800,100,1920,1080);
const area = doc.body.last;
assert(area && area.focusCalled, 'native DOM textarea takes input focus');
assert.strictEqual(area.style.left,'250px','input follows WebGL canvas transform');
assert.strictEqual(area.maxLength,800);
assert.strictEqual(area.placeholder,'想问些什么？输入问题…');
area.value='zhong';
area.dispatch('compositionstart');
area.dispatch('input');
assert.strictEqual(library.GuanduHistorianInputChanged(),0,'do not forward unfinished pinyin');
area.dispatch('keydown',{key:'Enter',isComposing:true,keyCode:229,preventDefault(){}});
assert.strictEqual(library.GuanduHistorianInputWasSubmitted(),0,'IME confirm not chat submit');
area.value='中文';
area.dispatch('compositionend');
assert.strictEqual(library.GuanduHistorianInputChanged(),1,'completed Chinese composition is accepted');
assert.strictEqual(library.GuanduHistorianInputRead(),'中文');
area.dispatch('keydown',{key:'Enter',isComposing:false,keyCode:13,shiftKey:false,preventDefault(){}});
assert.strictEqual(library.GuanduHistorianInputWasSubmitted(),0,'avoid duplicate Enter after IME candidate');
now += 200;
area.dispatch('keydown',{key:'Enter',isComposing:false,keyCode:13,shiftKey:false,preventDefault(){}});
assert.strictEqual(library.GuanduHistorianInputWasSubmitted(),1,'later Enter submits question');
area.dispatch('keydown',{key:'Enter',isComposing:false,keyCode:13,shiftKey:true,preventDefault(){}});
assert.strictEqual(library.GuanduHistorianInputWasSubmitted(),0,'Shift Enter is newline');
library.GuanduHistorianInputSetText('');
assert.strictEqual(area.value,'');
area.dispatch('keydown',{key:'Escape',isComposing:false,preventDefault(){}});
assert.strictEqual(library.GuanduHistorianInputWasCancelled(),1,'Escape closes chat');
library.GuanduHistorianInputClose();
assert(area.removed && !win.__guanduHistorianInput,'closing removes browser input overlay');

for(const symbol of ['GuanduHistorianInputOpen','GuanduHistorianInputChanged',
 'GuanduHistorianInputRead','GuanduHistorianInputWasSubmitted','GuanduHistorianInputWasCancelled',
 'GuanduHistorianInputSetText','GuanduHistorianInputClose'])
  assert(ui.includes(symbol), 'Unity binding missing: '+symbol);
assert(ui.includes('input.readOnly = true')&&ui.includes('OpenBrowserInput()')&&
  ui.includes('#if UNITY_WEBGL && !UNITY_EDITOR'),'WebGL uses native IME, editor keeps TMP');
assert(ui.includes('input.text = GuanduHistorianInputRead()') &&
  ui.includes('SubmitQuestion(input.text)'), 'IME committed question reaches historian request');
assert(agent.includes('baseUrl.TrimEnd(\'/\') + "/api/historian/ask"') &&
  agent.includes('request.SendWebRequest()'),
  'Unity historian connects to the configured network endpoint');
assert(grounding.includes('process.env.OPENAI_API_KEY') &&
  grounding.includes('process.env.OPENAI_API_URL') &&
  grounding.includes("mode = 'grounded_model'"),
  'server may call a configured model, but only grounded_model proves a validated model response');
assert(finalUi.includes('if (gameInterfacePanel != null) gameInterfacePanel.SetActive(false);') &&
  !finalUi.includes('StartCoroutine(ShowCharactersCoroutine(node))') &&
  !finalUi.includes('yield return null;\n\n        if (!string.IsNullOrEmpty(node.avatarCharacter))'),
  'new run hides old scene and paints node portraits without a stale frame');
console.log('WebGL historian IME, API route and node flash contracts: PASS');
