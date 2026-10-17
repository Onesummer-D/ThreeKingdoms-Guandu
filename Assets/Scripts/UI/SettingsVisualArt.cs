using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Lightweight hand-drawn ornamentation for the settings screen.  All artwork
/// is created at runtime so the Unity scene has no extra serialized references.
/// </summary>
/// <summary>
/// Sprite regions in the artist-approved, transparent 640x416 PNG.
/// Image coordinates below are from the TOP LEFT of the atlas; Sprite.Create
/// uses Unity's bottom-left origin, so conversion happens in one place.
/// </summary>
public static class SettingsArtAtlas
{
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites =
        new System.Collections.Generic.Dictionary<string, Sprite>();
    private static Texture2D atlas;

    public static Sprite Icon(SettingsGlyphKind kind)
    {
        switch (kind)
        {
            case SettingsGlyphKind.Lyre: return Get("lyre");
            case SettingsGlyphKind.Drum: return Get("drum");
            case SettingsGlyphKind.Sun: return Get("sun");
            default: return null;
        }
    }

    public static Sprite Get(string name)
    {
        Sprite cached;
        if (sprites.TryGetValue(name, out cached)) return cached;
        if (atlas == null) atlas = Resources.Load<Texture2D>("SettingsUI/SettingsAtlas");
        if (atlas == null) return null;

        Rect rect;
        Vector4 border = Vector4.zero;
        switch (name)
        {
            case "lyre":   rect = SourceRect(0, 0, 108, 108); break;
            case "drum":   rect = SourceRect(114, 0, 108, 108); break;
            case "sun":    rect = SourceRect(228, 0, 108, 108); break;
            case "arrow":  rect = SourceRect(352, 10, 85, 85); break;
            case "knob":   rect = SourceRect(453, 13, 80, 80); break;
            case "bubble": rect = SourceRect(536, 23, 100, 63);
                border = new Vector4(17f, 16f, 17f, 20f); break;
            case "track": rect = SourceRect(6, 130, 628, 65);
                border = new Vector4(57f, 10f, 57f, 10f); break;
            case "fill": rect = SourceRect(6, 213, 628, 65);
                border = new Vector4(57f, 10f, 57f, 10f); break;
            case "ticks": rect = SourceRect(6, 300, 628, 65); break;
            default: return null;
        }

        Sprite sprite = Sprite.Create(atlas, rect, Vector2.one * .5f,
            100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = "SettingsArt_" + name;
        sprites[name] = sprite;
        return sprite;
    }

    private static Rect SourceRect(int x, int top, int width, int height)
    {
        return new Rect(x, atlas.height - top - height, width, height);
    }
}

public enum SettingsGlyphKind { Lyre, Drum, Sun, ArrowLeft, ArrowRight, Corner }

public sealed class SettingsGlyphGraphic : MaskableGraphic
{
    [SerializeField] private SettingsGlyphKind kind;
    public SettingsGlyphKind Kind
    {
        get { return kind; }
        set { kind = value; SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float scale = Mathf.Min(rect.width, rect.height);
        if (scale <= 0f) return;
        Vector2 center = rect.center;
        Color32 gold = color;
        Color32 highlight = new Color(.99f, .86f, .60f, color.a);
        Color32 shadow = new Color(.31f, .17f, .10f, color.a);

        // Normalized local positions are centered in the graphic bounds.
        if (kind == SettingsGlyphKind.Lyre)
        {
            Line(vh, center, scale, -.28f, .31f, -.24f, -.18f, .075f, gold);
            Line(vh, center, scale, .28f, .31f, .24f, -.18f, .075f, gold);
            Line(vh, center, scale, -.28f, .31f, -.08f, .39f, .06f, gold);
            Line(vh, center, scale, .28f, .31f, .08f, .39f, .06f, gold);
            Line(vh, center, scale, -.24f, -.16f, .24f, -.16f, .085f, gold);
            Line(vh, center, scale, 0f, -.16f, 0f, -.38f, .085f, gold);
            Line(vh, center, scale, -.21f, .16f, .21f, .16f, .05f, highlight);
            for (int i = -2; i <= 2; i++)
                Line(vh, center, scale, i * .068f, .16f, i * .068f, -.16f, .012f, highlight);
        }
        else if (kind == SettingsGlyphKind.Drum)
        {
            Disc(vh, center, scale, 0f, .07f, .36f, shadow, 24);
            Disc(vh, center, scale, 0f, .07f, .29f, gold, 24);
            Disc(vh, center, scale, 0f, .07f, .21f, shadow, 24);
            Line(vh, center, scale, -.33f, .39f, .33f, -.35f, .09f, highlight);
            Line(vh, center, scale, .33f, .39f, -.33f, -.35f, .09f, gold);
            Disc(vh, center, scale, -.34f, .38f, .09f, gold, 12);
            Disc(vh, center, scale, .34f, .38f, .09f, gold, 12);
        }
        else if (kind == SettingsGlyphKind.Sun)
        {
            Disc(vh, center, scale, 0f, 0f, .22f, gold, 24);
            Disc(vh, center, scale, -.05f, .055f, .12f, highlight, 24);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                Line(vh, center, scale,
                    Mathf.Cos(angle) * .30f, Mathf.Sin(angle) * .30f,
                    Mathf.Cos(angle) * .45f, Mathf.Sin(angle) * .45f, .047f, gold);
            }
        }
        else if (kind == SettingsGlyphKind.ArrowLeft || kind == SettingsGlyphKind.ArrowRight)
        {
            float s = kind == SettingsGlyphKind.ArrowLeft ? -1f : 1f;
            Line(vh, center, scale, s * .20f, .23f, -s * .16f, 0f, .105f, gold);
            Line(vh, center, scale, -s * .16f, 0f, s * .20f, -.23f, .105f, gold);
        }
        else if (kind == SettingsGlyphKind.Corner)
        {
            Line(vh, center, scale, -.46f, .43f, .40f, .43f, .043f, gold);
            Line(vh, center, scale, -.46f, -.40f, -.46f, .43f, .043f, gold);
            Line(vh, center, scale, -.36f, .32f, -.16f, .32f, .037f, highlight);
            Line(vh, center, scale, -.36f, .32f, -.36f, .12f, .037f, highlight);
            Disc(vh, center, scale, -.36f, .32f, .058f, gold, 12);
            Line(vh, center, scale, -.28f, .26f, -.02f, .02f, .042f, gold);
        }
    }

    private static void Line(VertexHelper vh, Vector2 center, float scale,
        float x0, float y0, float x1, float y1, float width, Color32 tint)
    {
        Vector2 start = center + new Vector2(x0, y0) * scale;
        Vector2 end = center + new Vector2(x1, y1) * scale;
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < .001f) return;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (width * scale * .5f);
        int i = vh.currentVertCount;
        vh.AddVert(start - normal, tint, Vector2.zero);
        vh.AddVert(start + normal, tint, Vector2.up);
        vh.AddVert(end + normal, tint, Vector2.one);
        vh.AddVert(end - normal, tint, Vector2.right);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    private static void Disc(VertexHelper vh, Vector2 center, float scale,
        float x, float y, float radius, Color32 tint, int segments)
    {
        Vector2 origin = center + new Vector2(x, y) * scale;
        int i = vh.currentVertCount;
        vh.AddVert(origin, tint, new Vector2(.5f, .5f));
        for (int j = 0; j <= segments; j++)
        {
            float angle = j * Mathf.PI * 2f / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vh.AddVert(origin + direction * radius * scale, tint, direction * .5f + Vector2.one * .5f);
            if (j > 0) vh.AddTriangle(i, i + j, i + j + 1);
        }
    }
}

public static class SettingsMetalSprites
{
    private static Sprite rail, fill, knob, arrow;

    public static Sprite Rail { get { return rail ?? (rail = MakeHorizontal("SettingsRecessedRail", false)); } }
    public static Sprite Fill { get { return fill ?? (fill = MakeHorizontal("SettingsGoldFill", true)); } }
    public static Sprite Knob { get { return knob ?? (knob = MakeCircle()); } }
    public static Sprite Arrow { get { return arrow ?? (arrow = MakeArrowPlate()); } }

    private static Sprite MakeHorizontal(string name, bool golden)
    {
        const int w = 128, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float radius = 12f;
                float xx = Mathf.Max(radius, Mathf.Min(w - radius, x));
                float yy = h * .5f;
                float d = Mathf.Sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy));
                if (d > 15.5f) { pixels[y * w + x] = Color.clear; continue; }
                float t = y / (float)(h - 1);
                Color baseColor = golden
                    ? Color.Lerp(new Color(.33f, .18f, .09f), new Color(.96f, .74f, .40f), Mathf.Sin(t * Mathf.PI))
                    : Color.Lerp(new Color(.48f, .42f, .37f), new Color(.14f, .14f, .15f), Mathf.Sin(t * Mathf.PI));
                if (d > 13.0f) baseColor = new Color(.60f, .46f, .33f);
                else if (y > 23) baseColor = Color.Lerp(baseColor, golden ? new Color(.55f, .33f, .15f) : Color.black, .40f);
                else if (y < 7) baseColor = Color.Lerp(baseColor, Color.white, golden ? .22f : .09f);
                pixels[y * w + x] = baseColor;
            }
        tex.SetPixels(pixels); tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * .5f,
            100f, 0, SpriteMeshType.FullRect, new Vector4(16, 12, 16, 12));
        sprite.name = name;
        return sprite;
    }

    private static Sprite MakeCircle()
    {
        const int size = 96;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "SettingsEngravedKnob";
        tex.filterMode = FilterMode.Bilinear;
        Color[] p = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 d = new Vector2(x - size * .5f, y - size * .5f);
                float r = d.magnitude;
                if (r > 45f) { p[y * size + x] = Color.clear; continue; }
                Color c;
                if (r > 40f) c = new Color(.22f, .14f, .10f);
                else if (r > 35f) c = new Color(.80f, .63f, .37f);
                else if (r > 30f) c = new Color(.35f, .25f, .19f);
                else c = Color.Lerp(new Color(.29f, .23f, .20f), new Color(.76f, .67f, .52f),
                    Mathf.Clamp01((x + y + 35f) / (size * 2f)));
                if (x + y > 107 && r < 29f) c = Color.Lerp(c, Color.white, .12f);
                p[y * size + x] = c;
            }
        tex.SetPixels(p); tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f);
        sprite.name = tex.name;
        return sprite;
    }

    private static Sprite MakeArrowPlate()
    {
        const int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "SettingsBronzeStep";
        tex.filterMode = FilterMode.Bilinear;
        Color[] p = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float distance = Mathf.Max(Mathf.Abs(x - 31.5f), Mathf.Abs(y - 31.5f));
                if (distance > 30) { p[y * w + x] = Color.clear; continue; }
                Color c = distance > 26 ? new Color(.68f, .51f, .32f) :
                    Color.Lerp(new Color(.30f, .21f, .16f), new Color(.61f, .42f, .27f), y / 64f);
                p[y * w + x] = c;
            }
        tex.SetPixels(p); tex.Apply(false, true);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * .5f);
        sprite.name = tex.name;
        return sprite;
    }
}

/// <summary>Only decorative pointer feedback; Slider keeps full control of dragging.</summary>
public sealed class SettingsSliderFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IEndDragHandler
{
    private RectTransform handle;
    private Image handleImage;
    private GameObject tooltip;
    private TMP_Text tooltipText;
    private bool hovering, dragging;

    public void Initialize(RectTransform thumb, Image thumbImage, GameObject bubble, TMP_Text label)
    {
        handle = thumb; handleImage = thumbImage; tooltip = bubble; tooltipText = label;
        Refresh(0f);
    }

    public void Refresh(float value)
    {
        if (tooltipText != null) tooltipText.text = Mathf.RoundToInt(value * 100f) + "%";
        RefreshVisual();
    }
    public void OnPointerEnter(PointerEventData eventData) { hovering = true; RefreshVisual(); }
    public void OnPointerExit(PointerEventData eventData) { hovering = false; RefreshVisual(); }
    public void OnPointerDown(PointerEventData eventData) { dragging = true; RefreshVisual(); }
    public void OnPointerUp(PointerEventData eventData) { dragging = false; RefreshVisual(); }
    public void OnBeginDrag(PointerEventData eventData) { dragging = true; RefreshVisual(); }
    public void OnEndDrag(PointerEventData eventData) { dragging = false; RefreshVisual(); }

    private void OnDisable()
    {
        hovering = false; dragging = false; RefreshVisual();
    }
    private void RefreshVisual()
    {
        if (handle != null) handle.localScale = Vector3.one * (dragging ? 1.13f : hovering ? 1.06f : 1f);
        if (handleImage != null) handleImage.color = dragging
            ? new Color(1f, .93f, .72f) : Color.white;
        if (tooltip != null) tooltip.SetActive(dragging);
    }
}
