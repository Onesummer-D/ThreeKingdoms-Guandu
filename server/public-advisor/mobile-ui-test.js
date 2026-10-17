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
check(html.includes("background:url('/assets/advisor-option-paper.webp')") &&
      html.includes("item.classList.toggle('selected',active)") &&
      html.includes("item.setAttribute('aria-pressed',String(active))") &&
      !html.includes('setAttribute(\'viewBox\',active?'),
  'Real parchment UI replaces white option cards; selected/keyboard state is kept');
check(html.includes("url('/assets/battle-hero.png')"),
  'Selected wartime illustration C is mounted as mobile masthead');

check(html.includes('src="/assets/advisor-gold-title.webp"') &&
      html.includes('class="sr-only">官渡之战 · 军议邀约</h1>') &&
      server.includes("'advisor-gold-title.webp':'image/webp'") &&
      fs.existsSync(path.join(root,'public/assets/advisor-gold-title.webp')),
  'Uploaded transparent gold headline replaces CSS text without losing accessible heading');
check(html.includes('--reading-font:') &&
      html.includes('font-family:var(--reading-font)') &&
      html.includes('color:#28180d') &&
      html.includes('color:#69492e;opacity:1;font-weight:400'),
  'Reference-style readable Song typography and dark input contrast are configured');
check(html.includes('padding:clamp(41px,8.5vw,55px) clamp(46px,11vw,76px)') &&
      html.includes('grid-template-rows:minmax(0,1fr) minmax(0,1fr)') &&
      html.includes('.paper-field:focus-within'),
  'Both text fields are inset into their own parchment halves, clear of decorative rim');

check(['resource-troop.png','resource-food.png','resource-strategy.png','resource-risk.png']
      .every(file=>html.includes('/assets/'+file)) &&
      html.includes('resource-photo') && html.includes('photo.appendChild(img)'),
  'Original four resource textures are clipped to icons and values stay dynamic');
check(html.includes("url('/assets/advisor-submit-stone.webp')") &&
      html.includes("url('/assets/seal-present.png')") &&
      html.includes('献策已呈'),
  'User stone submission UI is shown with a truthful submitted state');
for (const file of ['battle-hero.png','resource-troop.png','resource-food.png','resource-strategy.png',
                    'resource-risk.png','seal-present.png']) {
  check(server.includes("'"+file+"':'image/png'") &&
    fs.existsSync(path.join(root,'public/assets',file)),file+' is present and served by server whitelist');
}
const customAssets = ['advisor-wood-briefing.webp','advisor-double-parchment.webp',
  'advisor-submit-stone.webp','advisor-vertical-scroll.webp',
  'advisor-question-panel.webp','advisor-option-paper.webp'];
for (const file of customAssets) {
  const contents = fs.readFileSync(path.join(root,'public/assets',file));
  check(server.includes("'"+file+"':'image/webp'") &&
        html.includes('/assets/'+file) &&
        contents.length > 4096 &&
        contents.toString('ascii',0,4) === 'RIFF' &&
        contents.toString('ascii',8,12) === 'WEBP',
    'Provided original artwork '+file+' is actually present and served as WebP');
}
check(!/<p>军前问策/.test(html) &&
      /\.masthead-title h1\s*\{font-size:clamp\(32px,8\.2vw,45px\)/.test(html) &&
      html.includes('inset:auto 0 33px'),
  'Header is larger, raised, and has no old subtitle');
check(html.includes("advisor-vertical-scroll.webp") &&
      html.includes('border-image:') &&
      html.includes("advisor-question-panel.webp") &&
      html.includes("advisor-double-parchment.webp"),
  'Situation is a stretchable vertical scroll; question and writing fields use the exact supplied frames');
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
check(!html.includes('本次军议仅接收一份有效建议；提交不代表主将采纳。') &&
      !html.includes('id="footerNote"') && html.includes('shareBtn'),
  'Requested bottom disclaimer removed without changing share API rules');
check(/joinGame/.test(html)&&/validGameUrl/.test(html),
  'Game CTA hidden until official URL configured');
check(server.includes(".replace('__GAME_ENTRY_URL__', inlineJson(PUBLIC_GAME_ENTRY_URL))"),
  'Server injects optional landing URL safely');
check(server.includes("replace(/</g, '\\\\u003c')"),
  'Inline JSON encodes HTML-breaking angle brackets');
console.log('mobile-advisor-page: '+count+'/'+count);
