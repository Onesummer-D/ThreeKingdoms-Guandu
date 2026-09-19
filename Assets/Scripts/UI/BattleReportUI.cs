using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleReportUI : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(.012f, .02f, .03f, .95f);
    private static readonly Color PanelColor = new Color(.075f, .11f, .15f, .995f);
    private static readonly Color CardColor = new Color(.11f, .16f, .20f, .98f);
    private static readonly Color TextPrimary = new Color(.94f, .94f, .88f, 1f);
    private static readonly Color TextMuted = new Color(.69f, .76f, .78f, 1f);
    private static readonly Color AccentColor = new Color(.89f, .71f, .32f, 1f);
    private static readonly Color ButtonColor = new Color(.44f, .32f, .22f, 1f);
    private static readonly Color[] ResourceColors =
    {
        new Color(.84f, .32f, .28f, 1f),
        new Color(.91f, .70f, .25f, 1f),
        new Color(.35f, .78f, .68f, 1f),
        new Color(.64f, .48f, .84f, 1f)
    };
    private const float CardBadgeRight = .16f;
    private const float CardTextLeft = .18f;

    [Header("战绩报告卡片左侧标题图（可选）")]
    public Sprite statsCardBadge;
    public Sprite tendencyCardBadge;
    public Sprite resourceTrendCardBadge;
    public Sprite cultureCardBadge;
    public Sprite advisorEchoCardBadge;

    private CampaignMapUI campaignMap;
    private RunHistoryTracker historyTracker;
    private GameObject overlay;
    private RectTransform content;
    private ScrollRect scrollRect;
    private TMP_Text statusText;
    private Sprite buttonSprite;
    private Sprite portraitSprite;
    private GameObject exportOverlay;
    private GameObject trendDetailOverlay;
    private ResourceTrendPlotUI trendDetailPlot;
    private readonly TMP_Text[] trendDetailSummaryValues = new TMP_Text[4];
    private string lastPosterPath;
    private bool capturing;

    public event Action OnClosed;

    public void Initialize(CampaignMapUI map, RunHistoryTracker tracker, Transform root,
        Sprite sharedButtonSprite, Sprite reportPortrait)
    {
        if (overlay != null || map == null || tracker == null || root == null) return;
        campaignMap = map;
        historyTracker = tracker;
        buttonSprite = sharedButtonSprite;
        portraitSprite = reportPortrait;
        CreateOverlay(root);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    public void Open()
    {
        if (overlay == null) return;
        Rebuild();
        if (exportOverlay != null) exportOverlay.SetActive(false);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        if (trendDetailOverlay != null) trendDetailOverlay.SetActive(false);
    }

    public void Close()
    {
        if (capturing || overlay == null) return;
        overlay.SetActive(false);
        OnClosed?.Invoke();
    }

    private void CreateOverlay(Transform root)
    {
        overlay = CreateUIObject("BattleReportOverlay", root);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = OverlayColor;

        GameObject panel = CreateUIObject("BattleReportPanel", overlay.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.055f, .045f), new Vector2(.945f, .955f));
        panel.AddComponent<Image>().color = PanelColor;

        TMP_Text title = CreateText(panel.transform, "战绩报告", 52f,
            TextAlignmentOptions.Left, TextPrimary, false);
        SetAnchored(title.rectTransform, new Vector2(.045f, .875f), new Vector2(.60f, .975f));

        statusText = CreateText(panel.transform, "", 25f,
            TextAlignmentOptions.Left, TextMuted, false);
        SetAnchored(statusText.rectTransform, new Vector2(.045f, .025f), new Vector2(.56f, .105f));

        Button share = CreateButton(panel.transform, "分享海报", 31f);
        SetAnchored(share.GetComponent<RectTransform>(), new Vector2(.64f, .025f), new Vector2(.79f, .105f));
        share.onClick.AddListener(() =>
        {
            if (!capturing) StartCoroutine(CapturePoster());
        });

        Button close = CreateButton(panel.transform, "关闭", 31f);
        SetAnchored(close.GetComponent<RectTransform>(), new Vector2(.81f, .025f), new Vector2(.95f, .105f));
        close.onClick.AddListener(Close);

        CreateScrollArea(panel.transform);
        TMP_Text disclaimer = CreateText(panel.transform,
            "架空推演：部分结局为游戏创作表达，不代表史实",
            22f, TextAlignmentOptions.Center, TextMuted, false);
        SetAnchored(disclaimer.rectTransform, new Vector2(.24f, .105f), new Vector2(.76f, .13f));
        CreateExportOverlay(panel.transform);
        CreateTrendDetailOverlay(overlay.transform);
        overlay.SetActive(false);
    }

    private void CreateScrollArea(Transform panel)
    {
        GameObject viewport = CreateUIObject("Viewport", panel);
        SetAnchored(viewport.GetComponent<RectTransform>(), new Vector2(.045f, .13f), new Vector2(.955f, .865f));
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, .14f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = CreateUIObject("Content", viewport.transform);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1200f);

        scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 45f;

        GameObject barObject = CreateUIObject("Scrollbar", panel);
        SetAnchored(barObject.GetComponent<RectTransform>(), new Vector2(.962f, .13f), new Vector2(.975f, .865f));
        Image barImage = barObject.AddComponent<Image>();
        barImage.color = new Color(.18f, .25f, .27f, .8f);
        Scrollbar bar = barObject.AddComponent<Scrollbar>();
        GameObject handle = CreateUIObject("Handle", barObject.transform);
        Stretch(handle.GetComponent<RectTransform>());
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = AccentColor;
        bar.handleRect = handle.GetComponent<RectTransform>();
        bar.targetGraphic = handleImage;
        bar.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.verticalScrollbar = bar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private void Rebuild()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        if (statusText != null) statusText.text = "";

        RunHistoryData history = historyTracker.ExportHistory();
        DecisionTendencyResult tendency = BattleReportAnalyzer.Analyze(history);
        float cursor = 14f;
        cursor = CreateHeroCard(cursor);
        cursor = CreateStatsCard(cursor, history);
        cursor = CreateTendencyCard(cursor, tendency);
        cursor = CreateTrendCard(cursor, history);
        cursor = CreateCultureCard(cursor);
        cursor = CreateAdvisorEchoCard(cursor, history);
        cursor += 22f;
        content.sizeDelta = new Vector2(0f, cursor + 26f);
        Canvas.ForceUpdateCanvases();
    }

    private float CreateHeroCard(float y)
    {
        GameObject card = CreateCard("Ending", y, 170f, false);
        Sprite sprite = portraitSprite != null ? portraitSprite : campaignMap.GetReachedEndingSprite();
        if (sprite != null)
        {
            GameObject imageObject = CreateUIObject("EndingImage", card.transform);
            SetAnchored(imageObject.GetComponent<RectTransform>(), new Vector2(.018f, .10f), new Vector2(.205f, .90f));
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
        }
        TMP_Text title = CreateText(card.transform, "恭喜通关 · " + campaignMap.GetReachedEndingLabel(), 46f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(.23f, .62f), new Vector2(.96f, .92f));
        // Keep the ending sentence complete. The previous two-line summary
        // appended the evaluation into a short card and TMP rendered the
        // overflow as an ellipsis immediately after “许都”. The evaluation
        // already has its own tendency/culture sections below, so the hero
        // card shows the authored ending summary verbatim.
        string endingSummary = campaignMap.GetReachedEndingSummary();
        string endingEvaluation = campaignMap.GetReachedEndingEvaluation();
        // Only add the evaluation when both authored sentences fit the hero
        // card comfortably. Otherwise omit it rather than showing a clipped
        // line or a misleading trailing ellipsis.
        string heroText = endingSummary;
        if (!string.IsNullOrWhiteSpace(endingEvaluation) &&
            endingSummary.Length + endingEvaluation.Length + 1 <= 38)
            heroText += "\n" + endingEvaluation;
        TMP_Text summary = CreateText(card.transform,
            heroText, 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        summary.verticalAlignment = VerticalAlignmentOptions.Middle;
        summary.overflowMode = TextOverflowModes.Overflow;
        summary.enableAutoSizing = true;
        summary.fontSizeMin = 24f;
        summary.fontSizeMax = 34f;
        // Center the ending sentence inside the right-hand text column, while
        // keeping the portrait in the reserved left-hand visual column.
        SetAnchored(summary.rectTransform, new Vector2(.23f, .10f), new Vector2(.96f, .62f));
        summary.rectTransform.pivot = new Vector2(.5f, .5f);
        summary.alignment = TextAlignmentOptions.Center;
        return y + 182f;
    }

    private float CreateStatsCard(float y, RunHistoryData history)
    {
        GameObject card = CreateCard("Stats", y, 130f);
        ResourceSnapshotData final = GetFinalSnapshot(history);
        string duration = FormatDuration(history != null ? history.activeSeconds : 0f);
        string values = final == null
            ? "暂无资源记录"
            : "兵力 " + Round(final.troop) + "　粮草 " + Round(final.food) + "　计策 " + Round(final.strategy) + "　风险 " + Round(final.risk);
        TMP_Text text = CreateText(card.transform,
            "有效游玩 " + duration + "　｜　实际决策 " + GetDecisionCount(history) + " 次\n" + values,
            30f, TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(text.rectTransform, new Vector2(CardTextLeft, .12f), new Vector2(.96f, .88f));
        return y + 142f;
    }

    private float CreateTendencyCard(float y, DecisionTendencyResult tendency)
    {
        GameObject card = CreateCard("Tendency", y, 160f);
        TMP_Text label = CreateText(card.transform, "本局决策倾向 · " + tendency.label, 40f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(label.rectTransform, new Vector2(CardTextLeft, .70f), new Vector2(.97f, .94f));
        TMP_Text body = CreateText(card.transform, tendency.summary, 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(CardTextLeft, .18f), new Vector2(.96f, .68f));
        return y + 172f;
    }

    private float CreateTrendCard(float y, RunHistoryData history)
    {
        GameObject card = CreateCard("ResourceTrend", y, 560f);
        TMP_Text title = CreateText(card.transform, "四项资源趋势 · 真实节点记录", 40f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        title.enableAutoSizing = true;
        title.fontSizeMin = 32f;
        title.fontSizeMax = 40f;
        SetAnchored(title.rectTransform, new Vector2(CardTextLeft, .925f), new Vector2(.97f, .985f));

        List<ResourceSnapshotData> timeline = BuildTrendTimeline(history);
        ResourceSnapshotData start = timeline.Count > 0 ? timeline[0] : null;
        ResourceSnapshotData final = timeline.Count > 0 ? timeline[timeline.Count - 1] : GetFinalSnapshot(history);
        CreateResourceSummary(card.transform, start, final, true, null);

        GameObject chartObject = CreateUIObject("TrendLines", card.transform);
        SetAnchored(chartObject.GetComponent<RectTransform>(), new Vector2(CardTextLeft, .12f), new Vector2(.975f, .70f));
        ResourceTrendPlotUI chart = chartObject.AddComponent<ResourceTrendPlotUI>();
        chart.SetData(timeline, ResourceTrendViewMode.Compact);
        Button detail = CreateButton(card.transform, "查看详情", 28f);
        SetAnchored(detail.GetComponent<RectTransform>(), new Vector2(.80f, .045f), new Vector2(.975f, .115f));
        detail.onClick.AddListener(() => OpenTrendDetail(timeline));
        return y + 572f;
    }

    private void CreateTrendDetailOverlay(Transform parent)
    {
        trendDetailOverlay = CreateUIObject("TrendDetailOverlay", parent);
        Stretch(trendDetailOverlay.GetComponent<RectTransform>());
        trendDetailOverlay.AddComponent<Image>().color = new Color(.008f, .014f, .022f, .94f);

        GameObject panel = CreateUIObject("TrendDetailPanel", trendDetailOverlay.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.12f, .10f), new Vector2(.88f, .90f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = PanelColor;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText(panel.transform, "资源趋势详情 · 决策前后节点", 46f,
            TextAlignmentOptions.Left, TextPrimary, false);
        title.enableAutoSizing = true;
        title.fontSizeMin = 36f;
        title.fontSizeMax = 46f;
        SetAnchored(title.rectTransform, new Vector2(.07f, .90f), new Vector2(.52f, .98f));

        Button close = CreateButton(panel.transform, "关闭", 32f);
        SetAnchored(close.GetComponent<RectTransform>(), new Vector2(.80f, .90f), new Vector2(.94f, .98f));
        close.onClick.AddListener(CloseTrendDetail);

        CreateResourceSummary(panel.transform, null, null, false, trendDetailSummaryValues);
        GameObject plotObject = CreateUIObject("TrendDetailPlot", panel.transform);
        SetAnchored(plotObject.GetComponent<RectTransform>(), new Vector2(.07f, .07f), new Vector2(.94f, .72f));
        trendDetailPlot = plotObject.AddComponent<ResourceTrendPlotUI>();
        trendDetailOverlay.SetActive(false);
    }

    private void OpenTrendDetail(IList<ResourceSnapshotData> timeline)
    {
        if (trendDetailOverlay == null || trendDetailPlot == null) return;
        trendDetailPlot.SetData(timeline, ResourceTrendViewMode.Detail);
        ResourceSnapshotData start = timeline != null && timeline.Count > 0 ? timeline[0] : null;
        ResourceSnapshotData final = timeline != null && timeline.Count > 0
            ? timeline[timeline.Count - 1]
            : null;
        UpdateResourceSummary(trendDetailSummaryValues, start, final);
        trendDetailOverlay.SetActive(true);
        trendDetailOverlay.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
    }

    private void CloseTrendDetail()
    {
        if (trendDetailOverlay != null) trendDetailOverlay.SetActive(false);
    }

    private void CreateResourceSummary(Transform parent, ResourceSnapshotData start,
        ResourceSnapshotData final, bool compact, TMP_Text[] valueTargets)
    {
        string[] labels = { "兵力", "粮草", "计策", "风险" };
        float[] xs = compact
            ? new[] { .18f, .58f, .18f, .58f }
            : new[] { .09f, .55f, .09f, .55f };
        float[] ys = compact
            ? new[] { .82f, .82f, .71f, .71f }
            : new[] { .83f, .83f, .73f, .73f };
        float itemWidth = compact ? .37f : .36f;
        float markerSize = compact ? 25f : 28f;
        float textSize = compact ? 27f : 29f;
        for (int i = 0; i < labels.Length; i++)
        {
            string value = FormatResourceSummaryValue(labels[i], i, start, final);
            TMP_Text text = CreateResourceSummaryItem(parent, labels[i], ResourceColors[i],
                value, xs[i], ys[i], itemWidth, markerSize, textSize);
            if (valueTargets != null && i < valueTargets.Length)
                valueTargets[i] = text;
        }
    }

    private static void UpdateResourceSummary(TMP_Text[] valueTargets,
        ResourceSnapshotData start, ResourceSnapshotData final)
    {
        if (valueTargets == null) return;
        string[] labels = { "兵力", "粮草", "计策", "风险" };
        for (int i = 0; i < labels.Length && i < valueTargets.Length; i++)
            if (valueTargets[i] != null)
                valueTargets[i].text = FormatResourceSummaryValue(labels[i], i, start, final);
    }

    private static string FormatResourceSummaryValue(string label, int index,
        ResourceSnapshotData start, ResourceSnapshotData final)
    {
        if (final == null) return label + " —";
        float value = index == 0 ? final.troop : index == 1 ? final.food :
            index == 2 ? final.strategy : final.risk;
        float startValue = start == null ? value : index == 0 ? start.troop : index == 1 ? start.food :
            index == 2 ? start.strategy : start.risk;
        return FormatResourceValue(label, value, value - startValue, start != null);
    }

    private TMP_Text CreateResourceSummaryItem(Transform parent, string label, Color color,
        string value, float x, float y, float width, float markerSize, float textSize)
    {
        GameObject item = CreateUIObject(label + "ResourceSummary", parent);
        SetAnchored(item.GetComponent<RectTransform>(), new Vector2(x, y),
            new Vector2(x + width, y + .105f));
        TMP_Text marker = CreateText(item.transform, GetResourceMarker(label), markerSize,
            TextAlignmentOptions.Center, color, false);
        marker.fontStyle = FontStyles.Bold;
        SetAnchored(marker.rectTransform, new Vector2(0f, 0f), new Vector2(.11f, 1f));
        TMP_Text text = CreateText(item.transform, value, textSize,
            TextAlignmentOptions.Left, TextPrimary, false);
        SetAnchored(text.rectTransform, new Vector2(.13f, 0f), new Vector2(1f, 1f));
        return text;
    }

    private static string FormatResourceValue(string label, float value, float delta, bool hasDelta)
    {
        return label + " " + Round(value) + "% " + (hasDelta ? FormatDelta(delta) : "—");
    }

    private static string FormatDelta(float delta)
    {
        if (Mathf.Abs(delta) < .5f) return "—";
        return delta > 0f ? "↑" + Round(Mathf.Abs(delta)) : "↓" + Round(Mathf.Abs(delta));
    }

    private static List<ResourceSnapshotData> BuildTrendTimeline(RunHistoryData history)
    {
        List<ResourceSnapshotData> points = new List<ResourceSnapshotData>();
        if (history != null && history.resourceTimeline != null)
            for (int i = 0; i < history.resourceTimeline.Count; i++)
                if (history.resourceTimeline[i] != null) points.Add(CloneSnapshot(history.resourceTimeline[i]));

        // Older local saves may predate resourceTimeline. Reconstructing the
        // actual before/after snapshots from their decision log keeps the
        // report honest while ensuring the chart is never an empty panel.
        if (points.Count == 0 && history != null && history.decisions != null)
        {
            for (int i = 0; i < history.decisions.Count; i++)
            {
                DecisionRecordData decision = history.decisions[i];
                if (decision == null) continue;
                if (decision.before != null) points.Add(CloneSnapshot(decision.before));
                if (decision.after != null) points.Add(CloneSnapshot(decision.after));
            }
        }
        return points;
    }

    private static ResourceSnapshotData CloneSnapshot(ResourceSnapshotData source)
    {
        if (source == null) return null;
        return new ResourceSnapshotData
        {
            sequence = source.sequence,
            nodeId = source.nodeId,
            label = source.label,
            elapsedSeconds = source.elapsedSeconds,
            troop = source.troop,
            food = source.food,
            strategy = source.strategy,
            risk = source.risk
        };
    }

    private float CreateCultureCard(float y)
    {
        GameObject card = CreateCard("Culture", y, 250f);
        TMP_Text title = CreateText(card.transform, "史官简注", 38f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(CardTextLeft, .76f), new Vector2(.97f, .94f));
        TMP_Text body = CreateText(card.transform, GetCultureNote(campaignMap.GetReachedEndingNodeId()), 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(CardTextLeft, .12f), new Vector2(.97f, .70f));
        body.overflowMode = TextOverflowModes.Overflow;
        body.enableAutoSizing = true;
        body.fontSizeMin = 24f;
        body.fontSizeMax = 34f;
        body.verticalAlignment = VerticalAlignmentOptions.Middle;
        return y + 262f;
    }

    private float CreateAdvisorEchoCard(float y, RunHistoryData history)
    {
        if (history == null || history.advisorEchoes == null || history.advisorEchoes.Count == 0)
            return y;

        float height = 190f + Mathf.Min(history.advisorEchoes.Count, 3) * 105f;
        GameObject card = CreateCard("AdvisorEcho", y, height);
        TMP_Text title = CreateText(card.transform, "共谋回声", 38f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(CardTextLeft, .82f), new Vector2(.97f, .95f));

        List<string> lines = new List<string>();
        int limit = Mathf.Min(history.advisorEchoes.Count, 3);
        for (int i = 0; i < limit; i++)
        {
            AdvisorEchoData echo = history.advisorEchoes[i];
            if (echo == null) continue;
            string verdict = echo.accepted ? "主将采纳" : "主将未采纳";
            lines.Add("参谋" + (string.IsNullOrWhiteSpace(echo.guestLabel) ? "" : "「" + echo.guestLabel + "」") +
                "建议：" + (echo.optionText ?? "未填写") + "\n" + verdict + "。" +
                (string.IsNullOrWhiteSpace(echo.reason) ? "" : "\n理由：" + echo.reason));
        }
        TMP_Text body = CreateText(card.transform, string.Join("\n\n", lines), 30f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(CardTextLeft, .10f), new Vector2(.97f, .78f));
        body.overflowMode = TextOverflowModes.Overflow;
        return y + height + 12f;
    }

    private IEnumerator CapturePoster()
    {
        capturing = true;
        if (statusText != null) statusText.text = "";
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        Texture2D image = null;
        try
        {
            image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            image.Apply();
            byte[] png = image.EncodeToPNG();
            string folder = GetPosterDirectory();
            Directory.CreateDirectory(folder);
            string fileName = "官渡战绩_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            File.WriteAllBytes(Path.Combine(folder, fileName), png);
            lastPosterPath = Path.Combine(folder, fileName);
            if (statusText != null) statusText.text = "已保存：" + lastPosterPath;
            if (exportOverlay != null) exportOverlay.SetActive(true);
        }
        catch (Exception exception)
        {
            Debug.LogError("战绩海报保存失败：" + exception.Message);
            if (statusText != null) statusText.text = "海报保存失败，请检查磁盘权限或剩余空间。";
        }
        finally
        {
            if (image != null) Destroy(image);
            capturing = false;
        }
    }

    private static string GetPosterDirectory()
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures)) pictures = Application.persistentDataPath;
        return Path.Combine(pictures, "官渡之战战绩");
    }

    private void CreateExportOverlay(Transform parent)
    {
        exportOverlay = CreateUIObject("PosterExportActions", parent);
        Stretch(exportOverlay.GetComponent<RectTransform>());
        exportOverlay.AddComponent<Image>().color = new Color(.01f, .015f, .025f, .72f);
        GameObject panel = CreateUIObject("PosterExportPanel", exportOverlay.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.22f, .30f), new Vector2(.78f, .70f));
        panel.AddComponent<Image>().color = PanelColor;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(3f, -3f);

        TMP_Text title = CreateText(panel.transform, "海报已保存", 46f, TextAlignmentOptions.Center, TextPrimary, false);
        SetAnchored(title.rectTransform, new Vector2(.08f, .68f), new Vector2(.92f, .92f));
        TMP_Text path = CreateText(panel.transform, "", 26f, TextAlignmentOptions.Center, TextMuted, true);
        SetAnchored(path.rectTransform, new Vector2(.06f, .47f), new Vector2(.94f, .68f));

        Button openImage = CreateButton(panel.transform, "打开图片", 30f);
        SetAnchored(openImage.GetComponent<RectTransform>(), new Vector2(.06f, .20f), new Vector2(.28f, .40f));
        openImage.onClick.AddListener(() => OpenFile(lastPosterPath));
        Button openFolder = CreateButton(panel.transform, "打开文件夹", 30f);
        SetAnchored(openFolder.GetComponent<RectTransform>(), new Vector2(.31f, .20f), new Vector2(.55f, .40f));
        openFolder.onClick.AddListener(() => OpenFolder(lastPosterPath));
        Button copyPath = CreateButton(panel.transform, "复制路径", 30f);
        SetAnchored(copyPath.GetComponent<RectTransform>(), new Vector2(.58f, .20f), new Vector2(.79f, .40f));
        copyPath.onClick.AddListener(() => GUIUtility.systemCopyBuffer = lastPosterPath ?? string.Empty);
        Button close = CreateButton(panel.transform, "关闭", 30f);
        SetAnchored(close.GetComponent<RectTransform>(), new Vector2(.82f, .20f), new Vector2(.94f, .40f));
        close.onClick.AddListener(() => exportOverlay.SetActive(false));
        exportOverlay.SetActive(false);
        exportOverlay.GetComponent<RectTransform>().SetAsLastSibling();

        // Refresh the path label whenever the panel is opened.
        PosterPathLabel = path;
    }

    private TMP_Text PosterPathLabel { get; set; }

    private void LateUpdate()
    {
        if (exportOverlay != null && exportOverlay.activeSelf && PosterPathLabel != null)
            PosterPathLabel.text = lastPosterPath ?? string.Empty;
    }

    private static void OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void OpenFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        string argument = "/select,\"" + path + "\"";
        Process.Start(new ProcessStartInfo("explorer.exe", argument) { UseShellExecute = true });
    }

    private GameObject CreateCard(string name, float y, float height)
    {
        return CreateCard(name, y, height, true);
    }

    private GameObject CreateCard(string name, float y, float height, bool withBadgeSlot)
    {
        GameObject card = CreateUIObject(name + "Card", content);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.015f, 1f);
        rect.anchorMax = new Vector2(.985f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(0f, height);
        Image image = card.AddComponent<Image>();
        image.color = CardColor;
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(.45f, .36f, .20f, .65f);
        outline.effectDistance = new Vector2(1f, -1f);
        if (withBadgeSlot) CreateCardBadgeSlot(card.transform, name);
        return card;
    }

    private void CreateCardBadgeSlot(Transform parent, string cardName)
    {
        GameObject slot = CreateUIObject("CardBadgeSlot", parent);
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(.018f, .10f), new Vector2(CardBadgeRight, .90f));
        Image image = slot.AddComponent<Image>();
        image.sprite = GetCardBadgeSprite(cardName);
        image.preserveAspect = true;
        image.color = image.sprite != null
            ? Color.white
            : new Color(.055f, .085f, .10f, .58f);
        image.raycastTarget = false;
        Outline outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(.45f, .36f, .20f, .72f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private Sprite GetCardBadgeSprite(string cardName)
    {
        switch (cardName)
        {
            case "Stats": return statsCardBadge;
            case "Tendency": return tendencyCardBadge;
            case "ResourceTrend": return resourceTrendCardBadge;
            case "Culture": return cultureCardBadge;
            case "AdvisorEcho": return advisorEchoCardBadge;
            default: return null;
        }
    }

    private static string GetResourceMarker(string label)
    {
        switch (label)
        {
            case "兵力": return "●";
            case "粮草": return "◆";
            case "计策": return "▲";
            case "风险": return "□";
            default: return "●";
        }
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

    private static ResourceSnapshotData GetFinalSnapshot(RunHistoryData history)
    {
        if (history == null || history.resourceTimeline == null || history.resourceTimeline.Count == 0) return null;
        return history.resourceTimeline[history.resourceTimeline.Count - 1];
    }

    private static int GetDecisionCount(RunHistoryData history)
    {
        return history != null && history.decisions != null ? history.decisions.Count : 0;
    }

    private static string FormatDecision(DecisionRecordData decision)
    {
        if (decision == null) return "无效决策记录";
        string chapter = decision.chapterIndex >= 0 ? "第" + (decision.chapterIndex + 1) + "幕" : "支线";
        return decision.sequence.ToString("00") + "　" + chapter + "　选择：" + Compact(decision.optionText, 28) + "　" + FormatDelta(decision);
    }

    private static string FormatDelta(DecisionRecordData decision)
    {
        if (decision.before == null || decision.after == null) return "资源记录缺失";
        List<string> parts = new List<string>();
        AddDelta(parts, "兵", decision.after.troop - decision.before.troop);
        AddDelta(parts, "粮", decision.after.food - decision.before.food);
        AddDelta(parts, "策", decision.after.strategy - decision.before.strategy);
        AddDelta(parts, "险", decision.after.risk - decision.before.risk);
        return parts.Count == 0 ? "资源未变" : string.Join(" ", parts);
    }

    private static void AddDelta(List<string> parts, string label, float value)
    {
        if (Mathf.Approximately(value, 0f)) return;
        parts.Add(label + (value > 0 ? "+" : "") + Mathf.RoundToInt(value));
    }

    private static string GetCultureNote(int endingNodeId)
    {
        switch (endingNodeId)
        {
            case 500217: return "史实结局：曹操夜袭乌巢，焚毁袁军粮秣，袁绍军势由此转弱，官渡战局转向曹操；本局结果与这一史实节点一致。";
            case 200314: return "史实背景：官渡相持与粮道争夺最终使曹操取胜；本局撤退失序、退回许都是架空分支。";
            case 300416: return "史实背景：许攸来投并提供乌巢粮道情报；本局情报未被采纳、粮草耗尽是架空分支。";
            case 500313: return "史实背景：乌巢粮秣被焚使袁军补给受创；本局夜袭未能彻底奏效、曹军元气大伤是架空分支。";
            case 500320: return "史实背景：官渡之战以曹操击败袁绍告终；本局保存主力、战局未决是架空推演。";
            case 500411: return "史实背景：乌巢奇袭成为扭转官渡战局的关键；本局直取袁绍本阵并提前完成北方统一是架空分支。";
            case 500418: return "史实背景：曹军最终在官渡取胜；本局高风险突击失败、全线溃败是架空分支。";
            default: return "本局尚未形成可核对的终局说明。";
        }
    }

    private static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private static int Round(float value) { return Mathf.RoundToInt(value); }

    private static string Compact(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return "未命名选择";
        string line = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return line.Length <= maxCharacters ? line : line.Substring(0, maxCharacters - 1) + "…";
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
        // Long report copy must wrap or overflow into a card that has been
        // sized for it. Ellipsis hides authored history and makes the report
        // look incomplete, especially for the historical ending note.
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

    private static void SetTop(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.offsetMin = new Vector2(minOffset.x, minOffset.y - maxOffset.y);
        rect.offsetMax = new Vector2(maxOffset.x, minOffset.y);
    }
}
