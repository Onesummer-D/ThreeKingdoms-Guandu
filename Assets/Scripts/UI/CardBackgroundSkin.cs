using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central mapping of the four illustrator-supplied backgrounds. All usages
/// gracefully retain their original color when the PNG has not been imported.
/// Import PNGs into Assets/Resources/CardSkins/ with these exact names.
/// </summary>
public static class CardBackgroundSkin
{
    public enum Style { SmallBanner, LightPanel, DarkPanel, LongPoster }

    public static bool Apply(Image target, Style style)
    {
        if (target == null) return false;
        string asset = style == Style.SmallBanner ? "SmallBanner"
            : style == Style.LightPanel ? "LightPanel"
            : style == Style.DarkPanel ? "DarkPanel" : "LongPoster";
        Texture2D tex = Resources.Load<Texture2D>("CardSkins/" + asset);
        if (tex == null)
        {
            target.color = style == Style.LightPanel
                ? new Color(.075f, .105f, .14f, 1f)
                : style == Style.SmallBanner
                    ? new Color(.09f, .11f, .14f, 1f)
                    : new Color(.07f, .10f, .13f, 1f);
            return false;
        }
        bool poster = style == Style.LongPoster;
        // The silhouettes are irregular: opaque outer image margins MUST be
        // kept rather than cropped by a Sprite atlas. Do not stretch the
        // ornamental end caps for the three horizontal styles.
        float edge = Mathf.Min(tex.width, tex.height) *
            (style == Style.SmallBanner ? .22f : .09f);
        Vector4 borders = poster ? Vector4.zero
            : new Vector4(edge, edge, edge, edge);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, borders);
        target.sprite = sprite;
        target.type = poster ? Image.Type.Simple : Image.Type.Sliced;
        target.color = Color.white;
        target.preserveAspect = false;
        return true;
    }
}
