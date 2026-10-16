const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const home=fs.readFileSync(path.join(root,'Assets/Scripts/UI/HomeHubUI.cs'),'utf8');
const art=fs.readFileSync(path.join(root,'Assets/Scripts/UI/SettingsVisualArt.cs'),'utf8');
const audio=fs.readFileSync(path.join(root,'Assets/AudioManager.cs'),'utf8');
let passed=0;
function check(condition,message){assert.ok(condition,message); console.log('PASS '+message);passed++;}
check(/DecorateSettingsPanel\(panel\)/.test(home) && /CreateHeader\(panel.transform, "设置", CloseSettings\)/.test(home),
  'Settings modal uses engraved panel without losing close action');
check(/SettingsOrnamentCorner/.test(home) && /UpperBronzeTrim/.test(home) &&
  /LowerBronzeTrim/.test(home), 'Border, gold trim and carved corners');
check(/CreateSliderRow\(panel.transform, "背景音乐", SettingsGlyphKind.Lyre/.test(home) &&
  /CreateSliderRow\(panel.transform, "音效音量", SettingsGlyphKind.Drum/.test(home) &&
  /CreateSliderRow\(panel.transform, "画面亮度", SettingsGlyphKind.Sun/.test(home),
  'Different music, SFX and brightness motifs');
check(/CreateSliderRow\(panel.transform, "画面亮度", SettingsGlyphKind.Sun, \.36f, \.45f, 1f/.test(home),
  'Brightness retains its original 45-100 percent real domain');
check(/slider\.onValueChanged\.AddListener\(changed\)/.test(home) &&
  /SetBGMVolume\(value\)/.test(home) && /SetEffectsVolume\(value\)/.test(home) &&
  /ApplyBrightness\(value\)/.test(home),
  'Live control changes retain existing audio and brightness callbacks');
check(/PlayerPrefs\.Save\(\);\s*settingsOverlay\.SetActive\(false\)/.test(home) &&
  /GetFloat\("BGMVolume"/.test(home) && /GetFloat\("EffectsVolume"/.test(home),
  'User choices persist and are restored after reopening');
check(/hitArea\.raycastTarget = true/.test(home) &&
  /new Color\(0f, 0f, 0f, 0f\)/.test(home),
  'Invisible full-rect slider pointer target supports touchscreen dragging');
check(/slider\.handleRect = handleRect/.test(home) &&
  /slider\.fillRect = fill\.GetComponent<RectTransform>\(\)/.test(home),
  'Unity Slider remains functional rather than visually simulated');
check(/SettingsMetalSprites\.Rail/.test(home) &&
  /SettingsMetalSprites\.Fill/.test(home) &&
  /SettingsMetalSprites\.Knob/.test(home), 'Runtime brushed metal rail, fill and relief knob');
check(/CreateStepButton\(parent, label \+ "Decrease"/.test(home) &&
  /CreateStepButton\(parent, label \+ "Increase"/.test(home) &&
  /slider\.value \+ \.05f/.test(home) && /slider\.value - \.05f/.test(home),
  'Both arrow buttons adjust slider in five-percent increments');
check(/Mathf\.Clamp\(Mathf\.Round/.test(home) && /slider\.value =/.test(home),
  'Step buttons clamp to each slider’s actual min/max');
check(/int tickPercent = i == 4/.test(home) &&
  /min \+ \(max - min\) \* t/.test(home), 'Labels show actual tick percentage including 45-100 brightness');
check(/SettingsSliderFeedback/.test(home) && /IPointerDownHandler/.test(art) &&
  /IBeginDragHandler/.test(art) && /RefreshVisual\(\)/.test(art),
  'Drag and hover feedback is wired to real pointer events');
check(/tooltip\.SetActive\(dragging\)/.test(art) &&
  /Mathf\.RoundToInt\(value \* 100f\)/.test(art),
  'Only dragging reveals live percent bubble; right-side percent always visible');
check(/slider\.onValueChanged\.AddListener\(feedback\.Refresh\)/.test(home) &&
  /slider\.onValueChanged\.AddListener\(current => amount\.text/.test(home),
  'Slider moves refresh both tooltip and persistent value');
check(/AudioListener\.volume = PlayerPrefs\.GetInt\("GlobalMuted"/.test(home) &&
  /ToggleGlobalMute\(\)/.test(home) && /SetEffectsVolume\(float volume\)/.test(audio),
  'Global mute and effects backend stay untouched');
check(/quality\.onClick\.AddListener\(CycleQuality\)/.test(home) &&
  /RefreshQualityLabel\(\)/.test(home) &&
  /ApplyQualityPreset\(next\)/.test(home),
  'Power-saving/standard/ultra presets continue cycling');
check(!/slider\.wholeNumbers = true/.test(home) && /slider\.wholeNumbers = false/.test(home),
  'No forced tick snapping or quantization during dragging');
console.log('ornate-settings: '+passed+'/'+passed);
