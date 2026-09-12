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

    private CampaignMapUI campaignMap;
    private RunHistoryTracker historyTracker;
    private GameObject overlay;
    private RectTransform content;
    private ScrollRect scrollRect;
    private TMP_Text statusText;
    private Sprite buttonSprite;
    private Sprite portraitSprite;
    private GameObject exportOverlay;
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
        GameObject card = CreateCard("Ending", y, 170f);
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
        // Center the ending sentence against the entire card, matching the
        // tendency summary below. The portrait is a visual accent and must
        // not shift the sentence's perceived center to the right.
        // Use the same horizontal bounds as the tendency sentence below so
        // both lines share one unmistakable visual center across the card.
        SetAnchored(summary.rectTransform, new Vector2(.08f, .10f), new Vector2(.92f, .62f));
        summary.rectTransform.pivot = new Vector2(.5f, .5f);
        summary.alignment = TextAlignmentOptions.Center;
        return y + 182f;
    }

    private float CreateStatsCard(float y, RunHistoryData history)
    {
        GameObject card = CreateCard("Stats", y, 104f);
        ResourceSnapshotData final = GetFinalSnapshot(history);
        string duration = FormatDuration(history != null ? history.activeSeconds : 0f);
        string values = final == null
            ? "暂无资源记录"
            : "兵力 " + Round(final.troop) + "　粮草 " + Round(final.food) + "　计策 " + Round(final.strategy) + "　风险 " + Round(final.risk);
        TMP_Text text = CreateText(card.transform,
            "有效游玩 " + duration + "　｜　实际决策 " + GetDecisionCount(history) + " 次\n" + values,
            34f, TextAlignmentOptions.Center, TextPrimary, true);
        Stretch(text.rectTransform);
        return y + 116f;
    }

    private float CreateTendencyCard(float y, DecisionTendencyResult tendency)
    {
        GameObject card = CreateCard("Tendency", y, 160f);
        TMP_Text label = CreateText(card.transform, "本局决策倾向 · " + tendency.label, 40f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(label.rectTransform, new Vector2(.025f, .70f), new Vector2(.97f, .94f));
        TMP_Text body = CreateText(card.transform, tendency.summary, 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(.08f, .18f), new Vector2(.92f, .68f));
        return y + 172f;
    }

    private float CreateTrendCard(float y, RunHistoryData history)
    {
        GameObject card = CreateCard("ResourceTrend", y, 410f);
        TMP_Text title = CreateText(card.transform, "四项资源趋势 · 真实节点记录（0—100）", 38f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(.025f, .87f), new Vector2(.52f, .97f));
        CreateLegend(card.transform, "兵力", ResourceColors[0], .60f, .88f);
        CreateLegend(card.transform, "粮草", ResourceColors[1], .78f, .88f);
        CreateLegend(card.transform, "计策", ResourceColors[2], .60f, .76f);
        CreateLegend(card.transform, "风险", ResourceColors[3], .78f, .76f);

        GameObject chartObject = CreateUIObject("TrendLines", card.transform);
        SetAnchored(chartObject.GetComponent<RectTransform>(), new Vector2(.035f, .08f), new Vector2(.965f, .68f));
        ResourceTrendGraphic chart = chartObject.AddComponent<ResourceTrendGraphic>();
        chart.raycastTarget = false;
        List<ResourceSnapshotData> timeline = BuildTrendTimeline(history);
        chart.SetData(timeline);
        CreateResourceBarChart(card.transform,
            timeline.Count > 0 ? timeline[timeline.Count - 1] : GetFinalSnapshot(history));
        return y + 422f;
    }

    private void CreateResourceBarChart(Transform parent, ResourceSnapshotData snapshot)
    {
        string[] labels = { "兵力", "粮草", "计策", "风险" };
        float[] values = snapshot == null
            ? new float[4]
            : new[] { snapshot.troop, snapshot.food, snapshot.strategy, snapshot.risk };
        for (int i = 0; i < labels.Length; i++)
        {
            // Each row owns its labels and bar. This guarantees the text is
            // rendered above the trend graphic and remains visible even when
            // the report is rebuilt after a restored save.
            float rowTop = .68f - i * .145f;
            float rowBottom = rowTop - .095f;
            GameObject row = CreateUIObject(labels[i] + "ResourceRow", parent);
            SetAnchored(row.GetComponent<RectTransform>(), new Vector2(.035f, rowBottom), new Vector2(.965f, rowTop));

            GameObject background = CreateUIObject(labels[i] + "BarBackground", row.transform);
            SetAnchored(background.GetComponent<RectTransform>(), new Vector2(.18f, .18f), new Vector2(.82f, .82f));
            background.AddComponent<Image>().color = new Color(.22f, .29f, .31f, .95f);

            GameObject fill = CreateUIObject(labels[i] + "BarFill", background.transform);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            float normalized = Mathf.Clamp01(values[i] / 100f);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(normalized, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = ResourceColors[i];

            TMP_Text label = CreateText(row.transform, labels[i], 30f,
                TextAlignmentOptions.Left, TextPrimary, false);
            SetAnchored(label.rectTransform, new Vector2(.00f, .02f), new Vector2(.17f, .98f));
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.transform.SetAsLastSibling();

            TMP_Text amount = CreateText(row.transform, snapshot == null ? "—" : Round(values[i]).ToString() + "%",
                30f, TextAlignmentOptions.Right, ResourceColors[i], false);
            SetAnchored(amount.rectTransform, new Vector2(.83f, .02f), new Vector2(1f, .98f));
            amount.enableAutoSizing = false;
            amount.overflowMode = TextOverflowModes.Overflow;
            amount.transform.SetAsLastSibling();
        }
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
        GameObject card = CreateCard("Culture", y, 172f);
        TMP_Text title = CreateText(card.transform, "史官简注", 38f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(.025f, .63f), new Vector2(.97f, .93f));
        TMP_Text body = CreateText(card.transform, GetCultureNote(campaignMap.GetReachedEndingNodeId()), 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(.08f, .16f), new Vector2(.92f, .59f));
        return y + 184f;
    }

    private float CreateAdvisorEchoCard(float y, RunHistoryData history)
    {
        if (history == null || history.advisorEchoes == null || history.advisorEchoes.Count == 0)
            return y;

        float height = 148f + Mathf.Min(history.advisorEchoes.Count, 3) * 48f;
        GameObject card = CreateCard("AdvisorEcho", y, height);
        TMP_Text title = CreateText(card.transform, "共谋回声", 38f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(title.rectTransform, new Vector2(.025f, .76f), new Vector2(.97f, .95f));

        List<string> lines = new List<string>();
        int limit = Mathf.Min(history.advisorEchoes.Count, 3);
        for (int i = 0; i < limit; i++)
        {
            AdvisorEchoData echo = history.advisorEchoes[i];
            if (echo == null) continue;
            string verdict = echo.accepted ? "主将采纳" : "主将未采纳";
            lines.Add("参谋" + (string.IsNullOrWhiteSpace(echo.guestLabel) ? "" : "「" + echo.guestLabel + "」") +
                "建议“" + Compact(echo.optionText, 18) + "”，" + verdict + "。" +
                (string.IsNullOrWhiteSpace(echo.reason) ? "" : "\n理由：" + Compact(echo.reason, 24)));
        }
        TMP_Text body = CreateText(card.transform, string.Join("\n", lines), 34f,
            TextAlignmentOptions.Center, TextPrimary, true);
        SetAnchored(body.rectTransform, new Vector2(.04f, .12f), new Vector2(.96f, .72f));
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
        return card;
    }

    private void CreateLegend(Transform parent, string label, Color color, float x, float y)
    {
        GameObject mark = CreateUIObject(label + "Mark", parent);
        SetAnchored(mark.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(x + .026f, y + .08f));
        mark.AddComponent<Image>().color = color;
        TMP_Text text = CreateText(parent, label, 28f, TextAlignmentOptions.Left, TextMuted, false);
        SetAnchored(text.rectTransform, new Vector2(x + .032f, y - .01f), new Vector2(x + .15f, y + .09f));
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
            case 500217: return "史实结局：曹操夜袭乌巢，焚毁袁军粮秣，袁绍军势由此转弱，官渡战局转向曹操。";
            case 200314: return "史实结局：曹操最终守住官渡并取胜；本局的撤退失序属于架空分支。";
            case 300416: return "史实结局：许攸来投并献出乌巢粮道情报；本局假设这份情报未被采纳。";
            case 500313: return "史实结局：乌巢夜袭重创袁军补给；本局将行动未能彻底奏效写作架空结果。";
            case 500320: return "史实结局：曹操击败袁绍并奠定北方优势；本局的战局未决属于架空推演。";
            case 500411: return "史实结局：曹操以乌巢奇袭扭转战局；本局将奇袭延伸为直取袁绍本阵的架空结果。";
            case 500418: return "史实结局：曹军最终取胜；本局的高风险突击失败属于架空分支。";
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
        text.overflowMode = TextOverflowModes.Ellipsis;
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
