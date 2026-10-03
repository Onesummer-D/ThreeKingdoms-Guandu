using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Event-backed battle scroll. It presents the recorded run as a readable
/// timeline and never re-enters the authoritative dialogue flow.
/// </summary>
public sealed class BattleReplayUI : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(.012f, .02f, .03f, .96f);
    private static readonly Color PanelColor = new Color(.075f, .11f, .15f, .995f);
    private static readonly Color RowColor = new Color(.11f, .16f, .20f, .98f);
    private static readonly Color SelectedRowColor = new Color(.28f, .22f, .12f, .98f);
    private static readonly Color TextPrimary = new Color(.94f, .94f, .88f, 1f);
    private static readonly Color TextMuted = new Color(.69f, .76f, .78f, 1f);
    private static readonly Color AccentColor = new Color(.89f, .71f, .32f, 1f);
    private static readonly Color ButtonColor = new Color(.44f, .32f, .22f, 1f);

    private RunHistoryTracker historyTracker;
    private Transform uiRoot;
    private Sprite buttonSprite;
    private GameObject overlay;
    private RectTransform content;
    private ScrollRect scrollRect;
    private TMP_Text statusText;
    private TMP_Text emptyText;
    private readonly List<Image> rowImages = new List<Image>();
    private readonly List<TMP_Text> rowLabels = new List<TMP_Text>();
    private RunHistoryData history;
    private int cursor = -1;
    private bool playing;
    private Coroutine playbackRoutine;
    private bool initialized;

    public event Action OnClosed;

    public void Initialize(RunHistoryTracker tracker, Transform root, Sprite sharedButtonSprite)
    {
        if (initialized || tracker == null || root == null) return;
        historyTracker = tracker;
        uiRoot = root;
        buttonSprite = sharedButtonSprite;
        CreateOverlay();
        initialized = true;
    }

    private void OnDestroy()
    {
        StopPlayback();
        if (overlay != null) Destroy(overlay);
    }

    public void Open()
    {
        if (!initialized || overlay == null) return;
        StopPlayback();
        history = historyTracker.ExportHistory();
        cursor = -1;
        RebuildRows();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        RenderSelection();
    }

    private void Close()
    {
        StopPlayback();
        if (overlay != null) overlay.SetActive(false);
        OnClosed?.Invoke();
    }

    private void CreateOverlay()
    {
        overlay = CreateUIObject("BattleReplayOverlay", uiRoot);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = OverlayColor;

        GameObject panel = CreateUIObject("BattleReplayPanel", overlay.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.055f, .045f), new Vector2(.945f, .955f));
        panel.AddComponent<Image>().color = PanelColor;

        TMP_Text title = CreateText(panel.transform, "战役绘卷 · 真实事件回放", 48f,
            TextAlignmentOptions.Left, TextPrimary, false);
        SetAnchored(title.rectTransform, new Vector2(.045f, .91f), new Vector2(.72f, .98f));

        statusText = CreateText(panel.transform, "", 25f,
            TextAlignmentOptions.Left, TextMuted, true);
        SetAnchored(statusText.rectTransform, new Vector2(.045f, .805f), new Vector2(.955f, .90f));

        CreateScrollArea(panel.transform);

        emptyText = CreateText(panel.transform, "暂无事件记录", 34f,
            TextAlignmentOptions.Center, TextMuted, true);
        SetAnchored(emptyText.rectTransform, new Vector2(.18f, .43f), new Vector2(.82f, .56f));

        CreateControl(panel.transform, "播放", .045f, .02f, .14f, StartPlayback);
        CreateControl(panel.transform, "暂停", .155f, .02f, .25f, StopPlayback);
        CreateControl(panel.transform, "上一条", .265f, .02f, .375f, SelectPrevious);
        CreateControl(panel.transform, "下一条", .39f, .02f, .50f, SelectNext);
        CreateControl(panel.transform, "首个决策", .515f, .02f, .64f, SeekFirstDecision);
        CreateControl(panel.transform, "跳到结局", .655f, .02f, .78f, SeekEnding);
        CreateControl(panel.transform, "关闭", .83f, .02f, .955f, Close);

        overlay.SetActive(false);
    }

    private void CreateScrollArea(Transform panel)
    {
        GameObject viewport = CreateUIObject("ReplayViewport", panel);
        SetAnchored(viewport.GetComponent<RectTransform>(), new Vector2(.045f, .15f), new Vector2(.955f, .80f));
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, .14f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = CreateUIObject("ReplayContent", viewport.transform);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 45f;
    }

    private void RebuildRows()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        rowImages.Clear();
        rowLabels.Clear();

        List<RunEventData> events = history != null ? history.events : null;
        if (events == null || events.Count == 0)
        {
            emptyText.gameObject.SetActive(true);
            return;
        }
        emptyText.gameObject.SetActive(false);

        for (int i = 0; i < events.Count; i++)
        {
            RunEventData item = events[i];
            if (item == null) continue;
            GameObject row = CreateUIObject("ReplayEvent_" + i, content);
            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = 78f;
            Image image = row.AddComponent<Image>();
            image.color = RowColor;
            Button button = row.AddComponent<Button>();
            button.targetGraphic = image;
            int capturedIndex = i;
            button.onClick.AddListener(() => SelectEvent(capturedIndex));
            rowImages.Add(image);

            TMP_Text sequence = CreateText(row.transform,
                (i + 1).ToString("00") + "  " + FormatEventType(item.eventType),
                25f, TextAlignmentOptions.Left, AccentColor, false);
            SetAnchored(sequence.rectTransform, new Vector2(.025f, .54f), new Vector2(.33f, .93f));

            string label = string.IsNullOrWhiteSpace(item.label) ? "未命名事件" : item.label;
            string node = item.nodeId > 0 ? "节点 " + item.nodeId : "运行开始";
            TMP_Text body = CreateText(row.transform,
                node + "　" + label + "\n" + Compact(item.payload, 86),
                23f, TextAlignmentOptions.Left, TextPrimary, true);
            SetAnchored(body.rectTransform, new Vector2(.35f, .10f), new Vector2(.97f, .90f));
            rowLabels.Add(body);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void SelectEvent(int index)
    {
        if (history == null || history.events == null || history.events.Count == 0) return;
        cursor = Mathf.Clamp(index, 0, history.events.Count - 1);
        RenderSelection();
    }

    private void SelectPrevious()
    {
        if (history == null || history.events == null || history.events.Count == 0) return;
        SelectEvent(cursor <= 0 ? 0 : cursor - 1);
    }

    private void SelectNext()
    {
        if (history == null || history.events == null || history.events.Count == 0) return;
        SelectEvent(cursor < 0 ? 0 : Mathf.Min(history.events.Count - 1, cursor + 1));
    }

    private void SeekFirstDecision()
    {
        int index = FindEvent(RunEventTypes.DecisionMade);
        if (index >= 0) SelectEvent(index);
    }

    private void SeekEnding()
    {
        int index = FindEvent(RunEventTypes.EndingReached);
        if (index < 0) index = FindEvent(RunEventTypes.RunCompleted);
        if (index >= 0) SelectEvent(index);
    }

    private int FindEvent(string eventType)
    {
        if (history == null || history.events == null) return -1;
        for (int i = 0; i < history.events.Count; i++)
            if (history.events[i] != null && history.events[i].eventType == eventType) return i;
        return -1;
    }

    private void StartPlayback()
    {
        if (history == null || history.events == null || history.events.Count == 0) return;
        if (playing) return;
        if (cursor >= history.events.Count - 1) cursor = -1;
        playing = true;
        playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    private IEnumerator PlaybackRoutine()
    {
        while (playing && history != null && history.events != null && cursor < history.events.Count - 1)
        {
            SelectEvent(cursor + 1);
            yield return new WaitForSecondsRealtime(.68f);
        }
        playing = false;
        playbackRoutine = null;
    }

    private void StopPlayback()
    {
        playing = false;
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
    }

    private void RenderSelection()
    {
        if (history == null || history.events == null)
        {
            if (statusText != null) statusText.text = "暂无事件记录";
            return;
        }

        for (int i = 0; i < rowImages.Count; i++)
            rowImages[i].color = i == cursor ? SelectedRowColor : RowColor;

        string status = "已记录 " + history.events.Count + " 条事件";
        if (cursor >= 0 && cursor < history.events.Count)
        {
            RunEventData item = history.events[cursor];
            status += "\n当前：" + FormatEventType(item.eventType) + " · " +
                (item.label ?? string.Empty);
        }
        if (history.ending != null && history.ending.nodeId > 0)
        {
            status += "\n结局原因：" + (history.ending.reason ?? "未记录") +
                "\n史实边界：" + (history.ending.historicalDeviation ?? "未记录");
        }
        if (statusText != null) statusText.text = status;
        if (scrollRect != null && cursor >= 0 && cursor < rowImages.Count)
            Canvas.ForceUpdateCanvases();
    }

    private void CreateControl(Transform parent, string label, float minX, float minY,
        float maxX, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, label, 25f);
        SetAnchored(button.GetComponent<RectTransform>(), new Vector2(minX, minY), new Vector2(maxX, .09f));
        button.onClick.AddListener(action);
    }

    private Button CreateButton(Transform parent, string label, float fontSize)
    {
        GameObject buttonObject = CreateUIObject(label + "Button", parent);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = ButtonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(buttonObject.transform, label, fontSize,
            TextAlignmentOptions.Center, TextPrimary, false);
        Stretch(text.rectTransform);
        return button;
    }

    private static string FormatEventType(string eventType)
    {
        switch (eventType)
        {
            case RunEventTypes.RunStarted: return "出征";
            case RunEventTypes.NodeShown: return "进入节点";
            case RunEventTypes.DecisionMade: return "作出决策";
            case RunEventTypes.ResourceChanged: return "资源变化";
            case RunEventTypes.MiniGameStarted: return "小游戏开始";
            case RunEventTypes.MiniGameAction: return "小游戏操作";
            case RunEventTypes.MiniGameCompleted: return "小游戏结果";
            case RunEventTypes.AdvisorSuggested: return "参谋建议";
            case RunEventTypes.SceneStateChanged: return "战场状态";
            case RunEventTypes.EndingReached: return "抵达结局";
            case RunEventTypes.RunCompleted: return "战局完成";
            default: return eventType ?? "未知事件";
        }
    }

    private static string Compact(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return "无补充信息";
        string line = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return line.Length <= maxCharacters
            ? line
            : line.Substring(0, Mathf.Max(1, maxCharacters - 1)) + "…";
    }

    private static TMP_Text CreateText(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color, bool wrap)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
