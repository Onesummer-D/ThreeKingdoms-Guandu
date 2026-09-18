using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ResourceTrendViewMode
{
    Compact,
    Detail
}

/// <summary>
/// Draws the resource timeline with ordinary UI Images and TMP labels.
/// Compact and Detail share the same points but reserve different margins and
/// label densities so a small report card never inherits the detail layout.
/// </summary>
public sealed class ResourceTrendPlotUI : MonoBehaviour
{
    private static readonly Color GridColor = new Color(.38f, .45f, .47f, .42f);
    private static readonly Color AxisColor = new Color(.55f, .62f, .63f, .78f);
    private static readonly Color EmptyColor = new Color(.72f, .70f, .52f, .82f);
    private static readonly Color LabelColor = new Color(.88f, .90f, .89f, 1f);
    private static readonly Color[] SeriesColors =
    {
        new Color(.84f, .32f, .28f, 1f),
        new Color(.91f, .70f, .25f, 1f),
        new Color(.35f, .78f, .68f, 1f),
        new Color(.64f, .48f, .84f, 1f)
    };
    private static readonly string[] MarkerGlyphs = { "●", "◆", "▲", "□" };

    private readonly List<ResourceSnapshotData> points = new List<ResourceSnapshotData>();
    private ResourceTrendViewMode viewMode = ResourceTrendViewMode.Detail;
    private bool dirty = true;
    private float renderedWidth = -1f;
    private float renderedHeight = -1f;

    public void SetData(IList<ResourceSnapshotData> source)
    {
        SetData(source, ResourceTrendViewMode.Detail);
    }

    public void SetData(IList<ResourceSnapshotData> source, ResourceTrendViewMode mode)
    {
        points.Clear();
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null) points.Add(source[i]);
        }
        viewMode = mode;
        dirty = true;
    }

    private void OnEnable()
    {
        dirty = true;
    }

    private void LateUpdate()
    {
        Rect rect = GetComponent<RectTransform>().rect;
        if (rect.width <= 2f || rect.height <= 2f) return;
        if (!dirty && Mathf.Abs(rect.width - renderedWidth) < .5f &&
            Mathf.Abs(rect.height - renderedHeight) < .5f) return;
        Rebuild(rect);
    }

    private void Rebuild(Rect rect)
    {
        Canvas.ForceUpdateCanvases();
        ClearChildren();

        bool detail = viewMode == ResourceTrendViewMode.Detail;
        float left = rect.xMin + (detail ? 58f : 48f);
        float right = rect.xMax - (detail ? 22f : 18f);
        float bottom = rect.yMin + (detail ? 58f : 45f);
        float top = rect.yMax - (detail ? 20f : 18f);
        if (right <= left || top <= bottom) return;

        int[] ticks = detail ? new[] { 100, 75, 50, 25, 0 } : new[] { 100, 50, 0 };
        for (int i = 0; i < ticks.Length; i++)
        {
            float normalized = ticks[i] / 100f;
            float y = Mathf.Lerp(bottom, top, normalized);
            CreateLine("Grid" + ticks[i], new Vector2(left, y), new Vector2(right, y),
                detail ? 2.5f : 2f, GridColor);
            CreateAxisLabel("YLabel" + ticks[i], ticks[i].ToString(),
                new Vector2(rect.xMin + (detail ? 31f : 29f), y), detail ? 31f : 29f,
                TextAlignmentOptions.Right, new Vector2(detail ? 58f : 54f, detail ? 40f : 36f));
        }
        CreateLine("AxisY", new Vector2(left, bottom), new Vector2(left, top), 3f, AxisColor);
        CreateLine("AxisX", new Vector2(left, bottom), new Vector2(right, bottom), 3f, AxisColor);

        if (points.Count == 0)
        {
            CreateLine("EmptyTimeline", new Vector2(left, Mathf.Lerp(bottom, top, .5f)),
                new Vector2(right, Mathf.Lerp(bottom, top, .5f)), 5f, EmptyColor);
            CreateAxisLabel("EmptyLabel", "暂无资源记录",
                new Vector2((left + right) * .5f, (bottom + top) * .5f), detail ? 30f : 28f,
                TextAlignmentOptions.Center, new Vector2(detail ? 240f : 205f, detail ? 40f : 36f));
            renderedWidth = rect.width;
            renderedHeight = rect.height;
            dirty = false;
            return;
        }

        for (int series = 0; series < 4; series++)
        {
            Vector2 previous = GetPoint(points[0], series, 0, points.Count, left, right, bottom, top);
            CreateDot("Point_" + series + "_0", previous, detail ? 9f : 7f, series);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 next = GetPoint(points[i], series, i, points.Count, left, right, bottom, top);
                if (series == 3)
                    CreateDashedLine("RiskLine_" + i, previous, next, detail ? 5f : 4f, SeriesColors[series]);
                else
                    CreateLine("Line_" + series + "_" + i, previous, next, detail ? 6f : 5f, SeriesColors[series]);
                CreateDot("Point_" + series + "_" + i, next, detail ? 9f : 7f, series);
                previous = next;
            }
            if (points.Count == 1)
            {
                CreateLine("SinglePoint_" + series, previous - Vector2.right * 46f,
                    previous + Vector2.right * 46f, detail ? 4f : 3f, SeriesColors[series]);
            }
        }

        List<int> labelIndices = GetLabelIndices(points.Count, 5);
        for (int i = 0; i < labelIndices.Count; i++)
        {
            int index = labelIndices[i];
            Vector2 point = GetPoint(points[index], 0, index, points.Count, left, right, bottom, top);
            bool first = index == 0;
            bool last = index == points.Count - 1;
            float labelWidth = detail ? 164f : 126f;
            string label = GetPointLabel(points[index], index, points.Count, detail);
            TextAlignmentOptions alignment = first ? TextAlignmentOptions.Left :
                last ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            CreateAxisLabel("XLabel" + index, label,
                new Vector2(point.x, rect.yMin + (detail ? 22f : 16f)),
                detail ? 32f : 30f, alignment, new Vector2(labelWidth, detail ? 46f : 40f),
                first ? new Vector2(0f, .5f) : last ? new Vector2(1f, .5f) : new Vector2(.5f, .5f));
        }

        renderedWidth = rect.width;
        renderedHeight = rect.height;
        dirty = false;
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void CreateDot(string name, Vector2 center, float radius, int series)
    {
        GameObject halo = new GameObject(name + "Halo", typeof(RectTransform), typeof(Image));
        halo.transform.SetParent(transform, false);
        RectTransform haloRect = halo.GetComponent<RectTransform>();
        SetCentered(haloRect, center, new Vector2(radius * 2.5f, radius * 2.5f));
        Image haloImage = halo.GetComponent<Image>();
        haloImage.color = new Color(.02f, .035f, .045f, .92f);
        haloImage.raycastTarget = false;

        GameObject marker = new GameObject(name + "Marker", typeof(RectTransform));
        marker.transform.SetParent(transform, false);
        TextMeshProUGUI markerText = marker.AddComponent<TextMeshProUGUI>();
        ConfigureText(markerText, MarkerGlyphs[series], radius * 2.7f, SeriesColors[series],
            TextAlignmentOptions.Center, false);
        markerText.fontStyle = FontStyles.Bold;
        SetCentered(marker.GetComponent<RectTransform>(), center, new Vector2(radius * 3.2f, radius * 3.2f));
    }

    private void CreateDashedLine(string name, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < .5f)
        {
            CreateLine(name, from, to, thickness, color);
            return;
        }
        Vector2 direction = delta / length;
        const float dash = 14f;
        const float gap = 8f;
        int segment = 0;
        for (float offset = 0f; offset < length; offset += dash + gap)
        {
            float end = Mathf.Min(offset + dash, length);
            CreateLine(name + "_" + segment, from + direction * offset,
                from + direction * end, thickness, color);
            segment++;
        }
    }

    private GameObject CreateLine(string name, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < .5f) length = .5f;
        GameObject line = new GameObject(name, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(transform, false);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = (from + to) * .5f;
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Image image = line.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return line;
    }

    private void CreateAxisLabel(string name, string value, Vector2 position, float fontSize,
        TextAlignmentOptions alignment, Vector2 size)
    {
        CreateAxisLabel(name, value, position, fontSize, alignment, size, new Vector2(.5f, .5f));
    }

    private void CreateAxisLabel(string name, string value, Vector2 position, float fontSize,
        TextAlignmentOptions alignment, Vector2 size, Vector2 pivot)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        ConfigureText(label, value, fontSize, LabelColor, alignment, true);
        label.fontWeight = FontWeight.SemiBold;
        // These labels are deliberately short and should stay legible at the
        // authored size. Auto-sizing made the chart appear to solve layout by
        // shrinking its typography when the report card was narrow.
        label.enableAutoSizing = false;
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void ConfigureText(TextMeshProUGUI text, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, bool wrap)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.color = color;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static List<int> GetLabelIndices(int count, int maximum)
    {
        List<int> indices = new List<int>();
        if (count <= 0) return indices;
        if (count <= maximum)
        {
            for (int i = 0; i < count; i++) indices.Add(i);
            return indices;
        }
        for (int i = 0; i < maximum; i++)
        {
            int index = Mathf.RoundToInt(i * (count - 1) / (float)(maximum - 1));
            if (!indices.Contains(index)) indices.Add(index);
        }
        return indices;
    }

    private static string GetPointLabel(ResourceSnapshotData snapshot, int index, int count, bool detail)
    {
        if (count <= 1) return "起点/终点";
        if (index == 0) return "起点";
        if (index == count - 1) return "终点";
        string label = snapshot != null ? snapshot.label : string.Empty;
        if (string.IsNullOrWhiteSpace(label)) label = "决策" + index;
        label = label.Replace("决策 · ", string.Empty).Replace("决策·", string.Empty).Trim();
        return ShortNodeLabel(label, index, detail ? 6 : 5);
    }

    private static string ShortNodeLabel(string value, int index, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return "决策";
        value = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (value.Length <= maxCharacters) return value;

        if (value.Contains("观望") || value.Contains("按兵")) return "观望不攻";
        if (value.Contains("坚守")) return "坚守官渡";
        if (value.Contains("发石车")) return "发石车";
        if (value.Contains("诈败")) return "诈败设伏";
        if (value.Contains("地道") || value.Contains("埋伏")) return "挖地道";
        if (value.Contains("回许都") || value.Contains("回守") || value.Contains("暂回")) return "回守许都";
        if (value.Contains("粮草")) return "保住粮道";
        if (value.Contains("亲征")) return "亲征";
        if (value.Contains("遣将")) return "遣将出击";
        if (value.Contains("直取")) return "直取袁绍";
        if (value.Contains("坦诚")) return "坦诚相告";
        if (value.Contains("试探")) return "试探许攸";
        if (value.Contains("斩首")) return "斩首许攸";
        if (value.Contains("恶化")) return "战局恶化";
        if (value.Contains("变化")) return "战局变化";

        int separator = value.IndexOfAny(new[] { '，', '。', '；', '：', '、', ',', '.', ';' });
        if (separator > 0 && separator <= maxCharacters)
            return value.Substring(0, separator).Trim();
        return "决策" + index;
    }

    private static Vector2 GetPoint(ResourceSnapshotData snapshot, int series, int index, int count,
        float left, float right, float bottom, float top)
    {
        float value = series == 0 ? snapshot.troop : series == 1 ? snapshot.food :
            series == 2 ? snapshot.strategy : snapshot.risk;
        float x = count <= 1 ? Mathf.Lerp(left, right, .5f) :
            Mathf.Lerp(left, right, index / (float)(count - 1));
        float y = Mathf.Lerp(bottom, top, Mathf.Clamp01(value / 100f));
        return new Vector2(x, y);
    }
}
