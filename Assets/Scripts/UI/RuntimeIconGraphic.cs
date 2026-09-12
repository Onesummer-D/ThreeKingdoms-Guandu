using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small resolution-independent UI icons. The shapes follow familiar desktop
/// gear, speaker and exit-arrow conventions and require no runtime asset load.
/// </summary>
public sealed class RuntimeIconGraphic : MaskableGraphic
{
    public enum IconKind { Gear, Speaker, Exit }

    [SerializeField] private IconKind kind;
    [SerializeField] private bool slashed;
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public IconKind Kind
    {
        get => kind;
        set { kind = value; SetVerticesDirty(); }
    }

    public bool Slashed
    {
        get => slashed;
        set { slashed = value; SetVerticesDirty(); }
    }

    /// <summary>
    /// Creates a tiny local sprite fallback for builds where a custom UGUI
    /// graphic is not rebuilt until the first canvas refresh. This keeps the
    /// icon visible without shipping an external package or loading it online.
    /// </summary>
    public static Sprite CreateSprite(IconKind iconKind, Color tint, bool isSlashed = false)
    {
        string key = iconKind + ":" + ColorUtility.ToHtmlStringRGBA(tint) + ":" + isSlashed;
        Sprite cached;
        if (SpriteCache.TryGetValue(key, out cached)) return cached;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        texture.name = "RuntimeIcon_" + iconKind;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        if (iconKind == IconKind.Gear) DrawGearPixels(pixels, size, tint);
        else if (iconKind == IconKind.Speaker) DrawSpeakerPixels(pixels, size, tint);
        else DrawExitPixels(pixels, size, tint);
        if (isSlashed) DrawLinePixels(pixels, size, new Vector2(15f, 49f),
            new Vector2(49f, 15f), 4.5f, tint);

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(.5f, .5f), size);
        sprite.name = "RuntimeIconSprite_" + iconKind;
        SpriteCache.Add(key, sprite);
        return sprite;
    }

    private static void DrawGearPixels(Color[] pixels, int size, Color tint)
    {
        Vector2 center = new Vector2(size * .5f, size * .5f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float radius = offset.magnitude;
                float angle = Mathf.Atan2(offset.y, offset.x);
                float sector = Mathf.Abs(Mathf.Cos(angle * 4f));
                bool ring = radius >= 13f && radius <= 20f;
                bool tooth = radius > 20f && radius < 25f && sector > .82f;
                if ((ring || tooth) && radius > 7f) pixels[y * size + x] = tint;
            }
    }

    private static void DrawSpeakerPixels(Color[] pixels, int size, Color tint)
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cy = y - size * .5f;
                bool body = x >= 12f && x <= 23f && Mathf.Abs(cy) <= 7f;
                float triangleEdge = 7f + Mathf.Max(0f, x - 23f) * .78f;
                bool cone = x >= 23f && x <= 37f && Mathf.Abs(cy) <= triangleEdge;
                if (body || cone) pixels[y * size + x] = tint;
            }
        Vector2 center = new Vector2(34f, 32f);
        DrawArcPixels(pixels, size, center, 16f, -52f, 52f, 3f, tint);
        DrawArcPixels(pixels, size, center, 25f, -52f, 52f, 3f, tint);
    }

    private static void DrawExitPixels(Color[] pixels, int size, Color tint)
    {
        DrawLinePixels(pixels, size, new Vector2(16f, 17f), new Vector2(16f, 47f), 4f, tint);
        DrawLinePixels(pixels, size, new Vector2(16f, 17f), new Vector2(34f, 17f), 4f, tint);
        DrawLinePixels(pixels, size, new Vector2(16f, 47f), new Vector2(34f, 47f), 4f, tint);
        DrawLinePixels(pixels, size, new Vector2(29f, 32f), new Vector2(50f, 32f), 4f, tint);
        DrawLinePixels(pixels, size, new Vector2(50f, 32f), new Vector2(41f, 41f), 4f, tint);
        DrawLinePixels(pixels, size, new Vector2(50f, 32f), new Vector2(41f, 23f), 4f, tint);
    }

    private static void DrawArcPixels(Color[] pixels, int size, Vector2 center, float radius,
        float startDegrees, float endDegrees, float thickness, Color tint)
    {
        Vector2 previous = center + Direction(startDegrees) * radius;
        for (int i = 1; i <= 18; i++)
        {
            float angle = Mathf.Lerp(startDegrees, endDegrees, i / 18f);
            Vector2 next = center + Direction(angle) * radius;
            DrawLinePixels(pixels, size, previous, next, thickness, tint);
            previous = next;
        }
    }

    private static void DrawLinePixels(Color[] pixels, int size, Vector2 from, Vector2 to,
        float thickness, Color tint)
    {
        float radius = thickness * .5f;
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - radius));
        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(from.x, to.x) + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - radius));
        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(from.y, to.y) + radius));
        Vector2 line = to - from;
        float lengthSquared = Mathf.Max(.001f, line.sqrMagnitude);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(point - from, line) / lengthSquared);
                if ((point - Vector2.Lerp(from, to, t)).sqrMagnitude <= radius * radius)
                    pixels[y * size + x] = tint;
            }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float size = Mathf.Min(r.width, r.height);
        Vector2 c = r.center;
        if (kind == IconKind.Gear) DrawGear(vh, c, size);
        else if (kind == IconKind.Speaker) DrawSpeaker(vh, c, size);
        else DrawExit(vh, c, size);
        if (slashed) AddLine(vh, c + new Vector2(-size * .34f, size * .34f),
            c + new Vector2(size * .34f, -size * .34f), size * .09f, color);
    }

    private void DrawGear(VertexHelper vh, Vector2 c, float s)
    {
        const int segments = 28;
        float inner = s * .22f;
        float outer = s * .34f;
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * 2f * i / segments;
            float a1 = Mathf.PI * 2f * (i + 1) / segments;
            AddRingQuad(vh, c, inner, outer, a0, a1, color);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.PI * 2f * i / 8f;
            Vector2 start = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * .30f;
            Vector2 end = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * .43f;
            AddLine(vh, start, end, s * .13f, color);
        }
        AddDisc(vh, c, s * .095f, 20, color);
    }

    private void DrawSpeaker(VertexHelper vh, Vector2 c, float s)
    {
        Color tint = color;
        Vector2[] poly =
        {
            c + new Vector2(-s*.39f, -s*.13f), c + new Vector2(-s*.20f, -s*.13f),
            c + new Vector2(s*.03f, -s*.34f), c + new Vector2(s*.03f, s*.34f),
            c + new Vector2(-s*.20f, s*.13f), c + new Vector2(-s*.39f, s*.13f)
        };
        AddPolygon(vh, poly, tint);
        AddArc(vh, c + new Vector2(s*.02f, 0f), s*.24f, -55f, 55f, s*.055f, tint);
        AddArc(vh, c + new Vector2(s*.02f, 0f), s*.39f, -55f, 55f, s*.055f, tint);
    }

    private void DrawExit(VertexHelper vh, Vector2 c, float s)
    {
        Color tint = color;
        float left = c.x - s * .34f;
        float right = c.x + s * .10f;
        float top = c.y + s * .34f;
        float bottom = c.y - s * .34f;
        AddLine(vh, new Vector2(left, bottom), new Vector2(left, top), s*.065f, tint);
        AddLine(vh, new Vector2(left, top), new Vector2(right, top), s*.065f, tint);
        AddLine(vh, new Vector2(left, bottom), new Vector2(right, bottom), s*.065f, tint);
        AddLine(vh, new Vector2(c.x-s*.06f, c.y), new Vector2(c.x+s*.38f, c.y), s*.075f, tint);
        AddLine(vh, new Vector2(c.x+s*.38f, c.y), new Vector2(c.x+s*.20f, c.y+s*.17f), s*.075f, tint);
        AddLine(vh, new Vector2(c.x+s*.38f, c.y), new Vector2(c.x+s*.20f, c.y-s*.17f), s*.075f, tint);
    }

    private static void AddArc(VertexHelper vh, Vector2 c, float radius, float startDeg,
        float endDeg, float thickness, Color tint)
    {
        const int steps = 10;
        Vector2 previous = c + Direction(startDeg) * radius;
        for (int i = 1; i <= steps; i++)
        {
            float angle = Mathf.Lerp(startDeg, endDeg, i / (float)steps);
            Vector2 next = c + Direction(angle) * radius;
            AddLine(vh, previous, next, thickness, tint);
            previous = next;
        }
    }

    private static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private static void AddRingQuad(VertexHelper vh, Vector2 c, float inner, float outer,
        float a0, float a1, Color tint)
    {
        Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
        Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
        int start = vh.currentVertCount;
        vh.AddVert(c + d0 * inner, tint, Vector2.zero);
        vh.AddVert(c + d0 * outer, tint, Vector2.zero);
        vh.AddVert(c + d1 * outer, tint, Vector2.zero);
        vh.AddVert(c + d1 * inner, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddDisc(VertexHelper vh, Vector2 c, float radius, int segments, Color tint)
    {
        int center = vh.currentVertCount;
        vh.AddVert(c, tint, Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.PI * 2f * i / segments;
            vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, Vector2.zero);
            if (i > 0) vh.AddTriangle(center, center + i, center + i + 1);
        }
    }

    private static void AddPolygon(VertexHelper vh, Vector2[] points, Color tint)
    {
        if (points == null || points.Length < 3) return;
        int start = vh.currentVertCount;
        for (int i = 0; i < points.Length; i++) vh.AddVert(points[i], tint, Vector2.zero);
        for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
    }

    private static void AddLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color tint)
    {
        Vector2 direction = (to - from).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector2.right;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * .5f;
        int start = vh.currentVertCount;
        vh.AddVert(from - normal, tint, Vector2.zero);
        vh.AddVert(from + normal, tint, Vector2.zero);
        vh.AddVert(to + normal, tint, Vector2.zero);
        vh.AddVert(to - normal, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
