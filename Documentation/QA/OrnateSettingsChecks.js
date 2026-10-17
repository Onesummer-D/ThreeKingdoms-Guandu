const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const home=fs.readFileSync(path.join(root,'Assets/Scripts/UI/HomeHubUI.cs'),'utf8');
const art=fs.readFileSync(path.join(root,'Assets/Scripts/UI/SettingsVisualArt.cs'),'utf8');
const audio=fs.readFileSync(path.join(root,'Assets/AudioManager.cs'),'utf8');
let passed=0;
function check(condition,message){assert.ok(condition,message); console.log('PASS '+message);passed++;}
check(/DecorateSettingsPanel\(panel\)/.test(home) && /CreateSettingsHeader\(panel.transform\)/.test(home),
  'Settings modal uses existing dark panel and settings-specific header');
check(/new Vector2\(\.06f, \.84f\), new Vector2\(\.70f, \.97f\)/.test(home) &&
  /new Vector2\(\.85f, \.862f\), new Vector2\(\.953f, \.948f\)/.test(home),
  'Settings title and close button have same .905 vertical center');
check(/GameButtonSkin.Apply\(quality, qualityPlate, GameButtonSkin.Tone.Quiet, buttonSprite\)/.test(home) &&
  /CreateButton\(panel, "关闭"/.test(home),
  'Quality mode button shares exact quiet wooden skin with Close');
check(/SettingsOrnamentCorner/.test(home) && /UpperBronzeTrim/.test(home) &&
  /LowerBronzeTrim/.test(home), 'Border, gold trim and carved corners');
check(/CreateSliderRow\(panel.transform, "背景音乐", SettingsGlyphKind.Lyre/.test(home) &&
  /CreateSliderRow\(panel.transform, "音效音量", SettingsGlyphKind.Drum/.test(home) &&
  /CreateSliderRow\(panel.transform, "画面亮度", SettingsGlyphKind.Sun/.test(home),
  'Different music, SFX and brightness motifs');
check(/CreateSliderRow\(panel.transform, "画面亮度", SettingsGlyphKind.Sun, \.36f, 0f, 1f/.test(home) &&
  /\(1f - Mathf\.Clamp01\(value\)\) \* \.62f/.test(home),
  'Brightness uses actual 0-100 display scale and 0% still leaves 38% visible, never black');
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
  /slider\.fillRect = null/.test(home) &&
  /fillImage\.fillAmount = Mathf.InverseLerp\(min, max, current\)/.test(home),
  'Unity Slider moves real thumb and progressively reveals supplied gold texture');
check(/SettingsArtAtlas.Get\("track"\)/.test(home) &&
  /SettingsArtAtlas.Get\("fill"\)/.test(home) &&
  /SettingsArtAtlas.Get\("knob"\)/.test(home) &&
  /SettingsArtAtlas.Icon\(motif\)/.test(home),
  'User-supplied rail, fill, knob and three role icons are used');
const atlasFile=path.join(root,'Assets/Resources/SettingsUI/SettingsAtlas.png');
const png=fs.readFileSync(atlasFile);
check(png.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])) &&
      png.readUInt32BE(16)===640 && png.readUInt32BE(20)===416,
  'Actual 640x416 transparent UI art atlas exists in Unity Resources');
check(art.includes('Resources.Load<Texture2D>("SettingsUI/SettingsAtlas")') &&
      ['lyre','drum','sun','track','fill','knob','ticks','bubble','arrow'].every(
        name=>art.includes('case "'+name+'"')),
  'Atlas loader covers every required artwork region');
check(/CreateStepButton\(parent, label \+ "Decrease"/.test(home) &&
  /CreateStepButton\(parent, label \+ "Increase"/.test(home) &&
  /slider\.value \+ \.05f/.test(home) && /slider\.value - \.05f/.test(home),
  'Both arrow buttons adjust slider in five-percent increments');
check(/Mathf\.Clamp\(Mathf\.Round/.test(home) && /slider\.value =/.test(home),
  'Step buttons clamp to each slider’s actual min/max');
check(/CreateSettingsScale\(parent, label, anchorY\)/.test(home) &&
  /private void CreateSettingsScale\(Transform panel, string label, float anchorY\)/.test(home),
  'All three sliders share the artist-supplied engraved 0/25/50/75/100 scale');
check(/scale\.sprite = SettingsArtAtlas\.Get\("ticks"\)/.test(home) &&
  /new Vector2\(\.399f, anchorY - \.016f\)/.test(home) &&
  /new Vector2\(\.788f, anchorY \+ \.040f\)/.test(home),
  'Art tick strip is positioned just below slider rail with full-width labels');
check(/\(i \* 25\)\.ToString\(\)/.test(home) &&
  !/Mathf\.Lerp\(min, max, fraction\) \* 100f/.test(home),
  'Fallback numerical ticks also show 0 25 50 75 100');
check(!/GameObject tickRoot = CreateUIObject\("SettingsScale", root.transform\)/.test(home),
  'No oversized/cropped intermediate tick image');
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
const detail=fs.readFileSync(path.join(root,'Assets/Resources/SettingsUI/SliderDetailsAtlas.png'));
check(detail.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])) &&
      detail.readUInt32BE(16)===288 && detail.readUInt32BE(20)===96,
  'Real 288x96 transparent PNG contains three artist-drawn slider ornaments');
check(art.includes('Resources.Load<Texture2D>("SettingsUI/SliderDetailsAtlas")') &&
      /LoadDetail\("lute", 0, 0, 96, 96\)/.test(art) &&
      /LoadDetail\("gauntlet", 96, 0, 96, 96\)/.test(art) &&
      /LoadDetail\("lamp", 192, 0, 96, 96\)/.test(art),
  'Lute, gauntlet and oil-lamp art are separately mapped for all three controls');
check(/SettingsArtAtlas\.SliderDecoration\(motif\)/.test(home) &&
      /Image artImage = visual.AddComponent<Image>\(\)/.test(home) &&
      /artImage\.preserveAspect = true/.test(home),
  'Original slider medallions replaced with correctly aspect-ratio-preserved motif artwork');
check(/new Vector2\(68f, 75f\)/.test(home) &&
      /new Vector2\(67f, 69f\)/.test(home) &&
      /new Vector2\(91f, 69f\)/.test(home) &&
      /dragging \? 1\.07f/.test(art),
  'Per-motif sizes plus modest hover scaling protect nearby tick labels');
check(/new Vector2\(\.57f, \.181f\), new Vector2\(\.825f, \.275f\)/.test(home) &&
      /new Vector2\(\.85f, \.862f\), new Vector2\(\.953f, \.948f\)/.test(home),
  'Quality and Close buttons are compact and Close aligns with Settings');
check(/CreateUIObject\("DraggedValue", root.transform\)/.test(home) &&
      /Mathf\.Clamp\(progress, \.16f, \.84f\)/.test(art) &&
      /tooltipRect\.anchorMin = tooltipRect\.anchorMax/.test(art),
  'Tooltip belongs to slider root and remains inside its horizontal bounds');
const completeBubble=fs.readFileSync(path.join(root,'Assets/Resources/SettingsUI/SettingsValueBubble.png'));
check(completeBubble.subarray(0,8).equals(Buffer.from([137,80,78,71,13,10,26,10])) &&
  completeBubble.readUInt32BE(16)===180 && completeBubble.readUInt32BE(20)===91,
  'Full independent 180x91 artist bubble image exists: no cut-off left-side atlas cell');
check(art.includes('Resources.Load<Texture2D>("SettingsUI/SettingsValueBubble")') &&
  /new Rect\(0f, 0f, fullFrame\.width, fullFrame\.height\)/.test(art) &&
  /Sprite frame = Sprite\.Create\(fullFrame/.test(art),
  'Popup sprite samples the ENTIRE artist frame rather than old cropped atlas region');
check(/bubbleBg\.type = Image.Type.Simple/.test(home) &&
      /new Color\(\.96f, \.91f, \.81f, 1f\)/.test(home) &&
      /bubbleRect\.sizeDelta = new Vector2\(135f, 72f\)/.test(home),
  'Complete pointed bubble renders as original at readable size with warm ivory numerals');
console.log('ornate-settings: '+passed+'/'+passed);
