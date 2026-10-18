const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.join(__dirname, '../..');
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const audio = read('Assets/AudioManager.cs');
const data = read('Assets/Scripts/Data/GuanduDialogueData.asset');
const arrowMeta = read('Assets/Audio/output_sfx/箭矢破空音.ogg.meta');
const home = read('Assets/Scripts/UI/HomeHubUI.cs');
const chat = read('Assets/Scripts/UI/HistorianChatUI.cs');
const finalUi = read('Assets/Scripts/Managers/FinalUIManager.cs');
let passed = 0;
function check(condition, message) {
  assert.ok(condition, message);
  passed++;
  console.log('PASS ' + message);
}
const arrow = data.match(/  - nodeId: 100104\n([\s\S]*?)(?=\n  - nodeId: 100105\n)/);
check(arrow && /sfxOnEnter: \{fileID: 8300000, guid: 97ac0f4b6f0c1724fa5f909a7270a76c, type: 3\}/.test(arrow[1]),
  'opening 100104 arrow scene binds existing arrow asset');
check(/preloadAudioData: 1/.test(arrowMeta), 'short first-arrow sound is preloaded');
check(/AudioManager.Instance.PlaySFX\(currentNode.sfxOnEnter\)/.test(read('Assets/Scripts/Managers/DialogueSystem.cs')),
  'node entry really plays assigned clip');
check(/sfxSource.spatialBlend = 0f/.test(audio) && /uiSource.spatialBlend = 0f/.test(audio),
  'UI and story sounds use 2D audio sources');
check(/GetFloat\("EffectsVolume", 0\.9f\)/.test(audio) &&
  /GetFloat\("BGMVolume", 0\.75f\)/.test(audio),
  'new installs use balanced BGM and effects defaults');
check(/void UpdateMusicMix\(\)/.test(audio) &&
  /currentBGMVolume \* transitionGain \* gameplayDuck \* effectDuck/.test(audio),
  'user volume, transition, game and transient duck remain independent');
check(/duckUntil = Mathf.Max\(duckUntil/.test(audio) && /Time\.unscaledTime < duckUntil/.test(audio),
  'narrative SFX automatically holds music duck during clip');
check(/public void PlaySFX\(AudioClip clip, float volume = 1f\)/.test(audio) &&
  /PlayPuzzleCorrect\(\) => PlaySFX\(puzzleCorrectSFX, 0\.95f\)/.test(audio),
  'one-shot cues are not multiplied by arbitrary low defaults');
check(/persistentHud = CreateUIObject\("PersistentHUD", uiRoot\)/.test(home) &&
  /persistentHud.AddComponent<GraphicRaycaster>\(\)/.test(home) &&
  /hudCanvas.overrideSorting = true/.test(home),
  'persistent UI is on top-level raycastable canvas');
check(/CreateIconHomeButton\(persistentHud.transform, "SettingsIconButton"/.test(home) &&
  /CreateIconHomeButton\(persistentHud.transform, "GlobalMuteButton"/.test(home),
  'gear and speaker are no longer children of hidden home panel');
check(/CreateSettingsOverlay\(persistentHud.transform\)/.test(home) &&
  /ToggleGlobalMute/.test(home) && /SetEffectsVolume\(value\)/.test(home),
  'global settings overlay and volume actions remain wired');
check(/launcherLayer = Object\("HistorianLauncherLayer", parent\)/.test(chat) &&
  /CreateLauncher\(panel.transform.root\)/.test(chat) &&
  /new Vector2\(\.075f, \.55f\)/.test(chat),
  'historian icon is on the root canvas at the left portrait');
check(/private bool ShouldShowLauncher\(\)/.test(chat) &&
  /LocalSaveManager.Instance.CurrentRunId/.test(chat) &&
  /if \(!ShouldShowLauncher\(\)\) \{ Close\(\); return; \}/.test(chat),
  'historian follows active runs even when gameplay panel is hidden');
check(/if \(gameInterfacePanel != null\) gameInterfacePanel.SetActive\(false\)/.test(finalUi),
  'baseline game still swaps active content panels (persistence must be separate)');
console.log('audio-hud-static-checks: ' + passed + '/' + passed);
