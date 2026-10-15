using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One consistent wood/bronze button language for runtime-built panels.
/// Reuses the same scene button art as BattleReportUI (StartButton sprite).
/// Buttons remain normal Unity Buttons; no event listeners are replaced.
/// </summary>
public static class GameButtonSkin
{
    public enum Tone
    {
        Primary,     // confirm / accept / open
        Secondary,   // back / neutral action
        Quiet,       // cancel / withdraw
        Caution,     // reject or destructive action
        Highlight    // important optional discovery, e.g. 史官卡片
    }

    private static readonly Color Ivory = new Color(.97f, .92f, .81f, 1f);

    public static Tone ForLabel(string label)
    {
        switch (label)
        {
            case "采纳建议":
            case "确认":
            case "确定":
            case "保存":
            case "保存进度":
            case "开始游戏":
            case "以曹操身份出征":
            case "查看详情":
            case "打开图片":
            case "复制战局邀请":
            case "再邀一位参谋":
            case "分享海报":
                return Tone.Primary;
            case "史官卡片":
            case "问史官":
                return Tone.Highlight;
            case "撤销邀约":
            case "取消":
            case "返回":
            case "复制路径":
            case "关闭":
                return Tone.Quiet;
            case "拒绝建议":
            case "直接退出":
            case "退出游戏":
                return Tone.Caution;
            default:
                return Tone.Secondary;
        }
    }

    public static void Apply(Button button, Image background, Tone tone, Sprite preferredSprite = null)
    {
        if (button == null || background == null) return;
        Sprite visual = preferredSprite != null ? preferredSprite : FindSceneButtonSprite();
        if (visual != null)
        {
            background.sprite = visual;
            // Sliced only when source art really has 9-slice borders.
            background.type = visual.border.sqrMagnitude > 0.01f
                ? Image.Type.Sliced : Image.Type.Simple;
            background.preserveAspect = false;
        }

        background.color = Fill(tone);
        background.raycastTarget = true;
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.10f, 1.08f, 1.02f, 1f);
        colors.selectedColor = new Color(1.08f, 1.05f, 1f, 1f);
        colors.pressedColor = new Color(.82f, .78f, .68f, 1f);
        colors.disabledColor = new Color(.60f, .60f, .60f, .55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .12f;
        button.colors = colors;

        // Gold edge + dark shadow make buttons legible over the navy panels.
        Outline outline = background.GetComponent<Outline>();
        if (outline == null) outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = tone == Tone.Highlight
            ? new Color(.96f, .78f, .39f, .90f)
            : new Color(.82f, .63f, .34f, .52f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);
        outline.useGraphicAlpha = true;
        Shadow shadow = background.GetComponent<Shadow>();
        if (shadow == null)
        {
            // Outline derives from Shadow, so this check may return the
            // outline itself. A distinct drop shadow is optional.
        }
    }

    public static void Apply(Button button, Image background, string label, Sprite preferredSprite = null)
    {
        Apply(button, background, ForLabel(label), preferredSprite);
    }

    private static Color Fill(Tone tone)
    {
        switch (tone)
        {
            case Tone.Primary:   return new Color(.69f, .49f, .29f, 1f);
            case Tone.Caution:   return new Color(.57f, .30f, .26f, 1f);
            case Tone.Quiet:     return new Color(.48f, .41f, .34f, 1f);
            case Tone.Highlight: return new Color(.87f, .65f, .34f, 1f);
            default:             return new Color(.59f, .46f, .32f, 1f);
        }
    }

    private static Sprite FindSceneButtonSprite()
    {
        FinalUIManager ui = FinalUIManager.Instance;
        if (ui == null || ui.startMenuPanel == null) return null;

        // Resolve directly from the original start button, not from a
        // runtime modal's coloured Image or a temporary action control.
        Transform start = ui.startMenuPanel.transform.Find("StartButton");
        if (start != null)
        {
            Image image = start.GetComponent<Image>();
            if (image != null && image.sprite != null) return image.sprite;
        }
        Button[] buttons = ui.startMenuPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || button.name != "StartButton") continue;
            Image image = button.GetComponent<Image>();
            if (image != null && image.sprite != null) return image.sprite;
        }
        // The gallery and report already use the first source button as a
        // fallback; do the same when a custom StartMenu scene has renamed it.
        foreach (Button button in buttons)
        {
            if (button == null) continue;
            Image image = button.GetComponent<Image>();
            if (image != null && image.sprite != null) return image.sprite;
        }
        return null;
    }

    public static Color LabelColor => Ivory;
}
