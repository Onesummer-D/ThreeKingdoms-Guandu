using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayExitUI : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(.01f, .015f, .025f, .86f);
    private static readonly Color PanelColor = new Color(.075f, .11f, .15f, .995f);
    private static readonly Color TextColor = new Color(.94f, .94f, .88f, 1f);
    private static readonly Color Accent = new Color(.89f, .71f, .32f, 1f);
    private static readonly Color Paper = new Color(.94f, .92f, .85f, .94f);

    private FinalUIManager manager;
    private GameObject exitButton;
    private GameObject overlay;
    private TMP_Text status;
    private TMP_Text question;
    private Button saveButton;
    private Button directButton;
    private Button confirmButton;
    private Sprite buttonSprite;
    private bool endingMode;

    public void Initialize(FinalUIManager owner, Transform root, Sprite sharedButtonSprite)
    {
        if (manager != null || owner == null || root == null) return;
        manager = owner;
        buttonSprite = sharedButtonSprite;
        CreateExitButton(root);
        CreateOverlay(root);
        SetEndingMode(false);
        SetVisible(false);
    }

    /// <summary>
    /// Ending screens keep the same bottom-right exit affordance, but the
    /// primary action starts a fresh run instead of writing another save.
    /// </summary>
    public void SetEndingMode(bool value)
    {
        endingMode = value;
        if (saveButton != null)
        {
            TMP_Text label = saveButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = endingMode ? "再玩一局" : "保存进度";
        }
    }

    public void SetVisible(bool visible)
    {
        if (exitButton != null) exitButton.SetActive(visible);
        if (!visible && overlay != null) overlay.SetActive(false);
    }

    private void CreateExitButton(Transform root)
    {
        exitButton = CreateObject("GameplayExitButton", root);
        RectTransform rect = exitButton.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        // Match the inspected bottom-right RectTransform values supplied in
        // the latest screenshot so the icon sits flush with that safe corner.
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(67.701f, 69.2f);
        Image bg = exitButton.AddComponent<Image>();
        bg.color = new Color(.12f, .15f, .18f, .88f);
        Outline outline = exitButton.AddComponent<Outline>();
        outline.effectColor = Accent;
        outline.effectDistance = new Vector2(3f, -3f);
        Button button = exitButton.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(Open);

        GameObject iconObject = CreateObject("ExitArrow", exitButton.transform);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        SetAnchored(iconRect, new Vector2(.19f, .19f), new Vector2(.81f, .81f));
        RuntimeIconGraphic icon = iconObject.AddComponent<RuntimeIconGraphic>();
        icon.Kind = RuntimeIconGraphic.IconKind.Exit;
        icon.color = Color.clear;
        icon.raycastTarget = false;
        GameObject fallbackObject = CreateObject("FallbackIcon", exitButton.transform);
        SetAnchored(fallbackObject.GetComponent<RectTransform>(), new Vector2(.19f, .19f), new Vector2(.81f, .81f));
        Image fallback = fallbackObject.AddComponent<Image>();
        fallback.sprite = RuntimeIconGraphic.CreateSprite(RuntimeIconGraphic.IconKind.Exit,
            new Color(.89f, .71f, .32f, 1f));
        fallback.preserveAspect = true;
        fallback.raycastTarget = false;
    }

    private void CreateOverlay(Transform root)
    {
        overlay = CreateObject("GameplayExitOverlay", root);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = OverlayColor;

        GameObject panel = CreateObject("ExitConfirmPanel", overlay.transform);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.30f, .34f), new Vector2(.70f, .66f));
        panel.AddComponent<Image>().color = PanelColor;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = Accent;
        outline.effectDistance = new Vector2(3f, -3f);

        question = CreateText(panel.transform, "确定要退出游戏吗？", 52f,
            TextAlignmentOptions.Center, TextColor);
        SetAnchored(question.rectTransform, new Vector2(.08f, .56f), new Vector2(.92f, .90f));

        saveButton = CreateButton(panel.transform, "保存进度", new Vector2(.08f, .18f), new Vector2(.47f, .45f));
        saveButton.onClick.AddListener(HandlePrimaryAction);
        directButton = CreateButton(panel.transform, "直接退出", new Vector2(.53f, .18f), new Vector2(.92f, .45f));
        directButton.onClick.AddListener(() => manager.ExitToMainMenuWithoutSaving());
        // The acknowledgement belongs under the saved-status title, not in
        // the right-hand action column where it obscured the message.
        confirmButton = CreateButton(panel.transform, "确定", new Vector2(.32f, .26f), new Vector2(.68f, .49f));
        confirmButton.onClick.AddListener(ResumeAfterSave);
        confirmButton.gameObject.SetActive(false);

        status = CreateText(panel.transform, "", 27f, TextAlignmentOptions.Center,
            new Color(.78f, .79f, .75f, 1f));
        SetAnchored(status.rectTransform, new Vector2(.06f, .02f), new Vector2(.94f, .16f));
        overlay.SetActive(false);
    }

    private void Open()
    {
        if (question != null) question.text = endingMode ? "本局已结束，要再玩一局吗？" : "确定要退出游戏吗？";
        if (status != null) status.text = "";
        if (saveButton != null) saveButton.gameObject.SetActive(true);
        if (directButton != null) directButton.gameObject.SetActive(true);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        SetEndingMode(endingMode);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    private void HandlePrimaryAction()
    {
        if (!endingMode)
        {
            SaveAndExit();
            return;
        }

        // Close the exit modal before the new-run brief opens. ResetRunState
        // then removes the completed-run actions and restores normal gameplay.
        if (overlay != null) overlay.SetActive(false);
        SetVisible(false);
        manager.StartReplay();
    }

    private void SaveAndExit()
    {
        string message;
        if (manager.SaveCurrentProgress(out message))
        {
            if (question != null) question.text = "进度已保存";
            if (status != null) status.text = "已保存到自动检查点，点击确定继续游戏";
            if (saveButton != null) saveButton.gameObject.SetActive(false);
            if (directButton != null) directButton.gameObject.SetActive(false);
            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
        }
        else if (status != null)
        {
            status.text = message;
        }
    }

    private void ResumeAfterSave()
    {
        if (overlay != null) overlay.SetActive(false);
        if (exitButton != null) exitButton.SetActive(true);
    }

    private Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max)
    {
        GameObject obj = CreateObject(label + "Button", parent);
        SetAnchored(obj.GetComponent<RectTransform>(), min, max);
        Image image = obj.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Simple : Image.Type.Sliced;
        image.color = new Color(.44f, .32f, .22f, 1f);
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(obj.transform, label, 36f, TextAlignmentOptions.Center, TextColor);
        Stretch(text.rectTransform);
        return button;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TMP_Text CreateText(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color)
    {
        TextMeshProUGUI text = CreateObject("Text", parent).AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
