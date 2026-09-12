using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A player-facing story recap. It compresses the 204-node data graph into five
/// chapter cards and only reveals chapters reached in the current run.
/// </summary>
public sealed class CampaignMapUI : MonoBehaviour
{
    private sealed class ChapterDefinition
    {
        public readonly string title;
        public readonly string timeLocation;
        public readonly string storySummary;
        public readonly string impact;
        public readonly int anchorNodeId;
        public readonly int imageNodeId;
        public readonly int[] beatNodeIds;
        public readonly int minNodeId;
        public readonly int maxNodeId;

        public ChapterDefinition(string title, string timeLocation, string storySummary, string impact,
            int anchorNodeId, int imageNodeId, int[] beatNodeIds, int minNodeId, int maxNodeId)
        {
            this.title = title;
            this.timeLocation = timeLocation;
            this.storySummary = storySummary;
            this.impact = impact;
            this.anchorNodeId = anchorNodeId;
            this.imageNodeId = imageNodeId;
            this.beatNodeIds = beatNodeIds;
            this.minNodeId = minNodeId;
            this.maxNodeId = maxNodeId;
        }
    }

    private sealed class HistorianNote
    {
        public readonly string source;
        public readonly string record;
        public readonly string interpretation;
        public readonly string adaptation;

        public HistorianNote(string source, string record, string interpretation, string adaptation)
        {
            this.source = source;
            this.record = record;
            this.interpretation = interpretation;
            this.adaptation = adaptation;
        }
    }

    public static CampaignMapUI Instance { get; private set; }

    private static readonly ChapterDefinition[] Chapters =
    {
        new ChapterDefinition("第一幕 · 两军对峙", "建安五年 · 官渡开局",
            "袁绍大军压境，曹操在官渡以少守多，初战受挫。",
            "发石车、地道与诈败，三种破局方案摆在眼前。",
            1001, 100101, new[] { 100001, 100101, 100201, 100301 }, 1001, 199999),
        new ChapterDefinition("第二幕 · 官渡相持", "官渡前线 · 相持半年",
            "两军相持，粮草见底；坚守还是撤退，战局悬于一念。",
            "兵力与粮草同时告急，撤退与坚持都将改写后续路线。",
            2001, 200201, new[] { 200001, 200005, 200310, 200313 }, 2001, 299999),
        new ChapterDefinition("第三幕 · 许攸夜访", "建安五年 · 寒夜",
            "许攸夜访，乌巢粮仓情报成为破局关键。",
            "情报变成行动窗口，信任与误判只差一步。",
            3001, 300201, new[] { 300101, 300201, 300309, 300411, 300415 }, 3001, 399999),
        new ChapterDefinition("第四幕 · 奇袭部署", "官渡以北 · 出兵前夜",
            "奇袭乌巢前夜，兵力、风险与行军路线必须重新计算。",
            "战术部署与小游戏表现，将共同决定冒险能否成立。",
            4001, 400101, new[] { 400101, 400201 }, 4001, 499999),
        new ChapterDefinition("第五幕 · 乌巢决断", "乌巢 · 决战之夜",
            "乌巢起火，粮道与军心同时动摇，官渡进入终局。",
            "从一场冒险突袭走向不同结局，余波将由你的选择留下。",
            5001, 500201,
            new[]
            {
                500101, 500201,
                // 史实结局与 IF 线解锁后的代表性余波，各保留两张。
                500212, 500213,
                // 彩蛋触发节点：它本身是一段有独立画面的关键剧情，
                // 访问后应像其他战况片段一样留在第五幕回顾中。
                500215,
                500310, 500312,
                500318, 500319,
                500408, 500410,
                500415, 500417
            },
            5001, 599999)
    };

    private static readonly int[] EndingNodeIds =
    {
        200314, 300416, 500217, 500313, 500320, 500411, 500418
    };

    // The game pauses on these unlock-notification nodes before the final
    // blank terminal node is reached. They still represent a completed ending
    // from the player's perspective and must appear in the recap immediately.
    private static readonly int[] EndingUnlockNodeIds =
    {
        200309, 300410, 500211, 500309, 500317, 500407, 500414
    };

    private static readonly Dictionary<int, HistorianNote> HistorianNotes =
        new Dictionary<int, HistorianNote>
        {
            {
                1001,
                new HistorianNote(
                    "参考来源：陈寿《三国志·魏书·武帝纪》（通行译述）",
                    "袁绍南下后，曹操与其相持于官渡，兵力悬殊却未立即退让。",
                    "官渡开局的关键不是求快，而是在劣势中守住可反击的空间。",
                    "游戏把守、攻、奇袭拆成三条入口，让第一步改变后续资源与路线。")
            },
            {
                2001,
                new HistorianNote(
                    "参考来源：陈寿《三国志·魏书·武帝纪》（通行译述）",
                    "曹操与袁绍相拒官渡，连月不决，军粮逐渐成为胜负关键。",
                    "长期对峙考验的不是一时勇气，而是补给、军心和撤退秩序。",
                    "游戏用兵力、粮草和风险三项资源，把“坚守还是暂退”变成可感知的取舍。")
            },
            {
                3001,
                new HistorianNote(
                    "参考来源：陈寿《三国志·魏书·武帝纪》（通行译述）",
                    "许攸来奔，向曹操告知袁绍军粮囤积于乌巢，战局由此出现转机。",
                    "情报只有被信任并及时执行，才会从消息变成真正的战略机会。",
                    "游戏把许攸的态度与曹操的判断拆成分支，决定乌巢情报能否进入行动窗口。")
            },
            {
                4001,
                new HistorianNote(
                    "参考来源：陈寿《三国志·魏书·武帝纪》及后世官渡战事整理",
                    "曹操决定夜袭乌巢，行军隐蔽、军令统一和出兵节奏共同影响奇袭成败。",
                    "奇袭不是单纯押注速度，还要让路线、兵力和风险彼此匹配。",
                    "游戏用战术小游戏和资源门槛表现部署过程，让玩家为一次夜袭承担具体代价。")
            },
            {
                5001,
                new HistorianNote(
                    "参考来源：陈寿《三国志·魏书·武帝纪》（通行译述）",
                    "曹操夜袭乌巢并焚毁粮秣，袁绍军心动摇，官渡战局最终转向曹军。",
                    "决定胜负的往往不是某一个瞬间，而是此前积累的判断、补给与执行。",
                    "游戏将乌巢决断连接到多条结局线，呈现同一历史节点在不同选择下的分叉余波。")
            }
        };

    private readonly List<GameObject> chapterObjects = new List<GameObject>();
    private GameObject overlay;
    private GameObject mapButtonObject;
    private Action inviteAction;
    private TMP_Text progressText;
    private RectTransform storyContent;
    private GameObject recapPanel;
    private GameObject historianOverlay;
    private TMP_Text historianTitleText;
    private TMP_Text historianSourceText;
    private TMP_Text historianRecordLabelText;
    private TMP_Text historianRecordBodyText;
    private TMP_Text historianInterpretationLabelText;
    private TMP_Text historianInterpretationBodyText;
    private TMP_Text historianAdaptationLabelText;
    private TMP_Text historianAdaptationBodyText;
    private readonly Dictionary<int, int> selectedOptionIndices = new Dictionary<int, int>();
    private bool initialized;
    private bool isOpen;
    private bool endingReviewMode;
    private int recapLastVisitedCount = -1;
    private int recapLastCurrentNodeId = int.MinValue;
    private static TMP_FontAsset runtimeFont;
    private static bool runtimeFontLookupCompleted;

    /// <summary>
    /// Raised after either the normal recap or the ending-only recap closes.
    /// FinalUIManager uses it to restore the dedicated ending recap button
    /// without coupling the dynamically-created UI back to the scene.
    /// </summary>
    public event Action OnClosed;

    private static readonly Color OverlayColor = new Color(0.015f, 0.025f, 0.035f, 0.94f);
    private static readonly Color PanelColor = new Color(0.075f, 0.11f, 0.15f, 0.98f);
    private static readonly Color CardColor = new Color(0.11f, 0.16f, 0.20f, 0.98f);
    private static readonly Color LockedColor = new Color(0.34f, 0.39f, 0.40f, 0.66f);
    private static readonly Color VisitedColor = new Color(0.82f, 0.66f, 0.30f, 1f);
    private static readonly Color CurrentColor = new Color(0.42f, 0.85f, 0.80f, 1f);
    private static readonly Color TextPrimary = new Color(0.94f, 0.94f, 0.88f, 1f);
    private static readonly Color TextMuted = new Color(0.69f, 0.76f, 0.78f, 1f);
    private static readonly Color HistorianLabelColor = new Color(0.89f, 0.71f, 0.32f, 1f);

    public void Initialize(GameObject gameplayPanel)
    {
        if (initialized || gameplayPanel == null) return;
        Instance = this;
        initialized = true;

        CreateMapButton(gameplayPanel.transform);
        CreateOverlay(gameplayPanel.transform.root);

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown += HandleNodeShown;
            DialogueSystem.Instance.OnNodeTransition += HandleNodeTransition;
            DialogueSystem.Instance.OnOptionSelected += HandleOptionSelected;
        }
    }

    private void OnDestroy()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleNodeShown;
            DialogueSystem.Instance.OnNodeTransition -= HandleNodeTransition;
            DialogueSystem.Instance.OnOptionSelected -= HandleOptionSelected;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!initialized) return;
        if (Input.GetKeyDown(KeyCode.M)) ToggleMap();
        if (isOpen) SyncRecapIfStateChanged();
    }

    public void ToggleMap()
    {
        if (!initialized || overlay == null) return;
        // When the regular entry is intentionally hidden (main menu or an
        // ending screen), the M shortcut must not bypass that lifecycle and
        // reopen the ordinary recap behind the player's back.
        if (!isOpen && mapButtonObject != null && !mapButtonObject.activeSelf) return;
        if (isOpen) CloseMap();
        else OpenMap();
    }

    public void OpenMap()
    {
        if (!initialized || overlay == null) return;
        endingReviewMode = false;
        isOpen = true;
        overlay.SetActive(true);
        if (recapPanel != null) recapPanel.SetActive(true);
        if (historianOverlay != null) historianOverlay.SetActive(false);
        if (mapButtonObject != null) mapButtonObject.SetActive(false);
        RebuildStoryRecap();
    }

    /// <summary>
    /// Opens the same recap surface in ending mode. The ending screen calls
    /// this through its dedicated button so the ordinary in-game entry can
    /// remain hidden while the player reviews the completed route.
    /// </summary>
    public void OpenEndingReview()
    {
        if (!initialized || overlay == null) return;
        endingReviewMode = true;
        isOpen = true;
        overlay.SetActive(true);
        if (recapPanel != null) recapPanel.SetActive(true);
        if (historianOverlay != null) historianOverlay.SetActive(false);
        if (mapButtonObject != null) mapButtonObject.SetActive(false);
        RebuildStoryRecap();
    }

    public void CloseMap()
    {
        endingReviewMode = false;
        isOpen = false;
        recapLastVisitedCount = -1;
        recapLastCurrentNodeId = int.MinValue;
        if (overlay != null) overlay.SetActive(false);
        if (historianOverlay != null) historianOverlay.SetActive(false);
        if (recapPanel != null) recapPanel.SetActive(true);
        SetMapButtonVisible(true);
        OnClosed?.Invoke();
    }

    private void SyncRecapIfStateChanged()
    {
        if (DialogueSystem.Instance == null) return;

        int visitedCount = DialogueSystem.Instance.visitedNodeIds != null
            ? DialogueSystem.Instance.visitedNodeIds.Count
            : 0;
        int currentNodeId = DialogueSystem.Instance.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId
            : 0;
        if (visitedCount != recapLastVisitedCount || currentNodeId != recapLastCurrentNodeId)
            RebuildStoryRecap();
    }

    private void OpenHistorianNote(int anchorNodeId)
    {
        HistorianNote note;
        if (!isOpen || historianOverlay == null || !HistorianNotes.TryGetValue(anchorNodeId, out note)) return;

        if (recapPanel != null) recapPanel.SetActive(false);
        historianOverlay.SetActive(true);
        if (historianTitleText != null) historianTitleText.text = "史官注 · " + GetChapterTitle(anchorNodeId);
        if (historianSourceText != null) historianSourceText.text = note.source;
        if (historianRecordLabelText != null) historianRecordLabelText.text = "史书记载";
        if (historianRecordBodyText != null) historianRecordBodyText.text = note.record;
        if (historianInterpretationLabelText != null) historianInterpretationLabelText.text = "今天如何理解";
        if (historianInterpretationBodyText != null) historianInterpretationBodyText.text = note.interpretation;
        if (historianAdaptationLabelText != null) historianAdaptationLabelText.text = "游戏改编说明";
        if (historianAdaptationBodyText != null) historianAdaptationBodyText.text = note.adaptation;
    }

    private void CloseHistorianNote()
    {
        if (historianOverlay != null) historianOverlay.SetActive(false);
        if (isOpen && recapPanel != null) recapPanel.SetActive(true);
    }

    /// <summary>
    /// Explicitly controls the gameplay entry point. CloseMap intentionally
    /// restores it for normal play, while menu/ending flows can hide it
    /// without relying on hierarchy side effects.
    /// </summary>
    public void SetMapButtonVisible(bool visible)
    {
        if (mapButtonObject != null)
        {
            mapButtonObject.SetActive(visible);
            if (visible) mapButtonObject.transform.SetAsLastSibling();
        }
    }

    public int GetReachedEndingNodeId()
    {
        return FindReachedEndingNodeId(DialogueSystem.Instance?.visitedNodeIds);
    }

    public string GetReachedEndingLabel()
    {
        return GetEndingLabel(GetReachedEndingNodeId());
    }

    public string GetReachedEndingSummary()
    {
        return GetEndingFallbackSummary(GetReachedEndingNodeId());
    }

    public string GetReachedEndingEvaluation()
    {
        return GetEndingEvaluation(GetReachedEndingNodeId());
    }

    public Sprite GetReachedEndingSprite()
    {
        HashSet<int> visited = DialogueSystem.Instance?.visitedNodeIds;
        return ResolveEndingSprite(FindReachedEndingNodeId(visited), visited);
    }

    /// <summary>
    /// Registers the phase-four invite entry without exposing the recap
    /// hierarchy to the invite component. The button is only shown after at
    /// least one decision anchor has been reached.
    /// </summary>
    public void RegisterInviteAction(Action action)
    {
        inviteAction = action;
        // The invitation is a decision-point action. It is created beside
        // the live dialogue by InviteCoCreationUI, never inside the recap.
    }

    /// <summary>
    /// Lets a sibling runtime panel temporarily replace the recap surface
    /// while keeping the map open state and close event intact.
    /// </summary>
    public void SetRecapPanelVisible(bool visible)
    {
        if (recapPanel != null) recapPanel.SetActive(visible);
    }

    public bool TryBuildInviteSnapshot(out InviteSnapshotData snapshot)
    {
        snapshot = null;
        if (!initialized || DialogueSystem.Instance == null ||
            DialogueSystem.Instance.visitedNodeIds == null ||
            DialogueSystem.Instance.visitedNodeIds.Count == 0)
            return false;

        HashSet<int> visited = DialogueSystem.Instance.visitedNodeIds;
        int currentNodeId = DialogueSystem.Instance.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId
            : 0;
        int chapterIndex = GetCurrentChapterIndex(currentNodeId, visited);
        int latestAnchorIndex = GetLatestVisitedAnchorIndex(visited);
        if (chapterIndex < 0 || latestAnchorIndex < 0) return false;
        chapterIndex = Mathf.Max(chapterIndex, latestAnchorIndex);

        ChapterDefinition chapter = Chapters[chapterIndex];
        ResourceManager resources = ResourceManager.Instance;
        string selectedRoute = BuildSelectedRouteSummary(visited);
        snapshot = new InviteSnapshotData(
            chapterIndex,
            chapter.title,
            currentNodeId,
            selectedRoute,
            resources != null ? Mathf.RoundToInt(resources.GetTroop()) : 0,
            resources != null ? Mathf.RoundToInt(resources.GetFood()) : 0,
            resources != null ? Mathf.RoundToInt(resources.GetStrategy()) : 0,
            resources != null ? Mathf.RoundToInt(resources.GetRisk()) : 0);
        DialogueNode current = DialogueSystem.Instance.CurrentNode;
        snapshot.DecisionContext = "当前在剧情推进或结局回顾中，没有待选择的军议。可以邀请朋友讨论已走过的路线。";
        if (current != null && current.nodeId == chapter.anchorNodeId && current.options != null)
        {
            List<string> options = new List<string>();
            bool alreadyChosen = false;
            for (int i = 0; i < current.options.Count; i++)
            {
                DialogueOption option = current.options[i];
                if (option == null) continue;
                alreadyChosen |= IsOptionSelected(current, i, option, visited);
                options.Add((i + 1) + ". " + option.optionText);
            }
            if (!alreadyChosen && options.Count > 0)
                snapshot.DecisionContext = "正在考虑：" + current.dialogueText + "\n" + string.Join("\n", options);
        }
        snapshot.StoryRecap = BuildInviteStoryRecap(visited);
        return true;
    }

    public void InvokeInviteAction() { inviteAction?.Invoke(); }

    private string BuildInviteStoryRecap(HashSet<int> visited)
    {
        List<string> summaries = new List<string>();
        for (int i = 0; i < Chapters.Length; i++)
        {
            bool reached = visited.Contains(Chapters[i].anchorNodeId);
            if (!reached)
                for (int j = 0; j < Chapters[i].beatNodeIds.Length; j++)
                    if (visited.Contains(Chapters[i].beatNodeIds[j])) { reached = true; break; }
            if (reached)
                summaries.Add(CompactText(Chapters[i].storySummary, 52));
        }
        return summaries.Count == 0 ? "暂无已走剧情" : string.Join("\n", summaries.ToArray());
    }

    private void HandleNodeShown(int nodeId)
    {
        // The first intro node marks a fresh run. Clear the previous run's
        // choice markers before the new route starts writing its recap.
        if (nodeId == 100001 && DialogueSystem.Instance != null &&
            DialogueSystem.Instance.visitedNodeOrder.Count <= 1)
        {
            selectedOptionIndices.Clear();
        }
        if (isOpen) RebuildStoryRecap();
    }

    private void HandleNodeTransition(int fromNodeId, int toNodeId)
    {
        if (isOpen) RebuildStoryRecap();
    }

    private void HandleOptionSelected(int optionIndex, DialogueOption option)
    {
        DialogueNode anchor = DialogueSystem.Instance?.CurrentNode;
        if (anchor != null) selectedOptionIndices[anchor.nodeId] = optionIndex;
        if (isOpen) RebuildStoryRecap();
    }

    public List<SavedChoiceData> ExportSelectedChoices()
    {
        List<SavedChoiceData> result = new List<SavedChoiceData>();
        foreach (KeyValuePair<int, int> pair in selectedOptionIndices)
            result.Add(new SavedChoiceData(pair.Key, pair.Value));
        result.Sort((left, right) => left.anchorNodeId.CompareTo(right.anchorNodeId));
        return result;
    }

    public void RestoreSelectedChoices(IList<SavedChoiceData> savedChoices)
    {
        selectedOptionIndices.Clear();
        if (savedChoices != null)
        {
            for (int i = 0; i < savedChoices.Count; i++)
            {
                SavedChoiceData choice = savedChoices[i];
                if (choice == null || choice.optionIndex < 0) continue;
                DialogueNode anchor = DataLoader.Instance?.dialogueData?.GetNode(choice.anchorNodeId);
                if (anchor == null || anchor.options == null || choice.optionIndex >= anchor.options.Count) continue;
                selectedOptionIndices[choice.anchorNodeId] = choice.optionIndex;
            }
        }
        if (isOpen) RebuildStoryRecap();
    }

    private void CreateMapButton(Transform parent)
    {
        // Keep the button on the gameplay panel instead of under DialoguePanel.
        // FinalUIManager hides DialoguePanel on a terminal ending; parenting it
        // here keeps the recap reachable after the ending screen appears.
        Transform buttonParent = parent;
        mapButtonObject = CreateUIObject("CampaignMapButton", buttonParent);
        RectTransform rect = mapButtonObject.GetComponent<RectTransform>();
        // Match the empty wooden plaque area in the gameplay UI. These values
        // are converted from DialoguePanel space to its parent space. DialoguePanel
        // is offset by (-19, -26) in the scene, hence (215, 392) here.
        SetBottomLeft(rect, new Vector2(215f, 392f), new Vector2(238f, 62f));

        Image background = mapButtonObject.AddComponent<Image>();
        background.color = new Color32(112, 82, 55, 245);
        Button button = mapButtonObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ToggleMap);
        TMP_Text label = CreateText(mapButtonObject.transform, "剧情回顾", 40f, TextAlignmentOptions.Center, new Color(0.95f, 0.90f, 0.78f, 1f));
        Stretch(label.rectTransform);
    }

    private void CreateOverlay(Transform parent)
    {
        overlay = CreateUIObject("CampaignMapOverlay", parent);
        Stretch(overlay.GetComponent<RectTransform>());
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = OverlayColor;

        GameObject panel = CreateUIObject("CampaignRecapPanel", overlay.transform);
        recapPanel = panel;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.055f);
        panelRect.anchorMax = new Vector2(0.94f, 0.945f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;

        TMP_Text title = CreateText(panel.transform, "官渡战局 · 剧情回顾", 56f, TextAlignmentOptions.Left, TextPrimary);
        SetTopLeft(title.rectTransform, new Vector2(38f, -26f), new Vector2(1200f, 68f));
        progressText = CreateText(panel.transform, "", 30f, TextAlignmentOptions.Left, TextMuted);
        SetTopLeft(progressText.rectTransform, new Vector2(40f, -96f), new Vector2(1400f, 44f));

        Button closeButton = CreateButton(panel.transform, "关闭", new Vector2(180f, 72f), new Vector2(-30f, -26f), new Color(0.30f, 0.20f, 0.18f, 1f));
        closeButton.onClick.AddListener(CloseMap);

        GameObject scrollObject = CreateUIObject("StoryRecapScroll", panel.transform);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.025f, 0.035f);
        scrollRect.anchorMax = new Vector2(0.975f, 0.86f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(0.025f, 0.045f, 0.065f, 0.88f);

        GameObject viewportObject = CreateUIObject("Viewport", scrollObject.transform);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);
        viewportObject.AddComponent<RectMask2D>();

        GameObject contentObject = CreateUIObject("StoryContent", viewportObject.transform);
        storyContent = contentObject.GetComponent<RectTransform>();
        storyContent.anchorMin = new Vector2(0f, 1f);
        storyContent.anchorMax = new Vector2(1f, 1f);
        storyContent.pivot = new Vector2(0.5f, 1f);
        storyContent.anchoredPosition = Vector2.zero;
        storyContent.sizeDelta = new Vector2(0f, 1500f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = storyContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        CreateHistorianOverlay(overlay.transform);

        overlay.SetActive(false);
    }

    private void CreateHistorianOverlay(Transform parent)
    {
        historianOverlay = CreateUIObject("HistorianNoteOverlay", parent);
        Stretch(historianOverlay.GetComponent<RectTransform>());
        Image overlayImage = historianOverlay.AddComponent<Image>();
        overlayImage.color = new Color(0.015f, 0.025f, 0.035f, 0.90f);

        GameObject panel = CreateUIObject("HistorianNotePanel", historianOverlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.10f);
        panelRect.anchorMax = new Vector2(0.88f, 0.90f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;

        historianTitleText = CreateText(panel.transform, "史官注", 64f, TextAlignmentOptions.Left, TextPrimary);
        SetTopLeft(historianTitleText.rectTransform, new Vector2(40f, -30f), new Vector2(1200f, 80f));

        Button closeButton = CreateButton(panel.transform, "关闭", new Vector2(180f, 72f), new Vector2(-30f, -26f), new Color(0.30f, 0.20f, 0.18f, 1f));
        closeButton.onClick.AddListener(CloseHistorianNote);

        historianSourceText = CreateWrappedText(panel.transform, "", 30f, TextMuted);
        SetAnchored(historianSourceText.rectTransform, new Vector2(0.07f, 0.80f), new Vector2(0.93f, 0.87f));

        historianRecordLabelText = CreateText(panel.transform, "史书记载", 44f, TextAlignmentOptions.Left, HistorianLabelColor);
        SetAnchored(historianRecordLabelText.rectTransform, new Vector2(0.07f, 0.67f), new Vector2(0.93f, 0.77f));
        historianRecordBodyText = CreateWrappedText(panel.transform, "", 36f, TextPrimary);
        SetAnchored(historianRecordBodyText.rectTransform, new Vector2(0.07f, 0.54f), new Vector2(0.93f, 0.66f));

        historianInterpretationLabelText = CreateText(panel.transform, "今天如何理解", 44f, TextAlignmentOptions.Left, HistorianLabelColor);
        SetAnchored(historianInterpretationLabelText.rectTransform, new Vector2(0.07f, 0.41f), new Vector2(0.93f, 0.51f));
        historianInterpretationBodyText = CreateWrappedText(panel.transform, "", 36f, TextPrimary);
        SetAnchored(historianInterpretationBodyText.rectTransform, new Vector2(0.07f, 0.28f), new Vector2(0.93f, 0.40f));

        historianAdaptationLabelText = CreateText(panel.transform, "游戏改编说明", 44f, TextAlignmentOptions.Left, HistorianLabelColor);
        SetAnchored(historianAdaptationLabelText.rectTransform, new Vector2(0.07f, 0.15f), new Vector2(0.93f, 0.25f));
        historianAdaptationBodyText = CreateAdaptiveSingleLineText(panel.transform, "", 36f, 26f, TextPrimary);
        SetAnchored(historianAdaptationBodyText.rectTransform, new Vector2(0.07f, 0.02f), new Vector2(0.93f, 0.14f));

        historianOverlay.SetActive(false);
    }

    private void RebuildStoryRecap()
    {
        if (storyContent == null || DataLoader.Instance == null || DataLoader.Instance.dialogueData == null) return;

        for (int i = storyContent.childCount - 1; i >= 0; i--)
            Destroy(storyContent.GetChild(i).gameObject);
        chapterObjects.Clear();

        HashSet<int> visited = DialogueSystem.Instance != null ? DialogueSystem.Instance.visitedNodeIds : null;
        int currentNodeId = DialogueSystem.Instance?.CurrentNode?.nodeId ?? 0;
        int currentChapterIndex = GetCurrentChapterIndex(currentNodeId, visited);
        bool hasReachedFinalEnding = HasReachedFinalEnding(visited);
        bool hasReachedEnding = HasReachedEnding(visited);
        int displayIndex = 1;

        // Completed acts collapse to one summary card. Only the current act can
        // expand into several short beat cards as the player advances.
        for (int i = 0; i <= currentChapterIndex && i < Chapters.Length; i++)
        {
            ChapterDefinition chapter = Chapters[i];
            if (i < currentChapterIndex)
            {
                CreateChapterCard(chapter, currentNodeId, visited, displayIndex++, true);
            }
            else if (hasReachedFinalEnding)
            {
                // Keep the completed act compact, but retain the last two
                // representative beats so a terminal recap still shows the
                // moment immediately before the ending (including 500215).
                displayIndex = CreateFinalChapterCards(chapter, currentNodeId, visited, displayIndex);
            }
            else
            {
                displayIndex = CreateCurrentChapterCards(chapter, currentNodeId, visited, displayIndex);
            }
        }

        if (hasReachedEnding)
        {
            CreateEndingSummary(visited, displayIndex);
            displayIndex++;
            if (endingReviewMode)
                CreateEvaluationCard(visited, displayIndex);
        }
        float contentHeight = Mathf.Max(430f, 242f * chapterObjects.Count + 220f);
        storyContent.sizeDelta = new Vector2(0f, contentHeight);
        progressText.text = endingReviewMode && hasReachedEnding
            ? $"本局复盘 · 已走过 {currentChapterIndex + 1}/5 幕"
            : currentChapterIndex < 0
                ? "故事尚未展开 · 推进剧情后，这里会留下你真正走过的关键片段"
                : $"正在经历第 {currentChapterIndex + 1}/5 幕";

        recapLastVisitedCount = visited != null ? visited.Count : 0;
        recapLastCurrentNodeId = currentNodeId;
    }

    private int CreateCurrentChapterCards(ChapterDefinition chapter, int currentNodeId, HashSet<int> visited, int displayIndex)
    {
        CreateChapterCard(chapter, currentNodeId, visited, displayIndex++, false);
        return CreateVisitedBeatCards(chapter, currentNodeId, visited, displayIndex, int.MaxValue);
    }

    private int CreateFinalChapterCards(ChapterDefinition chapter, int currentNodeId, HashSet<int> visited, int displayIndex)
    {
        CreateChapterCard(chapter, currentNodeId, visited, displayIndex++, true);
        return CreateVisitedBeatCards(chapter, currentNodeId, visited, displayIndex, 2);
    }

    private int CreateVisitedBeatCards(ChapterDefinition chapter, int currentNodeId, HashSet<int> visited,
        int displayIndex, int maxCount)
    {
        if (chapter.beatNodeIds == null || visited == null) return displayIndex;

        List<int> visitedBeatIds = new List<int>();
        HashSet<int> addedBeatIds = new HashSet<int>();
        List<int> visitOrder = DialogueSystem.Instance?.visitedNodeOrder;
        if (visitOrder != null)
        {
            for (int i = 0; i < visitOrder.Count; i++)
            {
                int nodeId = visitOrder[i];
                if (IsConfiguredBeat(chapter, nodeId) && visited.Contains(nodeId) && addedBeatIds.Add(nodeId))
                    visitedBeatIds.Add(nodeId);
            }
        }

        // Compatibility fallback for a route restored without visit-order
        // data. The chapter definition still provides deterministic ordering.
        if (visitedBeatIds.Count == 0)
        {
            for (int i = 0; i < chapter.beatNodeIds.Length; i++)
            {
                int nodeId = chapter.beatNodeIds[i];
                if (IsConfiguredBeat(chapter, nodeId) && visited.Contains(nodeId) && addedBeatIds.Add(nodeId))
                    visitedBeatIds.Add(nodeId);
            }
        }

        int firstIndex = maxCount == int.MaxValue
            ? 0
            : Mathf.Max(0, visitedBeatIds.Count - maxCount);
        for (int i = firstIndex; i < visitedBeatIds.Count; i++)
        {
            int beatNodeId = visitedBeatIds[i];

            DialogueNode beat = DataLoader.Instance.dialogueData.GetNode(beatNodeId);
            if (beat == null) continue;
            CreateBeatCard(chapter, beat, currentNodeId, displayIndex++);
        }
        return displayIndex;
    }

    private static bool IsConfiguredBeat(ChapterDefinition chapter, int nodeId)
    {
        if (chapter == null || chapter.beatNodeIds == null) return false;
        if (nodeId == chapter.imageNodeId || nodeId == chapter.anchorNodeId) return false;
        for (int i = 0; i < chapter.beatNodeIds.Length; i++)
            if (chapter.beatNodeIds[i] == nodeId) return true;
        return false;
    }

    private void CreateChapterCard(ChapterDefinition chapter, int currentNodeId, HashSet<int> visited, int displayIndex, bool isSummary)
    {
        DialogueNode anchor = DataLoader.Instance.dialogueData.GetNode(chapter.anchorNodeId);
        if (anchor == null) return;
        GameObject card = CreateUIObject("ChapterCard_" + chapter.anchorNodeId, storyContent);
        chapterObjects.Add(card);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(0f, -24f - (displayIndex - 1) * 242f);
        cardRect.sizeDelta = new Vector2(0f, 220f);

        Image cardImage = card.AddComponent<Image>();
        bool isCurrentChapter = GetCurrentChapterIndex(currentNodeId) == GetCurrentChapterIndex(chapter.anchorNodeId);
        cardImage.color = isCurrentChapter && !isSummary ? new Color(0.12f, 0.21f, 0.23f, 1f) : CardColor;

        Sprite recapSprite = ResolveRecapSprite(chapter.imageNodeId, displayIndex - 1, visited);
        if (recapSprite != null)
        {
            GameObject imageObject = CreateUIObject("ChapterImage", card.transform);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            SetAnchored(imageRect, new Vector2(0.02f, 0.12f), new Vector2(0.205f, 0.88f));
            Image image = imageObject.AddComponent<Image>();
            image.sprite = recapSprite;
            image.preserveAspect = true;
        }

        string status = isSummary ? "已完成" : "正在经历";
        TMP_Text statusText = CreateText(card.transform, status, 22f, TextAlignmentOptions.Right, isSummary ? VisitedColor : CurrentColor);
        SetTopRight(statusText.rectTransform, new Vector2(-24f, -22f), new Vector2(150f, 30f));

        TMP_Text indexText = CreateText(card.transform, $"0{displayIndex}", 24f, TextAlignmentOptions.Center, VisitedColor);
        SetTopLeft(indexText.rectTransform, new Vector2(22f, -20f), new Vector2(50f, 28f));
        // Keep the act name visible as the primary heading so a player can
        // reconstruct the story even after forgetting individual scenes.
        // The time/location line remains as a quieter secondary label.
        TMP_Text chapterTitle = CreateText(card.transform, chapter.title, 30f, TextAlignmentOptions.Left, VisitedColor);
        SetAnchored(chapterTitle.rectTransform, new Vector2(0.235f, 0.78f), new Vector2(0.82f, 0.94f));
        TMP_Text kicker = CreateText(card.transform, chapter.timeLocation, 24f, TextAlignmentOptions.Left, TextMuted);
        SetAnchored(kicker.rectTransform, new Vector2(0.235f, 0.59f), new Vector2(0.82f, 0.75f));

        TMP_Text summaryText = CreateText(card.transform, CompactText(chapter.storySummary, 46), 32f, TextAlignmentOptions.Left, TextMuted);
        SetAnchored(summaryText.rectTransform, new Vector2(0.235f, 0.25f), new Vector2(0.97f, 0.56f));

        // Do not reveal the three decision routes during the opening
        // narration. The branch row becomes meaningful once the chapter's
        // actual decision anchor has been reached.
        if (!isSummary && visited != null && visited.Contains(chapter.anchorNodeId))
            CreateBranchCards(card.transform, anchor, visited);
        else if (endingReviewMode && isSummary && visited != null && visited.Contains(chapter.anchorNodeId))
            CreateChoiceSummary(card.transform, anchor, visited);

        if (visited != null && visited.Contains(chapter.anchorNodeId) && HistorianNotes.ContainsKey(chapter.anchorNodeId))
            CreateHistorianButton(card.transform, chapter.anchorNodeId);
    }

    private void CreateHistorianButton(Transform parent, int anchorNodeId)
    {
        GameObject buttonObject = CreateUIObject("HistorianButton_" + anchorNodeId, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetTopRight(rect, new Vector2(-24f, -60f), new Vector2(150f, 44f));

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.30f, 0.24f, 0.15f, 0.96f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        int capturedAnchorId = anchorNodeId;
        button.onClick.AddListener(() => OpenHistorianNote(capturedAnchorId));

        TMP_Text label = CreateText(buttonObject.transform, "史官注", 26f,
            TextAlignmentOptions.Center, TextPrimary);
        Stretch(label.rectTransform);
    }

    private void CreateBeatCard(ChapterDefinition chapter, DialogueNode beat, int currentNodeId, int displayIndex)
    {
        GameObject card = CreateUIObject("StoryBeat_" + beat.nodeId, storyContent);
        chapterObjects.Add(card);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = new Vector2(0f, -24f - (displayIndex - 1) * 242f);
        // Keep every recap card on the same 220px rhythm.  The previous
        // 200px beat cards left a visible 42px gap before the next card,
        // which made the ending section look detached from the story.
        cardRect.sizeDelta = new Vector2(0f, 220f);

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.095f, 0.145f, 0.18f, 0.98f);
        Sprite recapSprite = ResolveRecapSprite(beat.nodeId, displayIndex - 1, null);
        if (recapSprite != null)
        {
            GameObject imageObject = CreateUIObject("BeatImage", card.transform);
            SetAnchored(imageObject.GetComponent<RectTransform>(), new Vector2(0.02f, 0.12f), new Vector2(0.205f, 0.88f));
            Image image = imageObject.AddComponent<Image>();
            image.sprite = recapSprite;
            image.preserveAspect = true;
        }

        TMP_Text label = CreateText(card.transform, chapter.title + " · 战况片段", 26f, TextAlignmentOptions.Left, VisitedColor);
        SetAnchored(label.rectTransform, new Vector2(0.235f, 0.66f), new Vector2(0.82f, 0.90f));
        TMP_Text body = CreateText(card.transform, OneLineSummary(beat.dialogueText, 34), 32f, TextAlignmentOptions.Left, TextPrimary);
        SetAnchored(body.rectTransform, new Vector2(0.235f, 0.30f), new Vector2(0.96f, 0.62f));
        TMP_Text state = CreateText(card.transform, beat.nodeId == currentNodeId ? "当前" : "已发生", 22f,
            TextAlignmentOptions.Right, beat.nodeId == currentNodeId ? CurrentColor : TextMuted);
        SetTopRight(state.rectTransform, new Vector2(-24f, -22f), new Vector2(120f, 28f));
    }

    private void CreateBranchCards(Transform parent, DialogueNode anchor, HashSet<int> visited)
    {
        if (anchor.options == null || anchor.options.Count == 0) return;
        GameObject row = CreateUIObject("BranchRow", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetAnchored(rowRect, new Vector2(0.235f, 0.045f), new Vector2(0.98f, 0.22f));
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < anchor.options.Count; i++)
        {
            DialogueOption option = anchor.options[i];
            if (option == null) continue;

            bool reached = IsOptionSelected(anchor, i, option, visited);
            Color color = reached ? new Color(0.38f, 0.30f, 0.16f, 0.96f) : new Color(0.13f, 0.19f, 0.22f, 0.96f);
            GameObject branch = CreateUIObject("BranchCard_" + i, row.transform);
            LayoutElement branchLayout = branch.AddComponent<LayoutElement>();
            branchLayout.flexibleWidth = 1f;
            Image branchImage = branch.AddComponent<Image>();
            branchImage.color = color;

            string branchLabel = (reached ? "已选择 · " : "未探索 · ")
                + GetShortOptionLabel(anchor.nodeId, i, option.optionText);
            TMP_Text branchText = CreateText(branch.transform, branchLabel, 26f, TextAlignmentOptions.Left,
                reached ? TextPrimary : TextMuted);
            RectTransform branchTextRect = branchText.rectTransform;
            branchTextRect.anchorMin = Vector2.zero;
            branchTextRect.anchorMax = Vector2.one;
            branchTextRect.offsetMin = new Vector2(14f, 0f);
            branchTextRect.offsetMax = new Vector2(-14f, 0f);
            branchTextRect.pivot = new Vector2(0.5f, 0.5f);
            branchText.alignment = TextAlignmentOptions.MidlineLeft;
            branchText.verticalAlignment = VerticalAlignmentOptions.Middle;
            branchText.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Completed chapters collapse to one card in the ending recap. Keep the
    /// player's actual decision visible as one short line instead of expanding
    /// every branch again and reintroducing the dense graph problem.
    /// </summary>
    private void CreateChoiceSummary(Transform parent, DialogueNode anchor, HashSet<int> visited)
    {
        string selectedLabel = "未记录";
        if (anchor != null && anchor.options != null)
        {
            for (int i = 0; i < anchor.options.Count; i++)
            {
                DialogueOption option = anchor.options[i];
                if (option != null && IsOptionSelected(anchor, i, option, visited))
                {
                    selectedLabel = GetShortOptionLabel(anchor.nodeId, i, option.optionText);
                    break;
                }
            }
        }

        TMP_Text choiceText = CreateText(parent, "本局选择：" + selectedLabel, 26f,
            TextAlignmentOptions.Left, VisitedColor);
        SetAnchored(choiceText.rectTransform, new Vector2(0.235f, 0.045f), new Vector2(0.97f, 0.22f));
    }

    private bool IsOptionSelected(DialogueNode anchor, int optionIndex, DialogueOption option, HashSet<int> visited)
    {
        int selectedIndex;
        if (anchor != null && selectedOptionIndices.TryGetValue(anchor.nodeId, out selectedIndex))
            return selectedIndex == optionIndex;

        // Keep compatibility with routes opened before the click-event marker
        // was added. Most branches point directly to their first visited node.
        return option != null && option.nextNodeId > 0 && visited != null && visited.Contains(option.nextNodeId);
    }

    private void CreateEndingSummary(HashSet<int> visited, int displayIndex)
    {
        int reachedEndingNodeId = FindReachedEndingNodeId(visited);
        bool reachedEnding = reachedEndingNodeId > 0;
        Sprite endingSprite = ResolveRecapSprite(reachedEndingNodeId, displayIndex - 1, visited);

        GameObject ending = CreateUIObject("EndingSummary", storyContent);
        chapterObjects.Add(ending);
        RectTransform endingRect = ending.GetComponent<RectTransform>();
        endingRect.anchorMin = new Vector2(0f, 1f);
        endingRect.anchorMax = new Vector2(1f, 1f);
        endingRect.pivot = new Vector2(0.5f, 1f);
        endingRect.anchoredPosition = new Vector2(0f, -24f - (displayIndex - 1) * 242f);
        endingRect.sizeDelta = new Vector2(0f, 220f);
        Image endingImage = ending.AddComponent<Image>();
        endingImage.color = reachedEnding ? CardColor : new Color(0.09f, 0.13f, 0.16f, 0.96f);

        if (endingSprite != null)
        {
            GameObject imageObject = CreateUIObject("EndingImage", ending.transform);
            SetAnchored(imageObject.GetComponent<RectTransform>(), new Vector2(0.02f, 0.12f), new Vector2(0.205f, 0.88f));
            Image image = imageObject.AddComponent<Image>();
            image.sprite = endingSprite;
            image.preserveAspect = true;
        }

        TMP_Text indexText = CreateText(ending.transform, $"0{displayIndex}", 24f, TextAlignmentOptions.Center, VisitedColor);
        SetTopLeft(indexText.rectTransform, new Vector2(22f, -20f), new Vector2(50f, 28f));
        TMP_Text statusText = CreateText(ending.transform,
            IsEndingUnlockNode(reachedEndingNodeId) ? "已解锁" : "已抵达", 22f,
            TextAlignmentOptions.Right, VisitedColor);
        SetTopRight(statusText.rectTransform, new Vector2(-24f, -22f), new Vector2(150f, 30f));

        string title = reachedEnding
            ? "结局 · " + GetEndingLabel(reachedEndingNodeId)
            : "结局 · 尚未抵达";
        TMP_Text titleText = CreateText(ending.transform, title, 26f, TextAlignmentOptions.Left, reachedEnding ? VisitedColor : TextMuted);
        SetAnchored(titleText.rectTransform, new Vector2(0.235f, 0.66f), new Vector2(0.82f, 0.90f));
        string summary = reachedEnding
            ? GetEndingFallbackSummary(reachedEndingNodeId)
            : "走完任一条路线后，结局会在这里留下印记。你下一次可以从另一个决策点改写官渡的走向。";
        TMP_Text summaryText = CreateText(ending.transform, CompactText(summary, 46), 32f, TextAlignmentOptions.Left, TextPrimary);
        SetAnchored(summaryText.rectTransform, new Vector2(0.235f, 0.30f), new Vector2(0.97f, 0.62f));
    }

    private void CreateEvaluationCard(HashSet<int> visited, int displayIndex)
    {
        int reachedEndingNodeId = FindReachedEndingNodeId(visited);
        if (reachedEndingNodeId <= 0) return;

        GameObject evaluation = CreateUIObject("EndingEvaluation", storyContent);
        chapterObjects.Add(evaluation);
        RectTransform evaluationRect = evaluation.GetComponent<RectTransform>();
        evaluationRect.anchorMin = new Vector2(0f, 1f);
        evaluationRect.anchorMax = new Vector2(1f, 1f);
        evaluationRect.pivot = new Vector2(0.5f, 1f);
        evaluationRect.anchoredPosition = new Vector2(0f, -24f - (displayIndex - 1) * 242f);
        evaluationRect.sizeDelta = new Vector2(0f, 220f);

        Image evaluationImage = evaluation.AddComponent<Image>();
        evaluationImage.color = CardColor;

        Sprite endingSprite = ResolveRecapSprite(reachedEndingNodeId, displayIndex - 1, visited);
        if (endingSprite != null)
        {
            GameObject imageObject = CreateUIObject("EvaluationImage", evaluation.transform);
            SetAnchored(imageObject.GetComponent<RectTransform>(), new Vector2(0.02f, 0.12f), new Vector2(0.205f, 0.88f));
            Image image = imageObject.AddComponent<Image>();
            image.sprite = endingSprite;
            image.preserveAspect = true;
        }

        TMP_Text titleText = CreateText(evaluation.transform, "本局复盘 · 总体评价", 30f,
            TextAlignmentOptions.Left, VisitedColor);
        SetAnchored(titleText.rectTransform, new Vector2(0.235f, 0.66f), new Vector2(0.82f, 0.90f));
        TMP_Text routeText = CreateText(evaluation.transform, GetEndingLabel(reachedEndingNodeId), 24f,
            TextAlignmentOptions.Right, TextMuted);
        SetTopRight(routeText.rectTransform, new Vector2(-24f, -22f), new Vector2(260f, 30f));

        TMP_Text evaluationText = CreateText(evaluation.transform,
            CompactText(GetEndingEvaluation(reachedEndingNodeId), 46), 32f,
            TextAlignmentOptions.Left, TextPrimary);
        SetAnchored(evaluationText.rectTransform, new Vector2(0.235f, 0.30f), new Vector2(0.97f, 0.62f));
    }

    private Sprite ResolveEndingSprite(int reachedEndingNodeId, HashSet<int> visited)
    {
        if (reachedEndingNodeId <= 0 || DataLoader.Instance == null || DataLoader.Instance.dialogueData == null)
            return null;

        DialogueNode endingNode = DataLoader.Instance.dialogueData.GetNode(reachedEndingNodeId);
        Sprite endingSprite = endingNode != null ? endingNode.backgroundImage : null;
        if (IsUsableEndingImage(endingSprite)) return endingSprite;

        int[] fallbackNodeIds =
        {
            GetEndingUnlockNodeId(reachedEndingNodeId),
            GetEndingContinuationNodeId(reachedEndingNodeId)
        };
        for (int i = 0; i < fallbackNodeIds.Length; i++)
        {
            if (fallbackNodeIds[i] <= 0) continue;
            if (i == 1 && (visited == null || !visited.Contains(fallbackNodeIds[i]))) continue;
            DialogueNode fallbackNode = DataLoader.Instance.dialogueData.GetNode(fallbackNodeIds[i]);
            if (fallbackNode != null && IsUsableEndingImage(fallbackNode.backgroundImage))
                return fallbackNode.backgroundImage;
        }

        ChapterDefinition endingChapter = GetEndingChapter(reachedEndingNodeId);
        DialogueNode chapterImageNode = endingChapter != null
            ? DataLoader.Instance.dialogueData.GetNode(endingChapter.imageNodeId)
            : null;
        return chapterImageNode != null ? chapterImageNode.backgroundImage : null;
    }

    /// <summary>
    /// Recap cards intentionally rotate through the real dialogue backdrops.
    /// Many nodes share one parchment image, so choosing only each chapter's
    /// image node made the final recap look like the same card repeated. The
    /// preferred node is kept first, followed by distinct assets found in the
    /// current route and then the full authored dialogue set as a safe visual
    /// fallback.
    /// </summary>
    private Sprite ResolveRecapSprite(int preferredNodeId, int cardIndex, HashSet<int> visited)
    {
        List<Sprite> candidates = new List<Sprite>();
        DialogueDataSO data = DataLoader.Instance != null ? DataLoader.Instance.dialogueData : null;
        if (data == null) return null;

        DialogueNode preferred = data.GetNode(preferredNodeId);
        AddDistinctSprite(candidates, preferred != null ? preferred.backgroundImage : null);

        if (visited != null && DialogueSystem.Instance != null &&
            DialogueSystem.Instance.visitedNodeOrder != null)
        {
            for (int i = 0; i < DialogueSystem.Instance.visitedNodeOrder.Count; i++)
            {
                int nodeId = DialogueSystem.Instance.visitedNodeOrder[i];
                if (!visited.Contains(nodeId)) continue;
                DialogueNode node = data.GetNode(nodeId);
                AddDistinctSprite(candidates, node != null ? node.backgroundImage : null);
            }
        }

        if (data.nodes != null)
        {
            for (int i = 0; i < data.nodes.Count; i++)
                AddDistinctSprite(candidates, data.nodes[i] != null ? data.nodes[i].backgroundImage : null);
        }

        if (candidates.Count == 0) return null;
        int index = Mathf.Abs(cardIndex) % candidates.Count;
        return candidates[index];
    }

    private static void AddDistinctSprite(List<Sprite> sprites, Sprite candidate)
    {
        if (candidate != null && !sprites.Contains(candidate)) sprites.Add(candidate);
    }

    private static int GetCurrentChapterIndex(int currentNodeId)
    {
        return CampaignChapterResolver.Resolve(currentNodeId);
    }

    private static int GetCurrentChapterIndex(int currentNodeId, HashSet<int> visited)
    {
        int chapterIndex = GetCurrentChapterIndex(currentNodeId);
        if (visited == null) return chapterIndex;

        foreach (int visitedNodeId in visited)
            chapterIndex = Mathf.Max(chapterIndex, GetCurrentChapterIndex(visitedNodeId));
        return chapterIndex;
    }

    private static string GetChapterTitle(int anchorNodeId)
    {
        for (int i = 0; i < Chapters.Length; i++)
            if (Chapters[i].anchorNodeId == anchorNodeId) return Chapters[i].title;
        return "关键决策";
    }

    private static bool HasReachedEnding(HashSet<int> visited)
    {
        return FindReachedEndingNodeId(visited) > 0;
    }

    private static bool HasReachedFinalEnding(HashSet<int> visited)
    {
        if (visited == null) return false;
        for (int i = 0; i < EndingNodeIds.Length; i++)
            if (visited.Contains(EndingNodeIds[i])) return true;
        return false;
    }

    private static int FindReachedEndingNodeId(HashSet<int> visited)
    {
        if (visited == null) return 0;
        for (int i = 0; i < EndingNodeIds.Length; i++)
            if (visited.Contains(EndingNodeIds[i])) return EndingNodeIds[i];
        for (int i = 0; i < EndingUnlockNodeIds.Length; i++)
            if (visited.Contains(EndingUnlockNodeIds[i])) return EndingUnlockNodeIds[i];
        return 0;
    }

    private static bool IsEndingUnlockNode(int nodeId)
    {
        for (int i = 0; i < EndingUnlockNodeIds.Length; i++)
            if (EndingUnlockNodeIds[i] == nodeId) return true;
        return false;
    }

    private static int GetEndingUnlockNodeId(int nodeId)
    {
        switch (nodeId)
        {
            case 200309:
            case 200314: return 200309;
            case 300410:
            case 300416: return 300410;
            case 500211:
            case 500217: return 500211;
            case 500309:
            case 500313: return 500309;
            case 500317:
            case 500320: return 500317;
            case 500407:
            case 500411: return 500407;
            case 500414:
            case 500418: return 500414;
            default: return 0;
        }
    }

    private static bool IsGenericEndingPlaceholder(Sprite sprite)
    {
        if (sprite == null) return false;
        string spriteName = sprite.name ?? string.Empty;
        return spriteName.IndexOf("失败if", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsUsableEndingImage(Sprite sprite)
    {
        return sprite != null && !IsGenericEndingPlaceholder(sprite);
    }

    private static int GetEndingContinuationNodeId(int nodeId)
    {
        switch (nodeId)
        {
            case 200309:
            case 200314: return 200310;
            case 300410:
            case 300416: return 300411;
            case 500211:
            case 500217: return 500212;
            case 500309:
            case 500313: return 500310;
            case 500317:
            case 500320: return 500318;
            case 500407:
            case 500411: return 500408;
            case 500414:
            case 500418: return 500415;
            default: return 0;
        }
    }

    private static ChapterDefinition GetEndingChapter(int nodeId)
    {
        if (nodeId == 200309 || nodeId == 200314) return Chapters[1];
        if (nodeId == 300410 || nodeId == 300416) return Chapters[2];
        if (nodeId >= 500000) return Chapters[4];
        return null;
    }

    private static string GetEndingLabel(int nodeId)
    {
        switch (nodeId)
        {
            case 200309:
            case 200314: return "IF线1 · 半途而废";
            case 300410:
            case 300416: return "IF线2 · 错失良机";
            case 500211:
            case 500217: return "史实结局 · 胜利";
            case 500309:
            case 500313: return "IF线3 · 元气大伤";
            case 500317:
            case 500320: return "IF线4 · 以退为进";
            case 500407:
            case 500411: return "IF线5 · 完美";
            case 500414:
            case 500418: return "IF线6 · 惨败";
            default: return "你留下的历史回响";
        }
    }

    private static string GetEndingFallbackSummary(int nodeId)
    {
        switch (nodeId)
        {
            case 200309:
            case 200314: return "撤退失序，官渡防线崩溃，曹军被迫退回许都。";
            case 300410:
            case 300416: return "许攸情报未被采纳，粮草耗尽，曹军错失翻盘机会。";
            case 500211:
            case 500217: return "乌巢焚毁、袁绍败退，官渡之战以曹操胜利告终。";
            case 500309:
            case 500313: return "乌巢未能彻底摧毁，曹军两线消耗后元气大伤。";
            case 500317:
            case 500320: return "保存主力换取喘息，官渡未决，曹军仍保有再战筹码。";
            case 500407:
            case 500411: return "直取袁绍本阵成功，北方统一进程提前完成。";
            case 500414:
            case 500418: return "高风险突击失利，曹军全线溃败，北方统一遥遥无期。";
            default: return "本局选择已抵达结局节点。";
        }
    }

    private static string GetEndingEvaluation(int nodeId)
    {
        switch (nodeId)
        {
            case 200309:
            case 200314: return "你保全了眼前兵力，却让撤退失去秩序；谨慎也需要一条可执行的退路。";
            case 300410:
            case 300416: return "许攸带来的机会没有变成行动；战场上的犹豫，往往比冒险更昂贵。";
            case 500211:
            case 500217: return "你以坚守换来反击窗口，再用奇袭击穿粮道；胜利来自耐心与时机。";
            case 500309:
            case 500313: return "你击中了乌巢，却没有摧毁袁军意志；局部胜利仍要计算全局代价。";
            case 500317:
            case 500320: return "你保住主力，也保住再战可能；未决有时是在为下一次选择积蓄筹码。";
            case 500407:
            case 500411: return "你把情报、兵力与风险压在同一条线上，让奇袭直达袁绍本阵。";
            case 500414:
            case 500418: return "你把胜负押在高风险突击上，却没有为失手留下缓冲；勇气也需要边界。";
            default: return "这条路线留下了属于你的历史回响。";
        }
    }

    private static string CompactText(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "暂无剧情摘要";
        string compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        while (compact.Contains("  ")) compact = compact.Replace("  ", " ");
        if (compact.Length <= maxLength) return compact;
        return compact.Substring(0, Math.Max(0, maxLength - 1)) + "…";
    }

    private static string OneLineSummary(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "暂无剧情摘要";
        string compact = value.Replace("\r", " ").Replace("\n", " ").Trim();
        while (compact.Contains("  ")) compact = compact.Replace("  ", " ");

        int sentenceEnd = compact.IndexOfAny(new[] { '。', '！', '？', '；' });
        if (sentenceEnd >= 0 && sentenceEnd + 1 <= maxLength)
            compact = compact.Substring(0, sentenceEnd + 1);
        return CompactText(compact, maxLength);
    }

    private static int GetLatestVisitedAnchorIndex(HashSet<int> visited)
    {
        if (visited == null) return -1;
        int latestIndex = -1;
        for (int i = 0; i < Chapters.Length; i++)
            if (visited.Contains(Chapters[i].anchorNodeId)) latestIndex = i;
        return latestIndex;
    }

    private string BuildSelectedRouteSummary(HashSet<int> visited)
    {
        if (visited == null) return "暂未记录";

        List<string> routeLabels = new List<string>();
        for (int chapterIndex = 0; chapterIndex < Chapters.Length; chapterIndex++)
        {
            ChapterDefinition chapter = Chapters[chapterIndex];
            if (!visited.Contains(chapter.anchorNodeId)) continue;

            DialogueNode anchor = DataLoader.Instance?.dialogueData?.GetNode(chapter.anchorNodeId);
            string selectedLabel = "未记录";
            if (anchor != null && anchor.options != null)
            {
                for (int optionIndex = 0; optionIndex < anchor.options.Count; optionIndex++)
                {
                    DialogueOption option = anchor.options[optionIndex];
                    if (option != null && IsOptionSelected(anchor, optionIndex, option, visited))
                    {
                        selectedLabel = GetShortOptionLabel(anchor.nodeId, optionIndex, option.optionText);
                        break;
                    }
                }
            }
            routeLabels.Add(selectedLabel);
        }

        if (routeLabels.Count == 0) return "暂未记录";
        return string.Join(" / ", routeLabels.ToArray());
    }

    private static string GetShortOptionLabel(int anchorNodeId, int optionIndex, string fallback)
    {
        switch (anchorNodeId)
        {
            case 1001:
                return new[] { "发石车", "地道奇袭", "诈败埋伏" }[Mathf.Clamp(optionIndex, 0, 2)];
            case 2001:
                return new[] { "坚守官渡", "暂回许都" }[Mathf.Clamp(optionIndex, 0, 1)];
            case 3001:
                return new[] { "坦诚相告", "虚与委蛇", "推出斩首" }[Mathf.Clamp(optionIndex, 0, 2)];
            case 4001:
                return new[] { "亲自出征", "精兵奇袭" }[Mathf.Clamp(optionIndex, 0, 1)];
            case 5001:
                return new[] { "全力攻占", "分兵回守", "直取袁绍" }[Mathf.Clamp(optionIndex, 0, 2)];
            default:
                return CompactText(fallback, 8);
        }
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateText(Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetRuntimeFont();
        if (font != null)
        {
            text.font = font;
            if (font.material != null) text.fontSharedMaterial = font.material;
        }
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static TMP_Text CreateWrappedText(Transform parent, string value, float size, Color color)
    {
        TMP_Text text = CreateText(parent, value, size, TextAlignmentOptions.Left, color);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.lineSpacing = 4f;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        return text;
    }

    private static TMP_Text CreateAdaptiveSingleLineText(Transform parent, string value, float maxSize,
        float minSize, Color color)
    {
        TMP_Text text = CreateText(parent, value, maxSize, TextAlignmentOptions.Left, color);
        text.enableWordWrapping = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.verticalAlignment = VerticalAlignmentOptions.Top;
        return text;
    }

    private static TMP_FontAsset GetRuntimeFont()
    {
        if (runtimeFontLookupCompleted) return runtimeFont;
        runtimeFontLookupCompleted = true;

        // The project font lives under Assets/TextMesh Pro/Resources/Fonts & Materials.
        runtimeFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (runtimeFont == null)
        {
            TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            for (int i = 0; i < loadedFonts.Length; i++)
            {
                if (loadedFonts[i] != null && loadedFonts[i].name == "SC-Regular SDF")
                {
                    runtimeFont = loadedFonts[i];
                    break;
                }
            }
        }

        if (runtimeFont == null) runtimeFont = TMP_Settings.defaultFontAsset;
        if (runtimeFont == null)
            Debug.LogError("CampaignMapUI: 找不到中文字体 SC-Regular SDF 或 TMP 默认字体，剧情回顾文字无法显示。");
        return runtimeFont;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 topRightOffset, Color color)
    {
        GameObject buttonObject = CreateUIObject(label + "Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetTopRight(rect, topRightOffset, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(buttonObject.transform, label, 40f, TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        return button;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
