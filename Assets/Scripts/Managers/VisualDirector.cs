using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Converts the current resource state into a restrained, player-readable
/// visual atmosphere. It only observes game state; it never changes plot or
/// resource values, so the existing ScriptableObject flow remains reusable.
/// </summary>
public sealed class VisualDirector : MonoBehaviour
{
    private FinalUIManager uiManager;
    private Image backgroundImage;
    private RectTransform gameplayPanel;
    private Image tintOverlay;
    private Image vignetteOverlay;
    private Image eventFlashOverlay;
    private Texture2D vignetteTexture;
    private Sprite vignetteSprite;
    private Texture2D solidTexture;
    private Sprite solidSprite;
    private Coroutine transitionRoutine;
    private Coroutine pulseRoutine;
    private Coroutine eventFlashRoutine;
    private bool initialized;

    private Color baseTroopColor = Color.white;
    private Color baseFoodColor = Color.white;
    private Color baseStrategyColor = Color.white;
    private Color baseRiskColor = Color.white;

    private struct VisualState
    {
        public Color tint;
        public Color vignette;
        public float pulseStrength;

        public VisualState(Color tint, Color vignette, float pulseStrength)
        {
            this.tint = tint;
            this.vignette = vignette;
            this.pulseStrength = pulseStrength;
        }
    }

    public void Initialize(FinalUIManager manager)
    {
        if (initialized || manager == null) return;

        uiManager = manager;
        backgroundImage = manager.backgroundImage;
        gameplayPanel = manager.gameInterfacePanel != null
            ? manager.gameInterfacePanel.GetComponent<RectTransform>()
            : null;

        if (gameplayPanel == null)
        {
            Debug.LogWarning("VisualDirector: 未找到玩法面板，响应式影像未启用。");
            return;
        }

        CacheBaseTextColors();
        CreateAtmosphereLayers();
        Subscribe();
        initialized = true;
        RefreshImmediate();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (vignetteSprite != null) Destroy(vignetteSprite);
        if (vignetteTexture != null) Destroy(vignetteTexture);
        if (solidSprite != null) Destroy(solidSprite);
        if (solidTexture != null) Destroy(solidTexture);
    }

    private void Subscribe()
    {
        ResourceManager.OnResourceChanged -= HandleResourceChanged;
        ResourceManager.OnResourceChanged += HandleResourceChanged;

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleNodeShown;
            DialogueSystem.Instance.OnDialogueNodeShown += HandleNodeShown;
        }
    }

    private void Unsubscribe()
    {
        ResourceManager.OnResourceChanged -= HandleResourceChanged;
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleNodeShown;
    }

    private void CacheBaseTextColors()
    {
        if (uiManager.troopText != null) baseTroopColor = uiManager.troopText.color;
        if (uiManager.foodText != null) baseFoodColor = uiManager.foodText.color;
        if (uiManager.strategyText != null) baseStrategyColor = uiManager.strategyText.color;
        if (uiManager.riskText != null) baseRiskColor = uiManager.riskText.color;
    }

    private void CreateAtmosphereLayers()
    {
        tintOverlay = CreateOverlayImage("VisualDirector_Tint", 1);
        tintOverlay.sprite = CreateSolidSprite();
        vignetteOverlay = CreateOverlayImage("VisualDirector_Vignette", 2);
        vignetteOverlay.sprite = CreateVignetteSprite();
        vignetteOverlay.color = new Color(0.08f, 0.025f, 0.01f, 0f);
        eventFlashOverlay = CreateOverlayImage("VisualDirector_EventFlash", 3);
        // Reuse the 1x1 sprite; both layers are independent through their
        // Image colors and no extra runtime texture is needed.
        eventFlashOverlay.sprite = tintOverlay.sprite;
        eventFlashOverlay.color = Color.clear;
    }

    private Sprite CreateSolidSprite()
    {
        solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
        {
            name = "VisualDirector_SolidTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        solidTexture.SetPixel(0, 0, Color.white);
        solidTexture.Apply(false, false);
        solidSprite = Sprite.Create(solidTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        solidSprite.name = "VisualDirector_SolidSprite";
        return solidSprite;
    }

    private Image CreateOverlayImage(string objectName, int offsetFromBackground)
    {
        GameObject layer = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layer.transform.SetParent(gameplayPanel, false);

        RectTransform rect = layer.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = layer.GetComponent<Image>();
        image.raycastTarget = false;
        image.type = Image.Type.Simple;

        // In the current scene Background_Current is a sibling of
        // GameInterfacePanel, so a sibling index from that other parent must
        // not be reused here. Keep both layers behind all gameplay UI.
        int siblingIndex = backgroundImage != null && backgroundImage.transform.parent == gameplayPanel
            ? backgroundImage.transform.GetSiblingIndex() + offsetFromBackground
            : offsetFromBackground - 1;
        siblingIndex = Mathf.Clamp(siblingIndex, 0, gameplayPanel.childCount - 1);
        layer.transform.SetSiblingIndex(siblingIndex);
        return image;
    }

    private Sprite CreateVignetteSprite()
    {
        const int size = 96;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "VisualDirector_VignetteTexture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1.05f, distance));
                vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        vignetteTexture.Apply(false, false);
        vignetteSprite = Sprite.Create(vignetteTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        vignetteSprite.name = "VisualDirector_VignetteSprite";
        return vignetteSprite;
    }

    private void HandleResourceChanged(ResourceChangeEvent changeEvent)
    {
        if (!initialized) return;
        ApplyTextState();
        AnimateTo(CalculateState(), true);
        PulseResourceText(changeEvent.resourceType);
        PulseEventFlash(changeEvent);
    }

    private void HandleNodeShown(int nodeId)
    {
        if (!initialized) return;
        AnimateTo(CalculateState(), false);
    }

    private VisualState CalculateState()
    {
        if (ResourceManager.Instance == null)
            return new VisualState(Color.white, new Color(0.08f, 0.025f, 0.01f, 0f), 0f);

        float troop = ResourceManager.Instance.GetTroop();
        float food = ResourceManager.Instance.GetFood();
        float strategy = ResourceManager.Instance.GetStrategy();
        float risk = ResourceManager.Instance.GetRisk();

        float troopCrisis = Mathf.InverseLerp(45f, 8f, troop);
        float foodCrisis = Mathf.InverseLerp(45f, 8f, food);
        float danger = Mathf.InverseLerp(55f, 92f, risk);
        float strategyGlow = Mathf.InverseLerp(60f, 100f, strategy);
        float pressure = Mathf.Clamp01(Mathf.Max(Mathf.Max(troopCrisis, foodCrisis), danger));

        Color coolPressure = new Color(0.16f, 0.28f, 0.38f, 1f);
        Color warmDanger = new Color(0.58f, 0.16f, 0.07f, 1f);
        Color tint = Color.Lerp(Color.white, coolPressure, Mathf.Max(troopCrisis, foodCrisis) * 0.11f);
        tint = Color.Lerp(tint, warmDanger, danger * 0.18f);
        tint = Color.Lerp(tint, new Color(1f, 0.82f, 0.36f, 1f), strategyGlow * 0.035f);
        // The always-on layer stays restrained, but is now strong enough to
        // read as an intentional atmosphere shift on a normal 1080p display.
        tint.a = 0.065f + pressure * 0.22f;

        float vignetteAlpha = Mathf.Clamp01(pressure * 0.56f);
        Color vignette = Color.Lerp(new Color(0.08f, 0.025f, 0.01f, 0f),
            new Color(0.08f, 0.025f, 0.01f, vignetteAlpha), pressure);
        return new VisualState(tint, vignette, Mathf.Clamp01(pressure * 0.90f));
    }

    private void AnimateTo(VisualState target, bool resourceChanged)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(AnimateState(target, resourceChanged ? 0.32f : 0.55f));
    }

    private IEnumerator AnimateState(VisualState target, float duration)
    {
        Color startTint = tintOverlay != null ? tintOverlay.color : Color.clear;
        Color startVignette = vignetteOverlay != null ? vignetteOverlay.color : Color.clear;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            if (tintOverlay != null) tintOverlay.color = Color.Lerp(startTint, target.tint, t);
            if (vignetteOverlay != null) vignetteOverlay.color = Color.Lerp(startVignette, target.vignette, t);
            yield return null;
        }

        if (tintOverlay != null) tintOverlay.color = target.tint;
        if (vignetteOverlay != null) vignetteOverlay.color = target.vignette;
        transitionRoutine = null;
    }

    private void ApplyTextState()
    {
        if (ResourceManager.Instance == null) return;
        ApplyResourceTextColor(uiManager.troopText, baseTroopColor, ResourceManager.Instance.GetTroop(), false);
        ApplyResourceTextColor(uiManager.foodText, baseFoodColor, ResourceManager.Instance.GetFood(), false);
        ApplyResourceTextColor(uiManager.strategyText, baseStrategyColor, ResourceManager.Instance.GetStrategy(), false);
        ApplyResourceTextColor(uiManager.riskText, baseRiskColor, ResourceManager.Instance.GetRisk(), true);
    }

    private void ApplyResourceTextColor(TMP_Text text, Color baseColor, float value, bool inverse)
    {
        if (text == null) return;
        float intensity = inverse
            ? Mathf.InverseLerp(55f, 92f, value)
            : Mathf.InverseLerp(45f, 8f, value);
        Color alert = inverse || intensity > 0.65f
            ? new Color(1f, 0.35f, 0.20f, 1f)
            : new Color(1f, 0.82f, 0.34f, 1f);
        text.color = Color.Lerp(baseColor, alert, Mathf.Clamp01(intensity * 0.95f));
    }

    private void PulseResourceText(ResourceType type)
    {
        TMP_Text text = GetResourceText(type);
        if (text == null) return;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(PulseText(text));
    }

    private TMP_Text GetResourceText(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Troop: return uiManager.troopText;
            case ResourceType.Food: return uiManager.foodText;
            case ResourceType.Strategy: return uiManager.strategyText;
            case ResourceType.Risk: return uiManager.riskText;
            default: return null;
        }
    }

    private IEnumerator PulseText(TMP_Text text)
    {
        Vector3 baseScale = text.rectTransform.localScale;
        float elapsed = 0f;
        const float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.16f;
            text.rectTransform.localScale = baseScale * pulse;
            yield return null;
        }
        text.rectTransform.localScale = baseScale;
        pulseRoutine = null;
    }

    private void PulseEventFlash(ResourceChangeEvent changeEvent)
    {
        if (eventFlashOverlay == null || Mathf.Abs(changeEvent.changeAmount) < 0.01f) return;
        if (eventFlashRoutine != null) StopCoroutine(eventFlashRoutine);
        eventFlashRoutine = StartCoroutine(FlashEvent(changeEvent));
    }

    private IEnumerator FlashEvent(ResourceChangeEvent changeEvent)
    {
        bool harmful = changeEvent.resourceType == ResourceType.Risk
            ? changeEvent.changeAmount > 0f
            : changeEvent.changeAmount < 0f;
        Color flashColor = harmful
            ? new Color(0.95f, 0.10f, 0.045f, 1f)
            : new Color(1.00f, 0.72f, 0.18f, 1f);
        float peakAlpha = harmful ? 0.32f : 0.20f;
        const float duration = 0.68f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // A quick readable hit followed by a soft fade communicates the
            // event without leaving a persistent wash over the artwork.
            float envelope = Mathf.Sin(t * Mathf.PI);
            if (t < 0.18f) envelope = Mathf.SmoothStep(0f, 1f, t / 0.18f);
            if (eventFlashOverlay != null)
                eventFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, envelope * peakAlpha);
            yield return null;
        }

        if (eventFlashOverlay != null) eventFlashOverlay.color = Color.clear;
        eventFlashRoutine = null;
    }

    private void RefreshImmediate()
    {
        ApplyTextState();
        VisualState state = CalculateState();
        if (tintOverlay != null) tintOverlay.color = state.tint;
        if (vignetteOverlay != null) vignetteOverlay.color = state.vignette;
    }

    public void ResetVisualState()
    {
        if (!initialized) return;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        if (eventFlashRoutine != null) StopCoroutine(eventFlashRoutine);
        if (eventFlashOverlay != null) eventFlashOverlay.color = Color.clear;
        RefreshImmediate();
    }
}
