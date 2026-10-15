const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, file), 'utf8');

const dialogue = read('Assets/Scripts/Managers/DialogueSystem.cs');
const director = read('Assets/Scripts/Managers/VisualDirector.cs');
const audio = read('Assets/AudioManager.cs');
const manager = read('Assets/Scripts/Managers/FinalUIManager.cs');

let count = 0;
function check(condition, label) {
  assert.ok(condition, label);
  console.log('PASS ' + label);
  count++;
}

const handle = dialogue.match(/public bool HandleOptionSelected\(int optionIndex\)([\s\S]*?)\n    public List<int> ExportVisitedNodeOrder/);
check(!!handle, 'Can inspect dialogue option acceptance');
const choice = handle[1];
check(/AudioManager\.Instance\.PlayUI\(AudioManager\.Instance\.defaultButtonClick\)/.test(choice),
  'Choice keeps its neutral confirmation click');
check(!/PlayValueChange|PlayError|PlayAlert|PlaySFX/.test(choice),
  'Choice does not issue any gain/loss/alarm/error outcome sound');
check(/ApplyResourceEffects\(selectedOption\)/.test(choice) &&
  /OnOptionSelected\?\.Invoke\(optionIndex, selectedOption\)/.test(choice),
  'Choice still commits resource effects and emits normal selection event');
check(/gain|loss|positive|negative/.test(choice) === false,
  'No resource sign classification remains in choice sound logic');

const apply = dialogue.match(/void ApplyResourceEffects\(DialogueOption option\)([\s\S]*?)\n    public void StartDialogue/);
check(!!apply && /ModifyTroop/.test(apply[1]) && /ModifyFood/.test(apply[1]) &&
  /ModifyStrategy/.test(apply[1]) && /ModifyRisk/.test(apply[1]),
  'All four resource effects are still applied');

const tier = director.match(/private void ApplyTierResponse\(VisualState target, bool emitEvent\)([\s\S]*?)\n    private void ApplySceneElements/);
check(!!tier, 'Can inspect visual pressure response');
check(!/PlayAlert|PlayError|PlayValueChange|PlaySFX|PlayUI/.test(tier[1]),
  'Crossing resource pressure thresholds never emits a negative sound');
check(/RunHistoryTracker\.Instance\.RecordSceneState/.test(tier[1]) &&
  /CameraShake\(target.cameraShake\)/.test(tier[1]),
  'Tier event recording and camera feedback still work');
check(/PulseResourceText\(changeEvent.resourceType\)/.test(director) &&
  /PulseEventFlash\(changeEvent\)/.test(director),
  'Actual numerical changes still produce existing visual feedback');

check(/public void PlayAlert\(\)/.test(audio) &&
  /public void PlayError\(\)/.test(audio),
  'Explicit failure sounds stay available for real failures');
check(/OnGridGameFinished\(bool success, int nextNodeId\)/.test(manager) &&
  /ModifyTroop\(-20\)/.test(manager),
  'Grid-game real failure and penalty logic are unchanged');

console.log('neutral-choice-audio-checks: ' + count + '/' + count);
