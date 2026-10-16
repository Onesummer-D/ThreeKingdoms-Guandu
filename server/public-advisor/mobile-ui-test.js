const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
const root=__dirname;
const html=fs.readFileSync(path.join(root,'public/index.html'),'utf8');
const server=fs.readFileSync(path.join(root,'server.js'),'utf8');
let count=0;
function check(ok,label){assert.ok(ok,label);console.log('PASS '+label);count++;}
const code=html.match(/<script>([\s\S]*?)<\/script>/);
check(!!code,'H5 page contains executable script');
new vm.Script(code[1].replace('__SESSION_ID__','"testing"')
  .replace('__JOIN_TOKEN__','"token"').replace('__GAME_ENTRY_URL__','""'));
check(true,'H5 Javascript syntax valid after server token replacement');
check(/class="scroll-paper"/.test(html)&&/class="masthead"/.test(html),
  'Mobile military-camp hero and parchment briefing exist');
check(/class="question"/.test(html)&&/class="options"/.test(html),
  'Question and wooden-choice sections separated');
check(html.includes("url('/assets/option.png')")&&html.includes("url('/assets/option-selected.png')"),
  'Existing wooden option artwork reused');
check(/id="recapToggle"/.test(html)&&/function renderRecap/.test(html),
  'Long visited-event history can be expanded without early spoilers');
check(/function renderResources/.test(html)&&/Number.isFinite\(raw\)/.test(html),
  'Four real resource values drawn with validation, not forecasts');
check(/clientRequestId:retryId/.test(html)&&/optionIndex:selected/.test(html)&&
  /guestLabel,optionIndex:selected,reason/.test(html),
  'Original API submit payload and request idempotency retained');
check(/submitted=true/.test(html)&&/献策已呈/.test(html)&&
  !/已纳良言/.test(html),
  'Submitted does not falsely claim command acceptance');
check(/setUnavailable/.test(html)&&/data.state!=='awaiting_guest'/.test(html),
  'Expired/closed invitation disables submission');
check(/expiresAt=Number\(data.expiresAt\)/.test(html)&&/setInterval\(countdown,1000\)/.test(html),
  'Real server deadline drives expiry display');
check(/仅接收一份有效建议/.test(html)&&/shareBtn/.test(html),
  'Forwarding discloses one-submission limitation');
check(/joinGame/.test(html)&&/validGameUrl/.test(html),
  'Game CTA hidden until official URL configured');
check(server.includes(".replace('__GAME_ENTRY_URL__', inlineJson(PUBLIC_GAME_ENTRY_URL))"),
  'Server injects optional landing URL safely');
check(server.includes("replace(/</g, '\\\\u003c')"),
  'Inline JSON encodes HTML-breaking angle brackets');
console.log('mobile-advisor-page: '+count+'/'+count);
