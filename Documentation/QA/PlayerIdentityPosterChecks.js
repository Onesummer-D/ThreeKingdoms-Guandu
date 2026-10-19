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
check(/public void StartGame\(\)[\s\S]{0,420}!PlayerIdentityState.HasId/.test(manager) &&
  manager.includes('playerIdentityUI.ShowEditor(ContinueStartAfterIdentity)'),
  'Start Game requests player name before the brief without a second menu click');
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
check(!/CreateIdentityOverlay\(canvasRoot\);\s*ShowEditor\(\)/.test(entry) &&
  entry.includes('CreateIdentityOverlay(canvasRoot);') &&
  entry.includes('overlay.SetActive(false);'),
  'Normal home menu appears before name registration');
check(manager.includes('playerIdentityUI.ShowEditor(ContinueStartAfterIdentity)') &&
  manager.includes('ContinueStartAfterIdentity()') &&
  manager.includes('ShowPlayerBrief(BeginNewRun)') &&
  entry.includes('continuation?.Invoke();'),
  'Start Game opens name then briefing, with one-shot continuation to departure');
check(!entry.includes('CreateEditButton') && !entry.includes('主将 ID：'),
  'No extra menu edit button or backend ID label');
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
check(report.includes('CreatePosterSignature(canvasRect, bodyHeight, signatureHeight)') &&
  report.includes('background.color = PanelColor;') &&
  !report.includes('CardBackgroundSkin.Apply(background, CardBackgroundSkin.Style.LongPoster)'),
  'Poster uses one continuous dark background and a distinct footer without duplicated ornament');
check(report.includes('PlayerIdentityState.CurrentId')&&report.includes('"主将 ID："'),
  'Poster footer uses the same local ID');
check(report.includes('Resources.Load<Texture2D>("CardSkins/PosterDark")') &&
      report.includes('bool decoratedPoster = posterBackgroundTexture != null') &&
      report.includes('background.sprite = temporaryPosterSprite') &&
      report.includes('PrepareIllustratedPosterContent(copy)') &&
      report.includes('cardFill.color = Color.clear') &&
      report.includes('posterSideInset > 0f ? Color.clear : PanelColor') &&
      report.includes('copy.sizeDelta = new Vector2(-posterSideInset * 2f, bodyHeight)'),
  'Optional dark-scroll export is full-bleed, safe-inset and avoids nested rectangular cards');
check(report.includes('const float safeSide = .12f') &&
  report.includes('const float safeTop = .15f') &&
  report.includes('const float safeBottom = .10f') &&
  report.includes('width = bodyWidth / (1f - 2f * safeSide)') &&
  report.includes('float requiredHeight = (bodyHeight + signatureHeight) /') &&
  report.includes('topInset = height * safeTop'),
  'Poster scales safe text and QR margins to the provided scroll artwork');
check(report.includes('缺少海报底图 PosterDark.png') &&
  report.includes('Debug.LogWarning("战绩海报未生成：" + missingArt)') &&
  report.includes('if (posterBackgroundTexture == null)') &&
  report.includes('yield break;'),
  'Export does not silently emit an old plain poster when PosterDark image is missing');
// Block acceptance while only PosterDark.png.meta has been committed: this
// check is intentionally red until the actual 1086×1448 PNG is added.
const posterAsset = path.join(root, 'Assets/Resources/CardSkins/PosterDark.png');
const posterInstalled = fs.existsSync(posterAsset);
check(posterInstalled, 'Real illustrated poster PNG is present (not only its .meta)');
if (posterInstalled) {
  const png = fs.readFileSync(posterAsset);
  check(png.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10])) &&
    png.readUInt32BE(16) === 1086 && png.readUInt32BE(20) === 1448,
    'PosterDark has the supplied 1086x1448 dimensions and a valid PNG signature');
}
check(report.includes('gameEntryQrSprite != null')&&report.includes('BattleReport/GameEntryQR'),
  'QR drawn only after actual final URL QR Sprite is installed');
check(!report.includes('https://example.com')&&!report.includes('placeholderQR'),
  'No dummy or unscannable QR artwork is ever exported');
check(report.includes('copy.sizeDelta = new Vector2(0f, bodyHeight)') &&
      report.includes('float height = bodyHeight + signatureHeight') &&
      report.includes('copy.anchorMin = new Vector2(0f, 1f)') &&
      report.includes('copy.anchorMax = new Vector2(1f, 1f)') &&
      report.includes('copy.anchoredPosition = Vector2.zero;') &&
      !report.includes('StylePosterCopy(copy)'),
  'Poster preserves the original measured chart and report width instead of shrinking the clone');
check(entry.includes('"为这一局征程定下名号", 35f') &&
  !entry.includes('战绩海报将以此署名') &&
  !entry.includes('这不是联网账号'), 'Short, larger startup subtitle with no explanatory footer');
check(report.includes('GameEntryQRCodeReservedArea') &&
  report.includes('frameRect.sizeDelta = new Vector2(148f, 148f)') &&
  report.includes('上线后开放'), 'Visible fixed QR reservation even before a final URL asset exists');
check(!report.includes('战绩留影'), 'Poster footer no longer contains the duplicate slogan');
check(report.includes('evidenceDetailText') &&
  report.includes('EvidenceDetailOpenButton') &&
  report.includes('foreach (Button action in copy.GetComponentsInChildren<Button>(true))') &&
  report.includes('action.gameObject.SetActive(false);'),
  'Interactive evidence and detail controls remain in game but are absent from the exported poster');
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
check(entry.includes('Anchor(fieldRect, new Vector2(.20f, .36f), new Vector2(.80f, .56f))') &&
  entry.includes('fieldBg.color = new Color(.97f, .95f, .87f, .97f)') &&
  entry.includes('idInput.caretColor = new Color(.24f, .20f, .17f, 1f)'),
  'Startup light input is centered inside its illustrated border with ink-colored input and caret');
const saved = load('Assets/Scripts/UI/SaveArchiveUI.cs');
const chat = load('Assets/Scripts/UI/HistorianChatUI.cs');
const home = load('Assets/Scripts/UI/HomeHubUI.cs');
const gallery = load('Assets/Scripts/UI/CampaignMapUI.cs');
check(![report, saved, chat, home, gallery].some(text => text.includes('ApplyInsetDarkCard')) &&
      report.includes('image.color = CardColor;') &&
      saved.includes('row.AddComponent<Image>().color = rowColor;'),
  'Small game cards, save rows and historian bubbles keep dark, rectangular, legible surfaces');

// Visual regression constraints from the 19 Oct Unity screenshots. These are
// structural checks: a Unity Editor screenshot is still needed for approval.
const brief = load('Assets/Scripts/UI/InviteCoCreationUI.cs');
const exit = load('Assets/Scripts/UI/GameplayExitUI.cs');
check(entry.includes('new Vector2(-22f, 0f)') &&
      entry.includes('foreach (Transform child in panel.transform)') &&
      savePlaceholder(),
  'Commander name card shifts its actual interactive contents left of the asymmetric ornament');
function savePlaceholder() {
  return saved.includes('title.rectTransform.offsetMin = new Vector2(110f, 0f)');
}
check(brief.includes('title.rectTransform.offsetMin = new Vector2(108f, -102f)'),
  '出征之前 label avoids the left decorative corner');
check(chat.includes('new Vector2(.075f, .55f)') &&
      chat.includes('new Vector2(196f, 196f)') &&
      chat.includes('new Vector2(.12f, .085f), new Vector2(.88f, .925f)') &&
      !chat.includes('Enter 发送 · Shift + Enter 换行'),
  'Historian launcher grows inside left sidebar; dialogue enlarged and centered with obsolete hotkey help removed');
check(chat.includes('GameObject actionRow = Object("SourceActionRow", bubble)') &&
      chat.includes('HorizontalLayoutGroup actionLayout') &&
      chat.includes('toggleSize.preferredWidth = gameRecord ? 268f : 210f') &&
      !chat.includes('sourceLabel + "（" + citations.Length + "）"'),
  'Historical source button is compact and right aligned instead of a full-width banner');
check(exit.includes('new Vector2(.245f, .20f)') &&
      exit.includes('new Vector2(.755f, .82f)') &&
      exit.includes('reviewButton = CreateButton(panel.transform, "本局复盘"') &&
      exit.includes('reportButton = CreateButton(panel.transform, "战绩报告"') &&
      exit.includes('manager.SetEndingShortcutsVisible(false)') &&
      manager.includes('public void OpenEndingReviewFromExit()') &&
      manager.includes('public void OpenBattleReportFromExit()'),
  'End-of-run exit/review/report controls share one large centered modal rather than spilling outside the frame');

console.log('player-id-poster-integration: '+pass+'/'+pass);
