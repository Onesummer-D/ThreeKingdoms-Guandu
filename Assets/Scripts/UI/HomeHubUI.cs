using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeHubUI : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(.012f, .02f, .03f, .94f);
    private static readonly Color PanelColor = new Color(.075f, .11f, .15f, .99f);
    private static readonly Color CardColor = new Color(.11f, .16f, .20f, .98f);
    private static readonly Color TextPrimary = new Color(.94f, .94f, .88f, 1f);
    private static readonly Color TextMuted = new Color(.70f, .77f, .79f, 1f);
    private static readonly Color AccentColor = new Color(.89f, .71f, .32f, 1f);
    private static readonly Color ButtonColor = new Color(.44f, .32f, .22f, 1f);
    // The home-page start action stays brown while its label is forced white
    // by NormalizeMainMenuButton and the dedicated runtime guard below.
    private static readonly Color StartButtonColor = new Color(.62f, .46f, .30f, 1f);

    private GameObject aboutHomeButton;
    private GameObject settingsHomeButton;
    private GameObject muteHomeButton;
    private GameObject aboutOverlay;
    private GameObject settingsOverlay;
    private GameObject aboutLanding;
    private GameObject aboutDetail;
    private TMP_Text aboutDetailTitle;
    private TMP_Text aboutDetailBody;
    private GameObject peopleDetail;
    private Image brightnessOverlay;
    private Sprite buttonSprite;
    private TMP_Text qualityButtonLabel;
    private RuntimeIconGraphic muteIcon;
    private FinalUIManager uiManager;
    private Button startButton;
    private Sprite caocaoPortrait;
    private Sprite xuyouPortrait;
    private Sprite yuanshaoPortrait;

    public void Initialize(FinalUIManager manager, GameObject startMenuPanel, Transform uiRoot,
        Sprite caocao, Sprite xuyou, Sprite yuanshao)
    {
        if (aboutOverlay != null || startMenuPanel == null || uiRoot == null) return;
        uiManager = manager;
        caocaoPortrait = caocao;
        xuyouPortrait = xuyou;
        yuanshaoPortrait = yuanshao;

        startButton = FindStartButton(startMenuPanel.transform);
        if (startButton == null) return;
        Image sourceImage = startButton.GetComponent<Image>();
        if (sourceImage != null) buttonSprite = sourceImage.sprite;

        ConfigureMainMenuButton(startButton.gameObject, new Vector2(0f, -320f));
        NormalizeMainMenuButton(startButton.gameObject, StartButtonColor);
        GameObject saveButton = FindDirectChild(startMenuPanel.transform, "查看存档Button");
        if (saveButton != null)
        {
            ConfigureMainMenuButton(saveButton, new Vector2(-330f, -320f));
            NormalizeMainMenuButton(saveButton, Color.white);
        }
        aboutHomeButton = CreateMenuButton(startMenuPanel.transform, "关于游戏",
            new Vector2(330f, -320f), OpenAbout);
        settingsHomeButton = CreateIconHomeButton(startMenuPanel.transform, "SettingsIconButton",
            RuntimeIconGraphic.IconKind.Gear, new Vector2(-34f, -30f), OpenSettings, out _);
        muteHomeButton = CreateIconHomeButton(startMenuPanel.transform, "GlobalMuteButton",
            RuntimeIconGraphic.IconKind.Speaker, new Vector2(-124f, -30f), ToggleGlobalMute, out muteIcon);
        AudioListener.volume = PlayerPrefs.GetInt("GlobalMuted", 0) == 1 ? 0f : 1f;
        RefreshMuteIcon();

        CreateBrightnessOverlay(uiRoot);
        CreateAboutOverlay(uiRoot);
        CreateSettingsOverlay(uiRoot);
        ApplySavedDisplaySettings();
    }

    private void LateUpdate()
    {
        // ButtonSpriteSwap is a legacy scene component and can run after this
        // initializer. Re-assert the requested white label at the end of the
        // frame so pointer enter/exit and script execution order cannot turn
        // the start action black again.
        if (startButton == null) return;
        ButtonSpriteSwap spriteSwap = startButton.GetComponent<ButtonSpriteSwap>();
        if (spriteSwap != null)
        {
            spriteSwap.normalColor = Color.white;
            spriteSwap.highlightedColor = Color.white;
            if (spriteSwap.buttonText != null) spriteSwap.buttonText.color = Color.white;
        }
        TMP_Text[] labels = startButton.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null) labels[i].color = Color.white;
        }
    }

    private void OnDestroy()
    {
        if (aboutHomeButton != null) Destroy(aboutHomeButton);
        if (settingsHomeButton != null) Destroy(settingsHomeButton);
        if (muteHomeButton != null) Destroy(muteHomeButton);
        if (aboutOverlay != null) Destroy(aboutOverlay);
        if (settingsOverlay != null) Destroy(settingsOverlay);
        if (brightnessOverlay != null) Destroy(brightnessOverlay.gameObject);
    }

    private void CreateBrightnessOverlay(Transform root)
    {
        GameObject shade = CreateUIObject("DisplayBrightnessOverlay", root);
        Stretch(shade.GetComponent<RectTransform>());
        brightnessOverlay = shade.AddComponent<Image>();
        brightnessOverlay.color = Color.clear;
        brightnessOverlay.raycastTarget = false;
        shade.transform.SetAsLastSibling();
    }

    private void CreateAboutOverlay(Transform root)
    {
        aboutOverlay = CreateModalRoot("AboutGameOverlay", root);
        GameObject panel = CreatePanel(aboutOverlay.transform, "AboutGamePanel");
        CreateHeader(panel.transform, "关于游戏", () => aboutOverlay.SetActive(false));

        aboutLanding = CreateUIObject("AboutLanding", panel.transform);
        SetAnchored(aboutLanding.GetComponent<RectTransform>(), new Vector2(.06f, .11f), new Vector2(.94f, .82f));

        TMP_Text lead = CreateText(aboutLanding.transform,
            "你将从曹操视角调度兵力、粮草、计策与风险，走向七种结局。",
            42f, TextAlignmentOptions.TopLeft, TextPrimary, true);
        SetAnchored(lead.rectTransform, new Vector2(0f, .78f), new Vector2(1f, 1f));

        Button people = CreateCardButton(aboutLanding.transform, "认识人物",
            "认识曹操、许攸与袁绍",
            new Vector2(0f, .13f), new Vector2(.47f, .66f));
        people.onClick.AddListener(ShowPeople);

        Button rules = CreateCardButton(aboutLanding.transform, "玩法介绍",
            "时长、资源、分支结局、彩蛋与军议邀约",
            new Vector2(.53f, .13f), new Vector2(1f, .66f));
        rules.onClick.AddListener(ShowRules);

        aboutDetail = CreateUIObject("AboutDetail", panel.transform);
        SetAnchored(aboutDetail.GetComponent<RectTransform>(), new Vector2(.06f, .11f), new Vector2(.94f, .82f));
        aboutDetailTitle = CreateText(aboutDetail.transform, "", 52f,
            TextAlignmentOptions.TopLeft, AccentColor, false);
        SetAnchored(aboutDetailTitle.rectTransform, new Vector2(0f, .82f), new Vector2(1f, 1f));
        aboutDetailBody = CreateText(aboutDetail.transform, "", 36f,
            TextAlignmentOptions.TopLeft, TextPrimary, true);
        SetAnchored(aboutDetailBody.rectTransform, new Vector2(.02f, .08f), new Vector2(.98f, .84f));
        aboutDetailBody.overflowMode = TextOverflowModes.Overflow;
        aboutDetailBody.enableAutoSizing = true;
        aboutDetailBody.fontSizeMin = 28f;
        aboutDetailBody.fontSizeMax = 36f;
        Button back = CreateButton(aboutDetail.transform, "返回", 30f, new Vector2(150f, 60f));
        SetBottomRight(back.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(150f, 60f));
        back.onClick.AddListener(ShowAboutLanding);
        CreatePeopleDetail(aboutDetail.transform);
        aboutDetail.SetActive(false);
        aboutOverlay.SetActive(false);
    }

    private void CreateSettingsOverlay(Transform root)
    {
        settingsOverlay = CreateModalRoot("SettingsOverlay", root);
        GameObject panel = CreatePanel(settingsOverlay.transform, "SettingsPanel");
        CreateHeader(panel.transform, "设置", CloseSettings);

        float bgm = AudioManager.Instance != null
            ? AudioManager.Instance.GetBGMVolume()
            : PlayerPrefs.GetFloat("BGMVolume", 1f);
        float effects = AudioManager.Instance != null
            ? AudioManager.Instance.GetEffectsVolume()
            : PlayerPrefs.GetFloat("EffectsVolume", .6f);
        float brightness = PlayerPrefs.GetFloat("DisplayBrightness", 1f);

        CreateSliderRow(panel.transform, "背景音乐", .67f, 0f, 1f, bgm, value =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(value);
            else PlayerPrefs.SetFloat("BGMVolume", value);
        });
        CreateSliderRow(panel.transform, "音效音量", .52f, 0f, 1f, effects, value =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetEffectsVolume(value);
            else PlayerPrefs.SetFloat("EffectsVolume", value);
        });
        CreateSliderRow(panel.transform, "画面亮度", .37f, .45f, 1f, brightness, value =>
        {
            PlayerPrefs.SetFloat("DisplayBrightness", value);
            ApplyBrightness(value);
        });

        Button quality = CreateButton(panel.transform, "", 44f, new Vector2(320f, 66f));
        SetAnchored(quality.GetComponent<RectTransform>(), new Vector2(.55f, .20f), new Vector2(.88f, .29f));
        qualityButtonLabel = quality.GetComponentInChildren<TMP_Text>(true);
        if (qualityButtonLabel != null)
        {
            qualityButtonLabel.fontSize = 44f;
            qualityButtonLabel.enableAutoSizing = false;
            qualityButtonLabel.overflowMode = TextOverflowModes.Overflow;
            qualityButtonLabel.alignment = TextAlignmentOptions.Center;
        }
        RefreshQualityLabel();
        quality.onClick.AddListener(CycleQuality);
        settingsOverlay.SetActive(false);
    }

    private void ApplySavedDisplaySettings()
    {
        // Fullscreen is deliberately always on; the old checkbox was too
        // easy to clip in the compact settings panel and offered no useful
        // alternate presentation for this game.
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
        PlayerPrefs.SetInt("Fullscreen", 1);
        ApplyQualityPreset(PlayerPrefs.GetInt("DisplayQualityPreset", 1));
        ApplyBrightness(PlayerPrefs.GetFloat("DisplayBrightness", 1f));
    }

    private void OpenAbout()
    {
        ShowAboutLanding();
        aboutOverlay.SetActive(true);
        aboutOverlay.transform.SetAsLastSibling();
    }

    private void ShowAboutLanding()
    {
        aboutLanding.SetActive(true);
        aboutDetail.SetActive(false);
    }

    private void ShowPeople()
    {
        aboutDetailTitle.text = "认识人物";
        aboutLanding.SetActive(false);
        aboutDetail.SetActive(true);
        aboutDetailBody.gameObject.SetActive(false);
        if (peopleDetail != null) peopleDetail.SetActive(true);
    }

    private void ShowRules()
    {
        aboutDetailTitle.text = "玩法介绍";
        aboutDetailBody.text =
            "单局约 10—15 分钟，共有 7 条结局线，并藏有等待发现的彩蛋。\n\n" +
            "每次决策都会改变兵力、粮草、计策与风险。数值和已经走过的选择共同决定后续路线。\n\n" +
            "剧情进行中可随时查看史官注；决策点可开启“军议邀约”，把当前战况交给朋友参谋。\n\n" +
            "一局结束，剧情回顾和通关复盘为你保存独属于你的历史记录。";
        aboutLanding.SetActive(false);
        aboutDetail.SetActive(true);
        if (peopleDetail != null) peopleDetail.SetActive(false);
        aboutDetailBody.gameObject.SetActive(true);
    }

    private void OpenSettings()
    {
        RefreshQualityLabel();
        settingsOverlay.SetActive(true);
        settingsOverlay.transform.SetAsLastSibling();
    }

    private void CloseSettings()
    {
        PlayerPrefs.Save();
        settingsOverlay.SetActive(false);
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessOverlay == null) return;
        float darkness = Mathf.Clamp01((1f - value) / .55f) * .62f;
        brightnessOverlay.color = new Color(0f, 0f, 0f, darkness);
        brightnessOverlay.transform.SetAsLastSibling();
        if (aboutOverlay != null && aboutOverlay.activeSelf) aboutOverlay.transform.SetAsLastSibling();
        if (settingsOverlay != null && settingsOverlay.activeSelf) settingsOverlay.transform.SetAsLastSibling();
    }

    private void CycleQuality()
    {
        int next = (PlayerPrefs.GetInt("DisplayQualityPreset", 1) + 1) % 3;
        ApplyQualityPreset(next);
        RefreshQualityLabel();
    }

    private void RefreshQualityLabel()
    {
        if (qualityButtonLabel == null) return;
        int index = Mathf.Clamp(PlayerPrefs.GetInt("DisplayQualityPreset", 1), 0, 2);
        string[] names = { "省电", "标准", "超清" };
        qualityButtonLabel.text = "画面模式：" + names[index];
    }

    private static void ApplyQualityPreset(int preset)
    {
        preset = Mathf.Clamp(preset, 0, 2);
        PlayerPrefs.SetInt("DisplayQualityPreset", preset);
        Application.targetFrameRate = preset == 0 ? 30 : preset == 1 ? 60 : 120;
        QualitySettings.antiAliasing = preset == 0 ? 0 : preset == 1 ? 2 : 8;
        QualitySettings.globalTextureMipmapLimit = preset == 0 ? 1 : 0;
    }

    private void ToggleGlobalMute()
    {
        bool muted = AudioListener.volume > .001f;
        AudioListener.volume = muted ? 0f : 1f;
        PlayerPrefs.SetInt("GlobalMuted", muted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshMuteIcon();
    }

    private void RefreshMuteIcon()
    {
        if (muteIcon == null) return;
        bool muted = AudioListener.volume <= .001f;
        muteIcon.Slashed = muted;
        muteIcon.color = Color.clear;
        Transform fallbackTransform = muteIcon.transform.parent.Find("FallbackIcon");
        Image fallback = fallbackTransform != null ? fallbackTransform.GetComponent<Image>() : null;
        if (fallback != null)
        {
            Color iconColor = muted ? new Color(.82f, .30f, .22f, 1f) : new Color(.89f, .71f, .32f, 1f);
            fallback.sprite = RuntimeIconGraphic.CreateSprite(RuntimeIconGraphic.IconKind.Speaker,
                iconColor, muted);
            fallback.color = Color.white;
        }
    }

    private void CreatePeopleDetail(Transform parent)
    {
        peopleDetail = CreateUIObject("PeoplePortraits", parent);
        SetAnchored(peopleDetail.GetComponent<RectTransform>(), new Vector2(0f, .10f), new Vector2(1f, .80f));
        CreatePersonColumn(peopleDetail.transform, caocaoPortrait, "曹操", "果决 · 务实", 0);
        CreatePersonColumn(peopleDetail.transform, xuyouPortrait, "许攸", "敏锐 · 好功", 1);
        CreatePersonColumn(peopleDetail.transform, yuanshaoPortrait, "袁绍", "威望 · 多疑", 2);
        TMP_Text note = CreateText(peopleDetail.transform,
            "当前版本开放曹操路线；许攸与袁绍作为关键人物参与剧情",
            34f, TextAlignmentOptions.Center, TextMuted, true);
        SetAnchored(note.rectTransform, new Vector2(.04f, 0f), new Vector2(.96f, .12f));
        peopleDetail.SetActive(false);
    }

    private void CreatePersonColumn(Transform parent, Sprite portrait, string displayName,
        string keywords, int index)
    {
        float minX = index / 3f + .025f;
        float maxX = (index + 1) / 3f - .025f;
        GameObject column = CreateUIObject(displayName + "Column", parent);
        SetAnchored(column.GetComponent<RectTransform>(), new Vector2(minX, .14f), new Vector2(maxX, 1f));

        GameObject portraitObject = CreateUIObject(displayName + "Portrait", column.transform);
        SetAnchored(portraitObject.GetComponent<RectTransform>(), new Vector2(.12f, .37f), new Vector2(.88f, .98f));
        Image image = portraitObject.AddComponent<Image>();
        image.sprite = portrait;
        image.preserveAspect = true;
        image.color = Color.white;

        TMP_Text name = CreateText(column.transform, displayName, 48f,
            TextAlignmentOptions.Center, AccentColor, false);
        SetAnchored(name.rectTransform, new Vector2(0f, .19f), new Vector2(1f, .37f));
        TMP_Text tags = CreateText(column.transform, keywords, 35f,
            TextAlignmentOptions.Center, TextPrimary, false);
        SetAnchored(tags.rectTransform, new Vector2(0f, 0f), new Vector2(1f, .19f));
    }

    private GameObject CreateMenuButton(Transform parent, string label, Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        Button button = CreateButton(parent, label, 42f, new Vector2(300f, 90f));
        ConfigureMainMenuButton(button.gameObject, position);
        NormalizeMainMenuButton(button.gameObject, Color.white);
        button.onClick.AddListener(action);
        return button.gameObject;
    }

    private static void NormalizeMainMenuButton(GameObject buttonObject, Color buttonColor)
    {
        if (buttonObject == null) return;
        Image image = buttonObject.GetComponent<Image>();
        if (image != null) image.color = buttonColor;
        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = Color.Lerp(buttonColor, Color.white, .08f);
            colors.pressedColor = Color.Lerp(buttonColor, Color.black, .12f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, .55f);
            button.colors = colors;
        }
        TMP_Text[] texts = buttonObject.GetComponentsInChildren<TMP_Text>(true);
        if (texts == null || texts.Length == 0) return;
        bool isStartButton = buttonObject.name == "StartButton" ||
            buttonObject.name.IndexOf("开始游戏", StringComparison.Ordinal) >= 0;
        Color labelColor = isStartButton ? Color.white : new Color(.16f, .13f, .11f, 1f);
        // The legacy ButtonSpriteSwap component also writes the label color
        // during Start and on pointer exit. Keep its serialized/runtime
        // colors in sync so the start label cannot revert to black after the
        // menu has been initialized or hovered.
        ButtonSpriteSwap spriteSwap = buttonObject.GetComponent<ButtonSpriteSwap>();
        if (spriteSwap != null && isStartButton)
        {
            spriteSwap.normalColor = Color.white;
            spriteSwap.highlightedColor = Color.white;
            if (spriteSwap.buttonText != null) spriteSwap.buttonText.color = Color.white;
        }
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null) continue;
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = labelColor;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static void ConfigureMainMenuButton(GameObject buttonObject, Vector2 position)
    {
        if (buttonObject == null) return;
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 90f);
    }

    private GameObject CreateIconHomeButton(Transform parent, string name, RuntimeIconGraphic.IconKind kind,
        Vector2 position, UnityEngine.Events.UnityAction action, out RuntimeIconGraphic icon)
    {
        GameObject obj = CreateUIObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(76f, 76f);
        Image background = obj.AddComponent<Image>();
        background.color = Color.clear;
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);
        GameObject iconObject = CreateUIObject("Icon", obj.transform);
        SetAnchored(iconObject.GetComponent<RectTransform>(), new Vector2(.19f, .19f), new Vector2(.81f, .81f));
        icon = iconObject.AddComponent<RuntimeIconGraphic>();
        icon.Kind = kind;
        icon.color = Color.clear;
        icon.raycastTarget = false;
        GameObject fallbackObject = CreateUIObject("FallbackIcon", obj.transform);
        SetAnchored(fallbackObject.GetComponent<RectTransform>(), new Vector2(.19f, .19f), new Vector2(.81f, .81f));
        Image fallback = fallbackObject.AddComponent<Image>();
        fallback.sprite = RuntimeIconGraphic.CreateSprite(kind, new Color(.89f, .71f, .32f, 1f));
        fallback.preserveAspect = true;
        fallback.raycastTarget = false;
        button.targetGraphic = fallback;
        return obj;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject;
        return null;
    }

    private void CreateSliderRow(Transform parent, string label, float anchorY,
        float min, float max, float value, UnityEngine.Events.UnityAction<float> changed)
    {
        TMP_Text name = CreateText(parent, label, 40f, TextAlignmentOptions.Left, TextPrimary, false);
        SetAnchored(name.rectTransform, new Vector2(.12f, anchorY), new Vector2(.34f, anchorY + .10f));

        Slider slider = CreateSlider(parent, min, max, value);
        SetAnchored(slider.GetComponent<RectTransform>(), new Vector2(.36f, anchorY + .015f), new Vector2(.78f, anchorY + .085f));

        TMP_Text amount = CreateText(parent, Mathf.RoundToInt(value * 100f) + "%", 44f,
            TextAlignmentOptions.Center, TextMuted, false);
        amount.overflowMode = TextOverflowModes.Overflow;
        SetAnchored(amount.rectTransform, new Vector2(.80f, anchorY), new Vector2(.96f, anchorY + .10f));
        slider.onValueChanged.AddListener(current => amount.text = Mathf.RoundToInt(current * 100f) + "%");
        slider.onValueChanged.AddListener(changed);
    }

    private Slider CreateSlider(Transform parent, float min, float max, float value)
    {
        GameObject root = CreateUIObject("Slider", parent);
        Slider slider = root.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        GameObject background = CreateUIObject("Background", root.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        SetAnchored(backgroundRect, new Vector2(0f, .36f), new Vector2(1f, .64f));
        background.AddComponent<Image>().color = new Color(.20f, .27f, .29f, 1f);

        GameObject fillArea = CreateUIObject("Fill Area", root.transform);
        SetAnchored(fillArea.GetComponent<RectTransform>(), new Vector2(.02f, .36f), new Vector2(.98f, .64f));
        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        Stretch(fill.GetComponent<RectTransform>());
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = AccentColor;

        GameObject handleArea = CreateUIObject("Handle Slide Area", root.transform);
        SetAnchored(handleArea.GetComponent<RectTransform>(), new Vector2(.02f, 0f), new Vector2(.98f, 1f));
        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = handleRect.anchorMax = new Vector2(.5f, .5f);
        handleRect.pivot = new Vector2(.5f, .5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(18f, 18f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = TextPrimary;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private Button CreateCardButton(Transform parent, string title, string body, Vector2 min, Vector2 max)
    {
        GameObject card = CreateUIObject(title + "Card", parent);
        SetAnchored(card.GetComponent<RectTransform>(), min, max);
        Image image = card.AddComponent<Image>();
        // Keep the information cards visually distinct from the paper-shaped
        // menu buttons. A solid panel is more legible at every aspect ratio
        // and avoids stretching the old parchment sprite into a rectangle.
        image.type = Image.Type.Simple;
        image.color = new Color(.075f, .12f, .15f, .99f);
        Shadow shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .45f);
        shadow.effectDistance = new Vector2(7f, -7f);
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = card.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject divider = CreateUIObject("Divider", card.transform);
        SetAnchored(divider.GetComponent<RectTransform>(), new Vector2(.12f, .51f), new Vector2(.88f, .53f));
        divider.AddComponent<Image>().color = new Color(.89f, .71f, .32f, .70f);

        TMP_Text titleText = CreateText(card.transform, title, 52f, TextAlignmentOptions.Center, AccentColor, false);
        titleText.fontStyle = FontStyles.Bold;
        SetAnchored(titleText.rectTransform, new Vector2(.06f, .55f), new Vector2(.94f, .88f));
        TMP_Text bodyText = CreateText(card.transform, body, 36f, TextAlignmentOptions.Center, TextPrimary, true);
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 26f;
        bodyText.fontSizeMax = 36f;
        SetAnchored(bodyText.rectTransform, new Vector2(.08f, .12f), new Vector2(.92f, .50f));
        return button;
    }

    private GameObject CreateModalRoot(string name, Transform root)
    {
        GameObject modal = CreateUIObject(name, root);
        Stretch(modal.GetComponent<RectTransform>());
        modal.AddComponent<Image>().color = OverlayColor;
        return modal;
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = CreateUIObject(name, parent);
        SetAnchored(panel.GetComponent<RectTransform>(), new Vector2(.12f, .08f), new Vector2(.88f, .92f));
        panel.AddComponent<Image>().color = PanelColor;
        return panel;
    }

    private void CreateHeader(Transform panel, string title, UnityEngine.Events.UnityAction closeAction)
    {
        TMP_Text heading = CreateText(panel, title, 52f, TextAlignmentOptions.Left, TextPrimary, false);
        SetAnchored(heading.rectTransform, new Vector2(.06f, .84f), new Vector2(.70f, .97f));
        Button close = CreateButton(panel, "关闭", 34f, new Vector2(170f, 66f));
        SetTopRight(close.GetComponent<RectTransform>(), new Vector2(-28f, -24f), new Vector2(170f, 66f));
        close.onClick.AddListener(closeAction);
    }

    private GameObject CloneMenuButton(Button source, Transform parent, string label,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject clone = Instantiate(source.gameObject, parent);
        clone.name = label + "Button";
        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Button button = clone.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        return clone;
    }

    private Button CreateButton(Transform parent, string label, float fontSize, Vector2 size)
    {
        GameObject buttonObject = CreateUIObject(label + "Button", parent);
        buttonObject.GetComponent<RectTransform>().sizeDelta = size;
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = ButtonColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text text = CreateText(buttonObject.transform, label, fontSize,
            TextAlignmentOptions.Center, TextPrimary, false);
        Stretch(text.rectTransform);
        return button;
    }

    private static Button FindStartButton(Transform root)
    {
        Transform exact = root.Find("StartButton");
        if (exact != null) return exact.GetComponent<Button>();
        return root.GetComponentInChildren<Button>(true);
    }

    private static TMP_Text CreateText(Transform parent, string value, float size,
        TextAlignmentOptions alignment, Color color, bool wrap)
    {
        GameObject textObject = CreateUIObject("Text", parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SC-Regular SDF");
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopRight(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }

    private static void SetBottomRight(RectTransform rect, Vector2 offset, Vector2 size)
    {
        rect.anchorMin = Vector2.right;
        rect.anchorMax = Vector2.right;
        rect.pivot = Vector2.right;
        rect.anchoredPosition = offset;
        rect.sizeDelta = size;
    }
}
