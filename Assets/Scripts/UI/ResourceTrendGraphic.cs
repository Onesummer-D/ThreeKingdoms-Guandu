using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResourceTrendGraphic : MaskableGraphic
{
    private readonly List<ResourceSnapshotData> points = new List<ResourceSnapshotData>();
    private static readonly Color GridColor = new Color(.38f, .45f, .47f, .28f);
    private static readonly Color[] SeriesColors =
    {
        new Color(.84f, .32f, .28f, 1f),
        new Color(.91f, .70f, .25f, 1f),
        new Color(.35f, .78f, .68f, 1f),
        new Color(.64f, .48f, .84f, 1f)
    };

    public void SetData(IList<ResourceSnapshotData> source)
    {
        points.Clear();
        if (source != null)
            for (int i = 0; i < source.Count; i++)
                if (source[i] != null) points.Add(source[i]);
        SetVerticesDirty();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        float left = rect.xMin + 34f;
        float right = rect.xMax - 24f;
        float bottom = rect.yMin + 22f;
        float top = rect.yMax - 18f;

        for (int i = 0; i <= 4; i++)
        {
            float y = Mathf.Lerp(bottom, top, i / 4f);
            AddLine(vertexHelper, new Vector2(left, y), new Vector2(right, y), 2f, GridColor);
        }
        AddLine(vertexHelper, new Vector2(left, bottom), new Vector2(left, top), 3f,
            new Color(.55f, .62f, .63f, .65f));
        AddLine(vertexHelper, new Vector2(left, bottom), new Vector2(right, bottom), 3f,
            new Color(.55f, .62f, .63f, .65f));
        if (points.Count == 0)
        {
            AddLine(vertexHelper, new Vector2(left, Mathf.Lerp(bottom, top, .5f)),
                new Vector2(right, Mathf.Lerp(bottom, top, .5f)), 5f,
                new Color(.72f, .70f, .52f, .72f));
            return;
        }

        for (int series = 0; series < 4; series++)
        {
            Vector2 previous = GetPoint(points[0], series, 0, points.Count, left, right, bottom, top);
            AddDot(vertexHelper, previous, 8f, SeriesColors[series]);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 next = GetPoint(points[i], series, i, points.Count, left, right, bottom, top);
                AddLine(vertexHelper, previous, next, 6f, SeriesColors[series]);
                AddDot(vertexHelper, next, 8f, SeriesColors[series]);
                previous = next;
            }
            if (points.Count == 1)
                AddLine(vertexHelper, previous - Vector2.right * 46f,
                    previous + Vector2.right * 46f, 4f, SeriesColors[series]);
        }
    }

    private static Vector2 GetPoint(ResourceSnapshotData snapshot, int series, int index, int count,
        float left, float right, float bottom, float top)
    {
        float value = series == 0 ? snapshot.troop : series == 1 ? snapshot.food : series == 2 ? snapshot.strategy : snapshot.risk;
        float x = count <= 1 ? Mathf.Lerp(left, right, .5f) : Mathf.Lerp(left, right, index / (float)(count - 1));
        float y = Mathf.Lerp(bottom, top, Mathf.Clamp01(value / 100f));
        return new Vector2(x, y);
    }

    private static void AddLine(VertexHelper helper, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Vector2 direction = (to - from).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector2.right;
        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * .5f;
        int start = helper.currentVertCount;
        helper.AddVert(from - normal, color, Vector2.zero);
        helper.AddVert(from + normal, color, Vector2.zero);
        helper.AddVert(to + normal, color, Vector2.zero);
        helper.AddVert(to - normal, color, Vector2.zero);
        helper.AddTriangle(start, start + 1, start + 2);
        helper.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddDot(VertexHelper helper, Vector2 center, float radius, Color color)
    {
        int start = helper.currentVertCount;
        helper.AddVert(center + new Vector2(-radius, -radius), color, Vector2.zero);
        helper.AddVert(center + new Vector2(-radius, radius), color, Vector2.zero);
        helper.AddVert(center + new Vector2(radius, radius), color, Vector2.zero);
        helper.AddVert(center + new Vector2(radius, -radius), color, Vector2.zero);
        helper.AddTriangle(start, start + 1, start + 2);
        helper.AddTriangle(start, start + 2, start + 3);
    }
}
