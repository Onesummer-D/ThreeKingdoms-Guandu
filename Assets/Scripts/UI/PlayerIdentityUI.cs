using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Commander-name card shown at each fresh app launch, once per launch.
/// This is a fresh display name for the current app session. No remote
/// login, reused saved ID, credentials, or persistent account required.
/// </summary>
public sealed class PlayerIdentityUI : MonoBehaviour
{
    private GameObject overlay;
    private TMP_InputField idInput;
    private TMP_Text errorText;
    private Sprite sourceButton;
    private Coroutine pendingFocus;
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
        // Show once at every app start with an empty field.
        ShowEditor();
    }

    public void ShowEditor()
    {
        if (overlay == null) return;
        if (pendingFocus != null)
            StopCoroutine(pendingFocus);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        // Never reuse the previous play session's ID.
        idInput.text = string.Empty;
        idInput.ForceLabelUpdate();
        errorText.text = string.Empty;
        pendingFocus = StartCoroutine(FocusInputWhenReady());
    }

    private IEnumerator FocusInputWhenReady()
    {
        // Initialize runs while the scene and EventSystem are still being
        // activated. Selecting immediately can leave an inert TMP caret.
        yield return null;
        yield return new WaitForEndOfFrame();
        if (overlay == null || !overlay.activeInHierarchy ||
            idInput == null || !idInput.gameObject.activeInHierarchy)
        {
            pendingFocus = null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        idInput.Select();
        idInput.ActivateInputField();
        pendingFocus = null;
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
        panelRect.anchorMin = new Vector2(.18f, .30f);
        panelRect.anchorMax = new Vector2(.82f, .84f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        Image paper = panel.AddComponent<Image>();
        paper.color = new Color(.075f, .105f, .14f, 1f);
        CardBackgroundSkin.Apply(paper, CardBackgroundSkin.Style.LightPanel);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(.80f, .62f, .35f, .9f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = Text(panel.transform, "主将留名", 48f,
            TextAlignmentOptions.Center, new Color(.91f, .73f, .4f));
        Anchor(title.rectTransform, new Vector2(.06f, .78f), new Vector2(.94f, .95f));
        if (paper.sprite != null) title.color = new Color(.36f, .22f, .13f, 1f);

        TMP_Text intro = Text(panel.transform,
            "为这一局征程定下名号", 35f,
            TextAlignmentOptions.Center, new Color(.87f, .86f, .80f));
        Anchor(intro.rectTransform, new Vector2(.06f, .65f), new Vector2(.94f, .84f));
        if (paper.sprite != null) intro.color = new Color(.30f, .26f, .20f, 1f);

        GameObject field = Make("CommanderIdInput", panel.transform);
        RectTransform fieldRect = field.GetComponent<RectTransform>();
        // Center the ivory field well INSIDE the painted paper border
        // (the original 74%-width box touched the decorative right edge).
        Anchor(fieldRect, new Vector2(.20f, .36f), new Vector2(.80f, .56f));
        Image fieldBg = field.AddComponent<Image>();
        // The host panel is cream parchment: keep the input ivory as well.
        fieldBg.color = new Color(.97f, .95f, .87f, .97f);
        Outline inputBorder = field.AddComponent<Outline>();
        inputBorder.effectColor = new Color(.54f, .39f, .23f, .8f);
        inputBorder.effectDistance = new Vector2(2f, -2f);
        idInput = field.AddComponent<TMP_InputField>();
        idInput.targetGraphic = fieldBg;
        idInput.contentType = TMP_InputField.ContentType.Standard;
        idInput.lineType = TMP_InputField.LineType.SingleLine;
        idInput.characterValidation = TMP_InputField.CharacterValidation.None;
        idInput.characterLimit = PlayerIdentityState.MaxLength;
        // TMP wraps Chinese IME composition in <u> tags while typing.
        // Render this formatting correctly but never allow stored markup.
        idInput.richText = true;
        idInput.isRichTextEditingAllowed = false;
        idInput.caretWidth = 3;
        idInput.customCaretColor = true;
        idInput.caretColor = new Color(.24f, .20f, .17f, 1f);
        idInput.selectionColor = new Color(.86f, .66f, .31f, .45f);
        // Do not discard Chinese/IME characters or other normal player names.
        // Validation is performed only at submission time.
        idInput.onSubmit.AddListener(_ => SubmitId());

        GameObject viewport = Make("TextViewport", field.transform);
        Anchor(viewport.GetComponent<RectTransform>(), new Vector2(.035f, .09f), new Vector2(.965f, .91f));
        RectMask2D mask = viewport.AddComponent<RectMask2D>();
        TMP_Text inputText = Text(viewport.transform, "", 32f,
            TextAlignmentOptions.MidlineLeft, new Color(.22f, .20f, .19f, 1f));
        inputText.richText = true;
        inputText.enableWordWrapping = false;
        inputText.overflowMode = TextOverflowModes.Overflow;
        Stretch(inputText.rectTransform);
        inputText.raycastTarget = false;
        TMP_Text hint = Text(viewport.transform, "例如：Guandu_07", 30f,
            TextAlignmentOptions.MidlineLeft, new Color(.39f, .33f, .28f, 1f));
        Stretch(hint.rectTransform);
        hint.raycastTarget = false;
        idInput.textViewport = viewport.GetComponent<RectTransform>();
        idInput.textComponent = (TextMeshProUGUI)inputText;
        idInput.fontAsset = ((TextMeshProUGUI)inputText).font;
        idInput.placeholder = hint;
        idInput.text = string.Empty;

        errorText = Text(panel.transform, "", 22f, TextAlignmentOptions.Center,
            new Color(.94f, .62f, .51f));
        Anchor(errorText.rectTransform, new Vector2(.05f, .24f), new Vector2(.95f, .35f));

        Button confirm = CreateButton(panel.transform, "确认名号", 30f);
        Anchor(confirm.GetComponent<RectTransform>(),
            new Vector2(.37f, .075f), new Vector2(.63f, .225f));
        confirm.onClick.AddListener(SubmitId);

        // The illustrated paper has asymmetric ribbons on its right, so the
        // optical content center sits LEFT of its mathematical RectTransform.
        // Shift the content, not the artwork, after establishing all anchors.
        foreach (RectTransform child in panel.transform)
            child.anchoredPosition += new Vector2(-22f, 0f);

        overlay.SetActive(false);
    }

    private void SubmitId()
    {
        if (overlay == null || !overlay.activeInHierarchy) return;
        if (!PlayerIdentityState.TrySave(idInput.text, out string error))
        {
            // Only short, actionable feedback; never show restrictive
            // ASCII-only instructions or silently filter typed characters.
            errorText.text = error;
            idInput.Select();
            idInput.ActivateInputField();
            return;
        }
        if (pendingFocus != null)
        {
            StopCoroutine(pendingFocus);
            pendingFocus = null;
        }
        errorText.text = string.Empty;
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
        if (pendingFocus != null) StopCoroutine(pendingFocus);
        if (overlay != null) Destroy(overlay);
    }
}
