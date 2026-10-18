using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Four illustration styles shared by modal panels and exported artwork.
/// An ornamental dark frame is larger than the functional content rectangle:
/// this is deliberate. The old implementation drew the frame INSIDE the
/// content rectangle, leaving headings, scrollbars and buttons outside the
/// painted boundary even when their RectTransforms were correct.
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
            target.color = style == Style.LightPanel ? new Color(.075f, .105f, .14f, 1f)
                : new Color(.07f, .10f, .13f, 1f);
            return false;
        }

        bool poster = style == Style.LongPoster;
        float edge = Mathf.Min(tex.width, tex.height) *
            (style == Style.SmallBanner ? .22f : .09f);
        Vector4 borders = poster ? Vector4.zero : new Vector4(edge, edge, edge, edge);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, borders);

        if (style == Style.DarkPanel)
        {
            // The image has an inset ornamental frame, transparent edges and
            // a wide decorated lower-right flourish. Let that artwork extend
            // PAST the interaction panel rather than forcing every existing
            // text and scrollbar anchor to live inside its much smaller frame.
            // Put it at sibling zero: all real content paints on top of it.
            target.sprite = null;
            target.color = new Color(.055f, .078f, .105f, .96f);
            const string childName = "OrnateFrameArt";
            Transform old = target.transform.Find(childName);
            GameObject art = old != null ? old.gameObject :
                new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = art.GetComponent<RectTransform>();
            rect.SetParent(target.transform, false);
            rect.anchorMin = new Vector2(-.11f, -.065f);
            rect.anchorMax = new Vector2(1.11f, 1.065f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            Image artImage = art.GetComponent<Image>();
            artImage.sprite = sprite;
            artImage.type = Image.Type.Sliced;
            artImage.color = Color.white;
            artImage.raycastTarget = false;
            return true;
        }

        target.sprite = sprite;
        target.type = poster ? Image.Type.Simple : Image.Type.Sliced;
        target.color = Color.white;
        target.preserveAspect = false;
        return true;
    }
}
