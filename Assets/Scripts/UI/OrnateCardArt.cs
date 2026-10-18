using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central four-asset skin registry. The artwork files are deliberately kept
/// apart from scripts so the art team can revise PNGs without touching logic.
/// No mask or RaycastTarget is added: buttons, sliders and scroll views keep
/// their original hit areas and behavior.
/// </summary>
public static class OrnateCardArt
{
    public enum Kind { Compact, Paper, Ink, Poster }

    private static readonly Dictionary<Kind, Sprite> Cache = new Dictionary<Kind, Sprite>();
    private static readonly HashSet<Kind> Missing = new HashSet<Kind>();

    private static string ResourcePath(Kind kind)
    {
        switch (kind)
        {
            case Kind.Compact: return "PanelArt/SmallNotice";
            case Kind.Paper: return "PanelArt/LightParchment";
            case Kind.Ink: return "PanelArt/DarkCloud";
            default: return "PanelArt/LongPoster";
        }
    }

    private static Sprite GetSprite(Kind kind)
    {
        Sprite sprite;
        if (Cache.TryGetValue(kind, out sprite)) return sprite;
        if (Missing.Contains(kind)) return null;
        Texture2D texture = Resources.Load<Texture2D>(ResourcePath(kind));
        if (texture == null)
        {
            Missing.Add(kind);
            return null;
        }

        // The imported images have large decorative corners and transparent
        // ornamented edges. Preserve those regions with nine-slicing, and
        // stretch only the quiet middle part of the image.
        float side = kind == Kind.Compact ? .16f : kind == Kind.Poster ? .12f : .13f;
        float end = kind == Kind.Poster ? .12f : kind == Kind.Compact ? .22f : .16f;
        Vector4 border = new Vector4(
            Mathf.Round(texture.width * side),
            Mathf.Round(texture.height * end),
            Mathf.Round(texture.width * side),
            Mathf.Round(texture.height * end));
        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.name = "PanelArt_" + kind;
        Cache.Add(kind, sprite);
        return sprite;
    }

    /// <summary>
    /// Reuses an existing Image component; returns false if the asset has not
    /// yet been imported, leaving the original colors/appearance untouched.
    /// </summary>
    public static bool Apply(Image target, Kind kind)
    {
        if (target == null) return false;
        Sprite sprite = GetSprite(kind);
        if (sprite == null) return false;
        bool hitArea = target.raycastTarget;
        target.sprite = sprite;
        target.type = Image.Type.Sliced;
        target.preserveAspect = false;
        target.color = Color.white;
        target.raycastTarget = hitArea;
        return true;
    }
}
