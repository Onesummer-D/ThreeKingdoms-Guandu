// Validate the real serialized DialogueDataSO node bindings, not mock examples.
// Silence at non-cue nodes is intentional; major unrecorded audio gaps are documented
// in Documentation/QA/NarrativeAudioAudit.md for the audio/copy team.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const file = name => fs.readFileSync(path.join(root, name), 'utf8');

function collectMetas(dir, table = new Map()) {
  for (const entry of fs.readdirSync(dir, {withFileTypes:true})) {
    const filename = path.join(dir, entry.name);
    if (entry.isDirectory()) collectMetas(filename, table);
    else if (/\.(?:ogg|wav|mp3)\.meta$/i.test(filename)) {
      const contents = fs.readFileSync(filename, 'utf8');
      const guid = contents.match(/^guid:\s*([a-f0-9]{32})\s*$/m)?.[1];
      assert.ok(guid, 'AudioImporter GUID missing: ' + filename);
      assert.ok(!table.has(guid), 'Duplicate sound GUID: ' + guid);
      const clipPath = path.relative(root, filename).replace(/\.meta$/, '').replace(/\\/g, '/');
      assert.ok(fs.existsSync(filename.replace(/\.meta$/, '')), 'Audio source missing: ' + filename);
      table.set(guid, clipPath);
    }
  }
  return table;
}
const assets = collectMetas(path.join(root,'Assets/Audio'));
const raw = file('Assets/Scripts/Data/GuanduDialogueData.asset');
const matches = [...raw.matchAll(/^  - nodeId: (\d+)\r?\n/gm)];
const nodes = new Map();
for (let i = 0; i < matches.length; i++) {
  const start = matches[i].index, end = i+1 < matches.length ? matches[i+1].index : raw.length;
  const node = raw.slice(start,end);
  const sfx = node.match(/^\s*sfxOnEnter:\s*\{fileID:\s*(\d+)(?:,\s*guid:\s*([a-f0-9]{32}))?/m);
  assert.ok(sfx, 'Node missing sfxOnEnter field: '+matches[i][1]);
  assert.ok(!nodes.has(+matches[i][1]), 'Duplicate node '+matches[i][1]);
  nodes.set(+matches[i][1], {sfx: sfx[1] === '0' ? null : sfx[2],body:node});
}
assert.ok(nodes.size >= 200, 'Unexpectedly small dialogue collection; verify parsing before trusting result');
const required = new Map([
  [100104,'箭矢破空音.ogg'],
  [100203,'锯子锯木头的声音.ogg'],
  [100302,'园艺-用铲子挖树叶和碎片_.ogg'],
  [100503,'奔跑的骏马.ogg'],
  [100605,'箭矢破空音.ogg'],
  [200003,'纸张翻动音效.ogg'],
  [500105,'警报声.ogg'],
  [500201,'拔剑_.ogg'],
  [500205,'箭矢破空音.ogg'],
  [500206,'奔跑的骏马.ogg'],
  [500215,'达成成就.ogg'],
  [500306,'奔跑的骏马.ogg'],
  [500408,'拔剑_.ogg'],
  [500409,'奔跑的骏马.ogg'],
  [500416,'箭矢破空音.ogg']
]);
let bound = 0;
for (const [id,doc] of nodes) {
  if (!doc.sfx) continue;
  bound++;
  assert.ok(assets.has(doc.sfx), 'Broken reference at node '+id+': '+doc.sfx);
  if (!required.has(id)) console.warn('REVIEW extra story cue '+id+' => '+assets.get(doc.sfx));
}
for(const [id,suffix] of required) {
  const doc = nodes.get(id);
  assert.ok(doc && doc.sfx, 'Missing planned sound binding at '+id);
  const actual = assets.get(doc.sfx);
  assert.ok(actual && actual.endsWith('/'+suffix),
    'Wrong clip at '+id+': expected '+suffix+', actual '+actual);
  console.log('PASS '+id+' '+suffix);
}
const scene=file('Assets/Scenes/Guanduuuu.unity');
// Inspector-assigned AudioManager and DialogueSystem tracks are a second
// reference channel, independent of per-node sfxOnEnter.
const inspectorSoundRefs = [...scene.matchAll(/^\s*(\w+): \{fileID: 8300000,\s*guid: ([a-f0-9]{32}), type: 3\}/gm)]
  .filter(m => /^(?:bgm\w+|warBackgroundBGM|defaultButtonClick|valueChange\w*SFX|puzzle\w*SFX|alertSFX|errorSFX|achievementSFX|craftSuccessSFX|fireWuchaoSFX|digTunnel\w*SFX|paperTurnSFX|gridStepSFX)$/.test(m[1]));
assert.ok(inspectorSoundRefs.length > 25, 'Inspector audio fields unexpectedly disappeared');
for (const match of inspectorSoundRefs) {
  assert.ok(assets.has(match[2]), 'Broken scene audio ref '+match[1]+': '+match[2]);
}
console.log('PASS scene inspector sound references: '+inspectorSoundRefs.length+' validated');
const audio=file('Assets/AudioManager.cs');
const dialogue=file('Assets/Scripts/Managers/DialogueSystem.cs');
const animation=file('Assets/Scripts/Managers/FinalUIManager.cs');
assert.match(scene,/fireWuchaoSFX:\s*\{fileID: 0\}/,
  'Reserved flame SFX must be unbound until a real fire sound is licensed');
assert.match(scene,/bgmFireWuchao:\s*\{fileID: 8300000,\s*guid: 34a7a3c5b65b26b489743e59b4cf9fb1/,
  'Fire animation music is no longer wired');
assert.match(animation,/PlayBGM\(AudioManager\.Instance\.bgmFireWuchao,\s*false,\s*true\)/,
  'Fire animation music trigger is missing');
assert.match(dialogue,/PlaySFX\(currentNode\.sfxOnEnter\)/,
  'Story entry SFX is not wired to playback');
assert.match(audio,/duckUntil = Mathf\.Max\(duckUntil/,
  'BGM duck on narrative cue is missing');
const cuePaths=new Set([...required.values()]);
for (const suffix of cuePaths) {
  const match=[...assets.entries()].find(([,value])=>value.endsWith('/'+suffix));
  assert.ok(match,'Expected source clip missing: '+suffix);
  const meta=file(match[1]+'.meta');
  assert.match(meta,/preloadAudioData:\s*1/,'Short story cue should preload: '+suffix);
}
console.log('PASS sound reference integrity: '+bound+' bound nodes of '+nodes.size+
  ' total story nodes; '+assets.size+' physical audio assets');
console.log('PASS every required sound and fire-music boundary; audit pending gaps separately');
