using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Commander-name card shown at each fresh app launch, once per launch.
/// ID may be prefilled from this device, but the player must confirm it.
/// No remote login, network permission or account credentials required.
/// </summary>
public sealed class PlayerIdentityUI : MonoBehaviour
{
    private GameObject overlay;
    private TMP_InputField idInput;
    private TMP_Text errorText;
    private Sprite sourceButton;
    private bool initialized;

    public void Initialize(Transform canvasRoot, Transform mainMenu)
    {
        if (initialized || canvasRoot == null || mainMenu == null) return;
        initialized = true;

        Transform startButton = mainMenu.Find("StartButton");
        if (startButton != null)
        {
            Image img = startButton.GetComponent<Image>();
            if (img != null) sourceButton = img.sprite;
        }

        CreateIdentityOverlay(canvasRoot);
        // Always request confirmation when Unity starts, even if PlayerPrefs
        // contains a previously used display name.
        ShowEditor();
    }

    public void ShowEditor()
    {
        if (overlay == null) return;
        idInput.text = PlayerIdentityState.CurrentId;
        errorText.text = "";
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        idInput.ActivateInputField();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(idInput.gameObject);
    }

    private void CreateIdentityOverlay(Transform root)
    {
        overlay = Make("PlayerIdentityOverlay", root);
        Stretch(overlay.GetComponent<RectTransform>());
        Canvas canvas = overlay.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 85;
        overlay.AddComponent<GraphicRaycaster>();
        Image shade = overlay.AddComponent<Image>();
        shade.color = new Color(.012f, .02f, .025f, .92f);
        shade.raycastTarget = true;

        GameObject panel = Make("CommanderRegisterPanel", overlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.18f, .23f);
        panelRect.anchorMax = new Vector2(.82f, .77f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        Image paper = panel.AddComponent<Image>();
        paper.color = new Color(.075f, .105f, .14f, 1f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(.80f, .62f, .35f, .9f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = Text(panel.transform, "主将留名", 48f,
            TextAlignmentOptions.Center, new Color(.91f, .73f, .4f));
        Anchor(title.rectTransform, new Vector2(.06f, .76f), new Vector2(.94f, .95f));

        TMP_Text intro = Text(panel.transform,
            "为这一局征程定下名号", 35f,
            TextAlignmentOptions.Center, new Color(.87f, .86f, .80f));
        Anchor(intro.rectTransform, new Vector2(.06f, .57f), new Vector2(.94f, .76f));

        GameObject field = Make("CommanderIdInput", panel.transform);
        RectTransform fieldRect = field.GetComponent<RectTransform>();
        Anchor(fieldRect, new Vector2(.13f, .36f), new Vector2(.87f, .56f));
        Image fieldBg = field.AddComponent<Image>();
        fieldBg.color = new Color(.16f, .20f, .24f, 1f);
        idInput = field.AddComponent<TMP_InputField>();
        idInput.targetGraphic = fieldBg;
        idInput.lineType = TMP_InputField.LineType.SingleLine;
        idInput.characterLimit = PlayerIdentityState.MaxLength;
        idInput.caretWidth = 3;
        idInput.customCaretColor = true;
        idInput.caretColor = Color.white;
        idInput.selectionColor = new Color(.86f, .66f, .31f, .45f);
        idInput.onValidateInput = ValidateIdCharacter;

        GameObject viewport = Make("TextViewport", field.transform);
        Anchor(viewport.GetComponent<RectTransform>(), new Vector2(.035f, .09f), new Vector2(.965f, .91f));
        RectMask2D mask = viewport.AddComponent<RectMask2D>();
        TMP_Text inputText = Text(viewport.transform, "", 32f,
            TextAlignmentOptions.MidlineLeft, Color.white);
        Stretch(inputText.rectTransform);
        inputText.raycastTarget = false;
        TMP_Text hint = Text(viewport.transform, "例如：Guandu_07", 30f,
            TextAlignmentOptions.MidlineLeft, new Color(.62f, .66f, .68f));
        Stretch(hint.rectTransform);
        hint.raycastTarget = false;
        idInput.textViewport = viewport.GetComponent<RectTransform>();
        idInput.textComponent = (TextMeshProUGUI)inputText;
        idInput.placeholder = hint;
        idInput.text = PlayerIdentityState.CurrentId;

        errorText = Text(panel.transform, "", 22f, TextAlignmentOptions.Center,
            new Color(.94f, .62f, .51f));
        Anchor(errorText.rectTransform, new Vector2(.05f, .24f), new Vector2(.95f, .35f));

        Button confirm = CreateButton(panel.transform, "确认名号", 30f);
        Anchor(confirm.GetComponent<RectTransform>(),
            new Vector2(.37f, .075f), new Vector2(.63f, .225f));
        confirm.onClick.AddListener(SubmitId);

        overlay.SetActive(false);
    }

    private char ValidateIdCharacter(string current, int position, char next)
    {
        if ((next >= 'a' && next <= 'z') || (next >= 'A' && next <= 'Z') ||
            (next >= '0' && next <= '9') || next == '_' || next == '-')
            return next;
        return '\0';
    }

    private void SubmitId()
    {
        if (!PlayerIdentityState.TrySave(idInput.text, out string error))
        {
            errorText.text = error;
            idInput.ActivateInputField();
            return;
        }
        errorText.text = "";
        overlay.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private Button CreateButton(Transform parent, string label, float size)
    {
        GameObject go = Make(label + "Button", parent);
        Image image = go.AddComponent<Image>();
        Button button = go.AddComponent<Button>();
        GameButtonSkin.Apply(button, image, GameButtonSkin.Tone.Primary, sourceButton);
        TMP_Text text = Text(go.transform, label, size, TextAlignmentOptions.Center,
            GameButtonSkin.LabelColor);
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text Text(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject go = Make("Text", parent);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) label.font = font;
        label.text = value;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject Make(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }
}
