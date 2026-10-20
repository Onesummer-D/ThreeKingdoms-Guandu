using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Independent, run-scoped historian chat. No scene wiring or API key in the client.</summary>
public sealed class HistorianChatUI : MonoBehaviour
{
    public static HistorianChatUI Instance { get; private set; }
    public bool IsOpen { get; private set; }
    private GameObject gameplayPanel;
    private GameObject launcher;
    private GameObject launcherLayer;
    private GameObject surface;
    private RectTransform content;
    private ScrollRect scroll;
    private TMP_InputField input;
    private TMP_Text status;
    private Button send;
    private readonly List<Button> suggestions = new List<Button>();
    private readonly string[] activeSuggestedQuestions = new string[3];
    // Three concise, chapter-scoped choices derived from the editorial guide.
    // Do not pre-suggest late-battle outcomes in early chapters.
    private static readonly string[][] ChapterSuggestedQuestions =
    {
        new[] { "曹操兵力有多悬殊？", "官渡为何重要？", "土山和霹雳车是真实的吗？" },
        new[] { "曹操为何议退许都？", "荀彧为何劝守官渡？", "袁绍的谋臣为何有分歧？" },
        new[] { "许攸为何转投曹操？", "许攸带来了什么情报？", "曹操为何敢相信许攸？" },
        new[] { "曹操夜袭带了多少人？", "荀攸、贾诩为何支持奇袭？", "徐晃、史涣烧粮车是怎么回事？" },
        new[] { "乌巢粮道为何关键？", "张郃为何主张救乌巢？", "IF 结局属于史实吗？" }
    };
    private readonly List<HistorianChatMessage> history = new List<HistorianChatMessage>();
    private HistorianAgentClient client;
    private TMP_FontAsset font;
    private Sprite icon;
    private bool busy;
    private bool ownsPause;
    private bool imeWasComposing;
#if UNITY_WEBGL && !UNITY_EDITOR
    // Unity's TMP input cannot reliably host browser Chinese/Japanese IME.
    // A native DOM textarea takes keyboard focus above this TMP field.
    [DllImport("__Internal")] private static extern void GuanduHistorianInputOpen(
        string value, float x, float y, float width, float height, float screenWidth, float screenHeight);
    [DllImport("__Internal")] private static extern int GuanduHistorianInputChanged();
    [DllImport("__Internal")] private static extern string GuanduHistorianInputRead();
    [DllImport("__Internal")] private static extern int GuanduHistorianInputWasSubmitted();
    [DllImport("__Internal")] private static extern int GuanduHistorianInputWasCancelled();
    [DllImport("__Internal")] private static extern void GuanduHistorianInputSetText(string value);
    [DllImport("__Internal")] private static extern void GuanduHistorianInputClose();

    private void OpenBrowserInput()
    {
        if (input == null || input.textViewport == null) return;
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        input.textViewport.GetWorldCorners(corners);
        Canvas canvas = input.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 low = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 high = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        GuanduHistorianInputOpen(input.text, low.x, low.y,
            Mathf.Max(1f, high.x - low.x), Mathf.Max(1f, high.y - low.y),
            Screen.width, Screen.height);
    }
#endif
    private float previousTimeScale;
    private int generation;
    private string runId = string.Empty;
    private readonly List<KeyValuePair<CanvasGroup, float>> coveredStoryText = new List<KeyValuePair<CanvasGroup, float>>();
    private readonly HistorianLocalKnowledge knowledge = new HistorianLocalKnowledge();
    private static readonly Color Paper = new Color(.95f, .92f, .83f, 1f);
    private static readonly Color Gold = new Color(.83f, .66f, .37f, 1f);
    private static readonly Color Muted = new Color(.73f, .80f, .79f, 1f);

    public void Initialize(GameObject panel)
    {
        if (surface != null || panel == null) return;
        Instance = this;
        gameplayPanel = panel;
        client = GetComponent<HistorianAgentClient>();
        if (client == null) client = gameObject.AddComponent<HistorianAgentClient>();
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF") ?? TMP_Settings.defaultFontAsset;
        icon = Resources.Load<Sprite>("Historian/AskHistorian");
        knowledge.Load();
        CreateLauncher(panel.transform.root);
        CreateSurface(panel.transform.root);
        ResetConversation();
    }

    private void CreateLauncher(Transform parent)
    {
        // Keep the entry outside GameInterfacePanel: the latter disappears
        // during background introductions, mini-games and transitions.
        launcherLayer = Object("HistorianLauncherLayer", parent);
        Anchors(launcherLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Canvas launcherCanvas = launcherLayer.AddComponent<Canvas>();
        launcherCanvas.overrideSorting = true;
        launcherCanvas.sortingOrder = 35;
        launcherLayer.AddComponent<GraphicRaycaster>();
        launcher = Object("AskHistorianIcon", launcherLayer.transform);
        RectTransform rect = launcher.GetComponent<RectTransform>();
        // Align below the character portrait along the left edge rather than
        // beneath the top-right resources. Reference coordinates are relative
        // to the root Canvas and remain stable across game panels.
        rect.anchorMin = rect.anchorMax = new Vector2(.055f, .38f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(170f, 170f);
        Image image = launcher.AddComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.color = icon != null ? Color.white : new Color(.11f, .17f, .20f, .9f);
        Button button = launcher.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Open);
        if (icon == null) Anchors(Text(launcher.transform, "问史官", 30f, Gold).rectTransform, Vector2.zero, Vector2.one);
        launcher.SetActive(false);
    }

    private void CreateSurface(Transform root)
    {
        surface = Object("HistorianChatCanvas", null);
        DontDestroyOnLoad(surface);
        Canvas canvas = surface.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = surface.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        surface.AddComponent<GraphicRaycaster>();
        GameObject backdrop = Object("Backdrop", surface.transform);
        Anchors(backdrop.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        backdrop.AddComponent<Image>().color = new Color(.015f, .025f, .03f, .18f);

        GameObject panel = Object("ChatWindow", surface.transform);
        // Keep the painted frame optically centered on the 1920x1080 canvas
        // and use most of the viewport to make historian answers readable.
        Anchors(panel.GetComponent<RectTransform>(), new Vector2(.12f, .085f), new Vector2(.88f, .925f));
        Image historianPanelImage = panel.AddComponent<Image>();
        historianPanelImage.color = new Color(.045f, .105f, .13f, .78f);
        CardBackgroundSkin.Apply(historianPanelImage, CardBackgroundSkin.Style.DarkPanel);
        // The ornate background now owns the frame; four extra straight edge
        // graphics previously appeared as a second, floating rectangle.

        GameObject headerIcon = Object("HistorianPortrait", panel.transform);
        Image portrait = headerIcon.AddComponent<Image>();
        portrait.sprite = icon;
        portrait.preserveAspect = true;
        portrait.color = icon != null ? Color.white : Color.clear;
        Anchors(portrait.rectTransform, new Vector2(.025f, .855f), new Vector2(.115f, .985f));
        TMP_Text title = Text(panel.transform, "问史官", 50f, Gold);
        Anchors(title.rectTransform, new Vector2(.13f, .92f), new Vector2(.74f, .98f));
        TMP_Text subtitle = Text(panel.transform, "聊聊官渡的故事", 30f, Muted);
        Anchors(subtitle.rectTransform, new Vector2(.13f, .86f), new Vector2(.81f, .92f));
        Button close = Button(panel.transform, "关闭", 31f);
        Anchors(close.GetComponent<RectTransform>(), new Vector2(.87f, .9f), new Vector2(.97f, .975f));
        close.onClick.AddListener(Close);

        GameObject scrollObject = Object("Conversation", panel.transform);
        Anchors(scrollObject.GetComponent<RectTransform>(), new Vector2(.055f, .255f), new Vector2(.945f, .84f));
        // Painted frame is optically weighted right; align reading content left.
        scrollObject.GetComponent<RectTransform>().anchoredPosition += new Vector2(-28f, 0f);
        scrollObject.AddComponent<Image>().color = new Color(.02f, .055f, .065f, .28f);
        GameObject viewport = Object("Viewport", scrollObject.transform);
        Anchors(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        viewport.AddComponent<RectMask2D>();
        content = Object("Messages", viewport.transform).GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1f);
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 14f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;
        GameObject track = Object("ScrollTrack", scrollObject.transform);
        Anchors(track.GetComponent<RectTransform>(), new Vector2(.992f, 0f), Vector2.one);
        track.AddComponent<Image>().color = new Color(.01f, .025f, .035f, .4f);
        GameObject handle = Object("Handle", track.transform);
        Anchors(handle.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(Gold.r, Gold.g, Gold.b, .6f);
        Scrollbar scrollbar = track.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        GameObject choices = Object("SuggestedQuestions", panel.transform);
        Anchors(choices.GetComponent<RectTransform>(), new Vector2(.055f, .185f), new Vector2(.945f, .24f));
        choices.GetComponent<RectTransform>().anchoredPosition += new Vector2(-28f, 0f);
        HorizontalLayoutGroup row = choices.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = true;
        for (int i = 0; i < activeSuggestedQuestions.Length; i++)
        {
            int capturedIndex = i;
            activeSuggestedQuestions[i] = ChapterSuggestedQuestions[0][i];
            Button chip = Button(choices.transform, activeSuggestedQuestions[i], 29f);
            chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            chip.onClick.AddListener(() => SubmitQuestion(activeSuggestedQuestions[capturedIndex]));
            suggestions.Add(chip);
        }

        GameObject inputObject = Object("QuestionInput", panel.transform);
        Anchors(inputObject.GetComponent<RectTransform>(), new Vector2(.055f, .055f), new Vector2(.805f, .17f));
        Image inputBg = inputObject.AddComponent<Image>();
        inputBg.color = new Color(.015f, .04f, .055f, .8f);
        GameObject textArea = Object("TextArea", inputObject.transform);
        Anchors(textArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(18f, 12f), new Vector2(-18f, -12f));
        textArea.AddComponent<RectMask2D>();
        TMP_Text inputText = Text(textArea.transform, "", 33f, Paper);
        Anchors(inputText.rectTransform, Vector2.zero, Vector2.one);
        // TMP uses <u>...</u> internally to underline Chinese/Japanese IME composition.
        // The common Text() helper disables rich text, which renders those tags literally.
        inputText.richText = true;
        inputText.enableWordWrapping = true;
        inputText.overflowMode = TextOverflowModes.Overflow;
        TMP_Text placeholder = Text(textArea.transform, "想问些什么？输入问题…", 32f, Muted);
        Anchors(placeholder.rectTransform, Vector2.zero, Vector2.one);
        input = inputObject.AddComponent<TMP_InputField>();
        input.targetGraphic = inputBg;
        input.textViewport = textArea.GetComponent<RectTransform>();
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.fontAsset = font;
        input.richText = true; // Render TMP's IME composition underline, not literal <u> tags.
        input.isRichTextEditingAllowed = false; // Player input must remain plain text.
        input.customCaretColor = true;
        input.caretColor = new Color(.98f, .86f, .58f, 1f);
        input.caretWidth = 3;
        input.caretBlinkRate = .85f;
        input.selectionColor = new Color(.83f, .66f, .37f, .38f);
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.characterLimit = 800;
#if UNITY_WEBGL && !UNITY_EDITOR
        // Prevent TMP from competing with the native DOM IME.
        input.readOnly = true;
#endif
        input.onValueChanged.AddListener(_ => RefreshControls());
        send = Button(panel.transform, "发送", 34f);
        Anchors(send.GetComponent<RectTransform>(), new Vector2(.82f, .055f), new Vector2(.945f, .17f));
        send.onClick.AddListener(() => SubmitQuestion(input.text));
        status = Text(panel.transform, "", 21f, Muted);
        Anchors(status.rectTransform, new Vector2(.055f, .005f), new Vector2(.945f, .048f));
        surface.SetActive(false);
    }

    public void Open()
    {
        if (surface == null || !ShouldShowLauncher()) return;
        SyncRun();
        if (IsOpen) return;
        IsOpen = true;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        CoverStoryText();
        surface.SetActive(true);
        if (launcher != null) launcher.SetActive(false);
        RefreshSuggestionsForCurrentChapter();
        RefreshControls();
        StartCoroutine(ScrollToLatest());
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenBrowserInput();
#else
        input.Select();
        input.ActivateInputField();
#endif
    }

    public void Close()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GuanduHistorianInputClose();
#endif
        IsOpen = false;
        if (input != null) input.DeactivateInputField();
        if (surface != null) surface.SetActive(false);
        foreach (KeyValuePair<CanvasGroup, float> entry in coveredStoryText)
            if (entry.Key != null) entry.Key.alpha = entry.Value;
        coveredStoryText.Clear();
        if (ownsPause) { Time.timeScale = previousTimeScale; ownsPause = false; }
        if (launcher != null) launcher.SetActive(ShouldShowLauncher());
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private bool ShouldShowLauncher()
    {
        // "Throughout the game" means active gameplay, including intro,
        // mini-games and ending screens; not the home menu before a run.
        if (gameplayPanel == null || LocalSaveManager.Instance == null ||
            string.IsNullOrEmpty(LocalSaveManager.Instance.CurrentRunId))
            return false;
        FinalUIManager ui = FinalUIManager.Instance;
        return ui == null || ui.startMenuPanel == null || !ui.startMenuPanel.activeInHierarchy;
    }

    private void RefreshSuggestionsForCurrentChapter()
    {
        int nodeId = DialogueSystem.Instance != null &&
            DialogueSystem.Instance.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId : 0;
        int chapter = CampaignChapterResolver.Resolve(nodeId);
        if (chapter < 0 || chapter >= ChapterSuggestedQuestions.Length) chapter = 0;
        string[] source = ChapterSuggestedQuestions[chapter];
        for (int i = 0; i < suggestions.Count && i < source.Length; i++)
        {
            activeSuggestedQuestions[i] = source[i];
            TMP_Text label = suggestions[i] != null
                ? suggestions[i].GetComponentInChildren<TMP_Text>() : null;
            if (label != null) label.text = source[i];
        }
    }

    private void CoverStoryText()
    {
        // Keep the scene and its artwork visible, but don't let the paused
        // dialogue text show through the chat composer or conversation.
        FinalUIManager ui = FinalUIManager.Instance;
        if (ui == null) return;
        CoverText(ui.dialogueText != null ? ui.dialogueText.gameObject : null);
        CoverText(ui.clickHint != null ? ui.clickHint.gameObject : null);
    }

    private void CoverText(GameObject target)
    {
        if (target == null) return;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        coveredStoryText.Add(new KeyValuePair<CanvasGroup, float>(group, group.alpha));
        group.alpha = 0f;
    }

    private void Update()
    {
        if (surface == null) return;
        SyncRun();
        if (launcher != null && !IsOpen) launcher.SetActive(ShouldShowLauncher());
        if (!IsOpen) return;
        if (!ShouldShowLauncher()) { Close(); return; }
#if UNITY_WEBGL && !UNITY_EDITOR
        // Poll committed text only. Enter within a Chinese IME candidate
        // selection is consumed by the DOM control, not as chat submit.
        if (GuanduHistorianInputChanged() != 0)
            input.text = GuanduHistorianInputRead();
        if (GuanduHistorianInputWasCancelled() != 0) { Close(); return; }
        if (GuanduHistorianInputWasSubmitted() != 0)
            SubmitQuestion(input.text);
#else
        bool composing = !string.IsNullOrEmpty(Input.compositionString);
        if (Input.GetKeyDown(KeyCode.Escape) && !composing && !imeWasComposing) { Close(); return; }
        if (input.isFocused && !composing && !imeWasComposing &&
            !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift) &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            SubmitQuestion(input.text);
        imeWasComposing = composing;
#endif
    }

    private void OnDisable() { Close(); }
    private void OnDestroy()
    {
        generation++;
        Close();
        if (launcherLayer != null) Destroy(launcherLayer);
        if (surface != null) Destroy(surface);
        if (Instance == this) Instance = null;
    }

    private void SyncRun()
    {
        string current = LocalSaveManager.Instance != null ? LocalSaveManager.Instance.CurrentRunId : string.Empty;
        if (current == runId) return;
        runId = current;
        ResetConversation();
    }

    public void ResetConversation()
    {
        generation++;
        if (client != null) client.CancelPending();
        busy = false;
        history.Clear();
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--) { content.GetChild(i).gameObject.SetActive(false); Destroy(content.GetChild(i).gameObject); }
        AddMessage("史官", "在下为你查阅官渡相关史料。你可以问战局、人物与粮道，也可以问游戏里哪些是历史、哪些是演绎。", false);
        if (input != null) input.text = string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
        GuanduHistorianInputSetText(string.Empty);
#endif
        if (status != null) status.text = string.Empty;
        RefreshControls();
    }

    private void SubmitQuestion(string value)
    {
        string question = (value ?? string.Empty).Trim();
        if (!IsOpen || busy || question.Length == 0) return;
        if (question.Length > 800) question = question.Substring(0, 800);
        busy = true;
        int requestGeneration = generation;
        HistorianChatMessage[] previous = history.GetRange(Math.Max(0, history.Count - 6), Math.Min(6, history.Count)).ToArray();
        for (int i = 0; i < previous.Length; i++)
        {
            string message = previous[i].content;
            previous[i] = new HistorianChatMessage(previous[i].role,
                message.Length > 1000 ? message.Substring(0, 1000) : message);
        }
        history.Add(new HistorianChatMessage("user", question));
        AddMessage("你", question, true);
        TMP_Text pendingText = AddMessage("史官", "正在查阅史料…", false);
        input.text = string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
        GuanduHistorianInputSetText(string.Empty);
#endif
        status.text = "史官正在查阅史料…";
        RefreshControls();
        string context = CurrentContext();
        HistorianRunEvidence run = CurrentRunEvidence();
        client.Ask(question, context, previous, run, response =>
        {
            if (generation != requestGeneration || pendingText == null) return;
            CompleteAnswer(pendingText, response);
        }, error =>
        {
            if (generation != requestGeneration || pendingText == null) return;
            CompleteAnswer(pendingText, IsRunReviewQuestion(question)
                ? LocalRunReview(run) : knowledge.Answer(question, previous, context), true);
        });
    }

    // Evidence references are for the validator, not part of the player's story.
    // Also clean older server responses, offline answers and restored history.
    private static string PlayerFacingText(string text)
    {
        return Regex.Replace(text ?? string.Empty,
            @"[\[【〔（(]\s*[A-Z]\d{2}\s*[\]】〕）)]", "",
            RegexOptions.IgnoreCase).Trim();
    }

    private void CompleteAnswer(TMP_Text body, HistorianAskResponse response,
        bool usedLocalFallback = false)
    {
        string playerAnswer = PlayerFacingText(response.answer);
        body.text = playerAnswer;
        history.Add(new HistorianChatMessage("assistant", playerAnswer));
        if (history.Count > 40) history.RemoveRange(0, history.Count - 40);
        AddSources(body.transform.parent, response.citations);
        busy = false;
        // A successful HTTP request may still return a server-side
        // grounded_fallback when the AI provider is slow or unavailable.
        // Only a failed UnityWebRequest means the local offline corpus was used.
        status.text = response.mode == "grounded_model" ? "依据史料回答" :
            response.mode == "run_evidence" ? "依据本局实际战况记录复盘（非历史史料）" :
            response.mode == "run_unavailable" ? "本局记录不足，暂不能判断" :
            response.mode == "grounded_refusal" ? "现有资料不足，你可以换个问题" :
            usedLocalFallback ? "联网暂不可用，已使用本地史料" :
            response.mode == "grounded_fallback" ? "依据史料摘录回答" :
            "依据史料回答";
        RefreshControls();
        if (IsOpen)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            input.Select();
            input.ActivateInputField();
#endif
            StartCoroutine(ScrollToLatest());
        }
    }

    private void RefreshControls()
    {
        if (send != null) send.interactable = !busy && input != null && !string.IsNullOrWhiteSpace(input.text);
        foreach (Button chip in suggestions) chip.interactable = !busy;
    }

    private string CurrentContext()
    {
        DialogueNode node = DialogueSystem.Instance != null ? DialogueSystem.Instance.CurrentNode : null;
        if (node == null) return "当前没有已显示剧情。";
        string text = Regex.Replace(node.dialogueText ?? string.Empty, "<[^>]*>", "");
        if (text.Length > 1400) text = text.Substring(0, 1400);
        return "当前已显示节点：" + node.nodeId + "\n游戏演绎文本（不能当作史实）：" + text;
    }


    // Export only already-recorded gameplay. Never send player identifiers,
    // unvisited dialogue nodes, future routes, or advisor participant names.
    private HistorianRunEvidence CurrentRunEvidence()
    {
        RunHistoryTracker tracker = RunHistoryTracker.Instance;
        if (tracker == null) return null;
        RunHistoryData recorded = tracker.ExportHistory();
        string expected = LocalSaveManager.Instance != null
            ? LocalSaveManager.Instance.CurrentRunId : string.Empty;
        if (recorded == null || string.IsNullOrWhiteSpace(expected) ||
            recorded.runId != expected || recorded.events == null || recorded.events.Count == 0)
            return null;

        HistorianRunEvidence result = new HistorianRunEvidence
        {
            completed = recorded.completed,
            integrityValid = recorded.dataIntegrityValid
        };
        if (recorded.resourceTimeline != null && recorded.resourceTimeline.Count > 0)
        {
            result.start = RunSnapshot(recorded.resourceTimeline[0]);
            result.current = RunSnapshot(recorded.resourceTimeline[recorded.resourceTimeline.Count - 1]);
        }
        // ResourceManager may be ahead of the last recorded snapshot by one frame.
        // Only use its live state when the run is still in progress.
        if (!recorded.completed && ResourceManager.Instance != null)
        {
            ResourceManager resources = ResourceManager.Instance;
            result.current = new HistorianRunSnapshot
            {
                troop = resources.GetTroop(), food = resources.GetFood(),
                strategy = resources.GetStrategy(), risk = resources.GetRisk()
            };
        }
        if (recorded.completed && recorded.ending != null)
        {
            result.ending = new HistorianRunEnding
            {
                id = recorded.ending.endingId,
                name = recorded.ending.endingName,
                reason = recorded.ending.reason,
                keyDecisions = recorded.ending.keyDecisionSequences != null
                    ? recorded.ending.keyDecisionSequences.ToArray() : new int[0]
            };
        }
        List<HistorianRunDecision> choices = new List<HistorianRunDecision>();
        if (recorded.decisions != null)
        {
            int first = Mathf.Max(0, recorded.decisions.Count - 40);
            for (int i = first; i < recorded.decisions.Count; i++)
            {
                DecisionRecordData d = recorded.decisions[i];
                if (d == null) continue;
                string label = Regex.Replace(d.optionText ?? string.Empty, "<[^>]*>", "");
                choices.Add(new HistorianRunDecision
                {
                    sequence = d.sequence, chapterIndex = d.chapterIndex,
                    optionText = label.Length > 110 ? label.Substring(0, 110) : label,
                    troopEffect = d.troopEffect, foodEffect = d.foodEffect,
                    strategyEffect = d.strategyEffect, riskEffect = d.riskEffect,
                    before = RunSnapshot(d.before), after = RunSnapshot(d.after)
                });
            }
        }
        result.decisions = choices.ToArray();
        List<HistorianRunMiniGame> games = new List<HistorianRunMiniGame>();
        if (recorded.miniGames != null)
        {
            int first = Mathf.Max(0, recorded.miniGames.Count - 12);
            for (int i = first; i < recorded.miniGames.Count; i++)
            {
                MiniGameTraceData item = recorded.miniGames[i];
                if (item == null) continue;
                games.Add(new HistorianRunMiniGame
                {
                    gameType = item.gameType, success = item.success,
                    failedAttempts = item.failedAttempts
                });
            }
        }
        result.miniGames = games.ToArray();
        return result;
    }

    private static HistorianRunSnapshot RunSnapshot(ResourceSnapshotData item)
    {
        if (item == null) return null;
        return new HistorianRunSnapshot
        {
            troop = item.troop, food = item.food,
            strategy = item.strategy, risk = item.risk
        };
    }

    private static bool IsRunReviewQuestion(string question)
    {
        return Regex.IsMatch(question ?? string.Empty,
            @"本局|这局|这一局|我的(选择|决策|兵力|粮草|计策|风险|战绩|资源|结局)|我(为什么|怎么)(会)?(输|赢|失败|获胜)|我(输了|赢了|失败了|获胜了)|哪一步|帮我复盘|给我复盘|复盘一下|刚才的选择|为什么(会)?(输|赢|失败|获胜)|怎么(会)?(输|赢|失败|获胜)");
    }

    private static HistorianAskResponse LocalRunReview(HistorianRunEvidence run)
    {
        if (run == null || !run.integrityValid)
            return new HistorianAskResponse
            {
                ok = true, mode = "run_unavailable",
                answer = "我目前没有可靠的本局决策记录，不能凭历史故事判断你为什么输或赢。",
                citations = new HistorianCitation[0]
            };
        StringBuilder text = new StringBuilder();
        if (run.completed && run.ending != null)
            text.Append("本局结局：「").Append(run.ending.name).Append("」。游戏评价：")
                .Append(run.ending.reason).Append("。[G01]\n");
        else
            text.Append("本局尚未出现结局，不能判断输赢原因。[G01]\n");
        if (run.decisions != null && run.decisions.Length > 0)
        {
            HistorianRunDecision latest = run.decisions[run.decisions.Length - 1];
            text.Append("最近的选择：").Append(latest.optionText).Append("。[G01]\n");
        }
        if (run.current != null)
            text.Append("已记录资源：兵力").Append(run.current.troop.ToString("0.#"))
                .Append("，粮草").Append(run.current.food.ToString("0.#"))
                .Append("，计策").Append(run.current.strategy.ToString("0.#"))
                .Append("，风险").Append(run.current.risk.ToString("0.#"))
                .Append("。[G01]\n");
        text.Append("这只是游戏内的实际记录，不能作为历史事实或唯一因果的证明。[G01]");
        return new HistorianAskResponse
        {
            ok = true, mode = "run_evidence", answer = text.ToString(),
            citations = new[]
            {
                new HistorianCitation
                {
                    id = "G01", title = "本局战况记录",
                    source = "本局实际发生的选择、资源变化与结局记录（非历史史料）"
                }
            }
        };
    }

    private TMP_Text AddMessage(string speaker, string value, bool fromPlayer)
    {
        GameObject bubble = Object(fromPlayer ? "PlayerMessage" : "HistorianMessage", content);
        bubble.AddComponent<Image>().color = fromPlayer ? new Color(.28f, .22f, .13f, .55f) : new Color(.065f, .15f, .175f, .55f);
        VerticalLayoutGroup layout = bubble.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 16, 18);
        layout.spacing = 8f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Text(bubble.transform, speaker, 28f, Gold);
        TMP_Text body = Text(bubble.transform, value, 35f, Paper);
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;
        body.lineSpacing = 5f;
        if (IsOpen) StartCoroutine(ScrollToLatest());
        return body;
    }

    private void AddSources(Transform bubble, HistorianCitation[] citations)
    {
        if (citations == null || citations.Length == 0) return;
        bool gameRecord = citations.Length == 1 && citations[0].id == "G01";
        bool gameDesign = citations.Length == 1 && citations[0].id == "D01";
        string sourceLabel = gameRecord ? "查看本局记录依据" :
            gameDesign ? "查看游戏设定说明" : "查看史料来源";
        // A source action is a small button at the bottom-right of the
        // message card, not an automatically stretched full-width banner.
        GameObject actionRow = Object("SourceActionRow", bubble);
        HorizontalLayoutGroup actionLayout = actionRow.AddComponent<HorizontalLayoutGroup>();
        actionLayout.childAlignment = TextAnchor.MiddleRight;
        actionLayout.childControlWidth = actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = actionLayout.childForceExpandHeight = false;
        actionRow.AddComponent<LayoutElement>().preferredHeight = 58f;
        Button toggle = Button(actionRow.transform, sourceLabel, 27f);
        LayoutElement toggleSize = toggle.gameObject.AddComponent<LayoutElement>();
        toggleSize.preferredWidth = gameRecord ? 268f : 210f;
        toggleSize.preferredHeight = 56f;
        StringBuilder lines = new StringBuilder();
        foreach (HistorianCitation citation in citations)
        {
            if (citation == null) continue;
            lines.Append(PlayerFacingText(citation.title)).Append("\n")
                .Append(PlayerFacingText(citation.source)).Append("\n\n");
        }
        TMP_Text details = Text(bubble, lines.ToString().Trim(), 22f, Muted);
        details.enableWordWrapping = true;
        details.overflowMode = TextOverflowModes.Overflow;
        details.gameObject.SetActive(false);
        toggle.onClick.AddListener(() =>
        {
            details.gameObject.SetActive(!details.gameObject.activeSelf);
            toggle.GetComponentInChildren<TMP_Text>().text = details.gameObject.activeSelf ? "收起来源" : sourceLabel;
            LayoutRebuilder.MarkLayoutForRebuild(content);
        });
    }

    private IEnumerator ScrollToLatest()
    {
        // TMP, nested layout groups and the auto-hiding scrollbar settle over
        // several rendered frames. Clear inertia after every settled rebuild.
        for (int frame = 0; frame < 4; frame++)
        {
            yield return null;
            if (!IsOpen || content == null || scroll == null) yield break;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 0f;
        }
    }

    private static GameObject Object(string name, Transform parent)
    {
        GameObject value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value;
    }

    private TMP_Text Text(Transform parent, string value, float size, Color color)
    {
        GameObject textObject = Object("Text", parent);
        textObject.SetActive(false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null) { text.font = font; text.fontSharedMaterial = font.material; }
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.richText = false;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = false;
        textObject.SetActive(true);
        return text;
    }

    private static void Edge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image edge = Object(name, parent).AddComponent<Image>();
        edge.color = new Color(Gold.r, Gold.g, Gold.b, .55f);
        edge.raycastTarget = false;
        Anchors(edge.rectTransform, min, max, offsetMin, offsetMax);
    }

    private Button Button(Transform parent, string label, float size)
    {
        GameObject value = Object(label + "Button", parent);
        Image image = value.AddComponent<Image>();
        image.color = new Color(.34f, .28f, .17f, .85f);
        Button button = value.AddComponent<Button>();
        GameButtonSkin.Apply(button, image, label);
        TMP_Text text = Text(value.transform, label, size, GameButtonSkin.LabelColor);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = size;
        Anchors(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        return button;
    }

    private static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        Anchors(rect, min, max, Vector2.zero, Vector2.zero);
    }
    private static void Anchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}

/// <summary>Same packaged knowledge as the service; works without a network connection.</summary>
public sealed class HistorianLocalKnowledge
{
    [Serializable] private sealed class Pack { public Card[] cards; }
    [Serializable] private sealed class Card
    {
        public string id, title, source, summary, boundary;
        public string[] tags;
    }
    private Card[] cards = new Card[0];
    public void Load()
    {
        TextAsset asset = Resources.Load<TextAsset>("Historian/corpus");
        if (asset == null) return;
        Pack pack = JsonUtility.FromJson<Pack>("{\"cards\":" + asset.text + "}");
        if (pack != null && pack.cards != null) cards = pack.cards;
    }

    public HistorianAskResponse Answer(string question, HistorianChatMessage[] history, string context)
    {
        string query = question ?? "";
        bool refersBack = Regex.IsMatch(query, @"他|她|它|他们|此人|此事|这件事|刚才|上面|上述|这一段|那个决定|这场");
        bool explicitSubject = Regex.IsMatch(query, @"曹操|孟德|袁绍|许攸|子远|荀彧|荀攸|贾诩|张郃|高览|官渡|乌巢|许都|粮草|粮道|兵力|发石车|霹雳车|地道");
        bool followUp = !(explicitSubject && !refersBack) &&
            (refersBack || Regex.IsMatch(query, @"^(那|那么|所以|还有|然后|为什么|为何|继续|展开|再说|接着)"));
        if (followUp)
        {
            if (history != null && history.Length >= 2)
                query += " " + history[history.Length - 2].content + " " + history[history.Length - 1].content;
            else query += " " + (context ?? "");
        }
        if (Regex.IsMatch(query, @"(精确|准确|具体).{0,10}(伤亡人数|死伤人数|伤亡数字|阵亡人数|死亡人数|分钟级|逐日兵力)|(每一天|每分钟|逐日).{0,8}(伤亡|人数|损失)"))
            return Refuse();

        List<Card> matches = new List<Card>();
        bool laterFate = Regex.IsMatch(question ?? "", @"(许攸|子远).{0,18}(后来|结局|下场|怎么死|为何死|被杀|被诛|许褚|死因)|(后来|结局|下场|被杀|被诛).{0,18}(许攸|子远)");
        foreach (Card card in cards)
        {
            if (card.id == "H10" && !laterFate) continue;
            if (Score(card, query) >= 8) matches.Add(card);
        }
        if (matches.Count == 0) return Refuse();
        matches.Sort((a, b) => {
            int c = Score(b, query).CompareTo(Score(a, query));
            return c != 0 ? c : string.CompareOrdinal(a.id, b.id);
        });

        StringBuilder answer = new StringBuilder();
        List<HistorianCitation> sources = new List<HistorianCitation>();
        // One directly matched card is more useful than two loosely related extracts.
        int limit = Math.Min(1, matches.Count);
        for (int i = 0; i < limit; i++)
        {
            Card card = matches[i];
            answer.Append(card.summary.Trim()).Append("[").Append(card.id).Append("]");
            sources.Add(new HistorianCitation { id = card.id, title = card.title, source = card.source });
        }
        return new HistorianAskResponse { ok = true, mode = "grounded_fallback",
            answer = answer.ToString().Trim(), citations = sources.ToArray() };
    }

    private static HistorianAskResponse Refuse()
    {
        return new HistorianAskResponse { ok = true, mode = "grounded_refusal",
            answer = "这道问题暂时没有匹配到可靠的史料卡。你可以问官渡的人物、粮草、战术，或者游戏改编了哪里。",
            citations = new HistorianCitation[0] };
    }

    private static int Score(Card card, string query)
    {
        if (card == null || string.IsNullOrEmpty(query)) return 0;
        string document = (card.title ?? "") + " " + (card.summary ?? "");
        int score = 0;
        if (card.tags != null) foreach (string tag in card.tags)
            if (!string.IsNullOrEmpty(tag) && tag.Length >= 2 &&
                tag != "游戏" && tag != "历史" &&
                query.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 12;
        // Lexical phrases, not isolated shared ideographs (三、史、人等).
        string stop = "什么|为什么|为什|怎么|如何|可以|请问|这个|那个|一下|知道|觉得|是否|是不是|故事|事情|我们|他们|时候";
        for (int i = 0; i < query.Length - 1; i++)
        {
            char a = query[i], b = query[i + 1];
            if (a < '\u4e00' || a > '\u9fff' || b < '\u4e00' || b > '\u9fff') continue;
            string pair = query.Substring(i, 2);
            if (stop.Contains(pair) && stop.Split('|').Length > 0 &&
                Array.Exists(stop.Split('|'), part => part == pair)) continue;
            if (document.Contains(pair)) score += 2;
        }
        return score;
    }
}
