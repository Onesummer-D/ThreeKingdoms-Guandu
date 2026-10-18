const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
const load=x=>fs.readFileSync(path.join(root,x),'utf8');
const manager=load('Assets/Scripts/Managers/FinalUIManager.cs');
const entry=load('Assets/Scripts/UI/PlayerIdentityUI.cs');
const report=load('Assets/Scripts/UI/BattleReportUI.cs');
const state=load('Assets/Scripts/UI/PlayerIdentityState.cs');
const tracker=load('Assets/Scripts/Managers/RunHistoryTracker.cs');
const analyzer=load('Assets/Scripts/UI/BattleReportAnalyzer.cs');
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
      entry.includes('idInput.caretColor')&&entry.includes('idInput.onSubmit.AddListener') &&
      entry.includes('idInput.Select();') && entry.includes('FocusInputWhenReady()') &&
      entry.includes('idInput.characterValidation = TMP_InputField.CharacterValidation.None') &&
      !entry.includes('onValidateInput ='),
      'TMP input waits for UI readiness, allows IME and supports Enter submission');
check(entry.includes('ShowEditor();') &&
  !entry.includes('CreateEditButton') &&
  !entry.includes('主将 ID：'),
  'Name confirmation is shown every launch without a menu edit button');
check(state.includes('ResetForNewLaunch') &&
  state.includes('RuntimeInitializeLoadType.SubsystemRegistration') &&
  state.includes('confirmedThisLaunch &&'),
  'Session gate resets even if Unity disables domain reload');
check(entry.includes('idInput.richText = true') &&
  entry.includes('idInput.isRichTextEditingAllowed = false') &&
  entry.includes('inputText.richText = true'),
  'Chinese IME composition remains visible without permitting markup edits');
check(entry.includes('Anchor(intro.rectTransform, new Vector2(.06f, .65f)') &&
  !entry.includes('new Vector2(.06f, .57f)'),
  'Introduction is visibly closer to the commander title');
check(!state.includes('PlayerPrefs.GetString')&&!state.includes('PlayerPrefs.Save()') &&
      !state.includes('UnityWebRequest') &&
      state.includes('currentId = string.Empty;'),
  'ID is session-only, reset on each restart and never persisted to device storage');
check(entry.includes('idInput.text = string.Empty;') &&
      !entry.includes('idInput.text = PlayerIdentityState.CurrentId') &&
      !entry.includes('请输入3–18位'),
  'Registration textbox is blank at every launch, with no old restrictive warning');
check(report.includes('CreatePosterSignature(canvasRect, topMargin + bodyHeight, signatureHeight)') &&
  report.includes('Image posterArt = posterPaper.AddComponent<Image>()'),
  'Export renderer appends one footer after the separately designed parchment page');
check(report.includes('PlayerIdentityState.CurrentId')&&report.includes('"主将 ID："'),
  'Poster footer uses the same local ID');
check(report.includes('gameEntryQrSprite != null')&&report.includes('BattleReport/GameEntryQR'),
  'QR drawn only after actual final URL QR Sprite is installed');
check(!report.includes('https://example.com')&&!report.includes('placeholderQR'),
  'No dummy or unscannable QR artwork is ever exported');
check(report.includes('copy.sizeDelta = new Vector2(0f, bodyHeight)')&&
      report.includes('float height = topMargin + bodyHeight + signatureHeight + bottomMargin') &&
      report.includes('copy.anchoredPosition = new Vector2(0f, -topMargin)') &&
      report.includes('StylePosterCopy(copy)'),
  'Export report retains its height while separate margins and an ink-on-parchment palette surround it');
check(entry.includes('"为这一局征程定下名号", 35f') &&
  !entry.includes('战绩海报将以此署名') &&
  !entry.includes('这不是联网账号'), 'Short, larger startup subtitle with no explanatory footer');
check(report.includes('GameEntryQRCodeReservedArea') &&
  report.includes('frameRect.sizeDelta = new Vector2(148f, 148f)') &&
  report.includes('上线后开放'), 'Visible fixed QR reservation even before a final URL asset exists');
check(!report.includes('战绩留影'), 'Poster footer no longer contains the duplicate slogan');
check(report.includes('evidenceDetailText') &&
  report.includes('EvidenceDetailOpenButton') &&
  report.includes('item.gameObject.SetActive(false)'), 'Evidence available in game but hidden in PNG output');
check(report.includes('AspectRatioFitter.AspectMode.EnvelopeParent') &&
  report.includes('Mask clippingMask') &&
  report.includes('Mathf.Clamp(cardHeight * .56f, 86f, 265f)'),
  'All report thumbnails share width, vary height, crop to rounded borders without distortion');
check(report.includes('CreateUIObject("EndingPortrait", card.transform)') &&
  report.includes('portraitImage.preserveAspect = true') &&
  !report.includes('CreateCardBadgeSlot(card.transform, "Ending"'),
  'Ending uses uncropped original circular Cao Cao portrait aligned with left badges');
check(manager.includes('RecordMiniGameUnavailable(reason)') &&
  manager.includes('FinishMiniGameTransition(true, nextNodeId, false)') &&
  manager.includes('if (recordCompletion && RunHistoryTracker.Instance != null)'),
  'Missing minigame UI may advance story but never record a fabricated victory');
check(tracker.includes('public void RecordMiniGameUnavailable(string reason)') &&
  tracker.includes('activeMiniGame = null;') &&
  tracker.includes('"ui-unavailable"'),
  'Recorder treats missing minigame UI as skipped with an audit event');
check(!analyzer.includes('case "slider": return "伏击布阵";') &&
  analyzer.includes('case "slider": return "诱敌兵力分配";'),
  'Slider is the bait troop-allocation choice rather than a fictitious skirmish game');
check(!report.includes('"谋议方式 · " + tendency.consultationLabel') &&
  report.includes('"谋议方式：" + tendency.consultationLabel') &&
  report.includes('CreateText(card.transform, tendency.consultationSummary, 29f'),
  'Counsel heading is detail-only and both poster summaries share font size 29');
check(analyzer.includes('report.evidenceNotes.Sort((a, b) => a.decisionSequence.CompareTo(b.decisionSequence))'),
  'Evidence is chronologically sorted by recorded decision sequence');
console.log('player-id-poster-integration: '+pass+'/'+pass);
