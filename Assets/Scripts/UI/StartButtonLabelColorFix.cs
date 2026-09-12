using TMPro;
using UnityEngine;

/// <summary>
/// Keeps the serialized StartButton label white even when the legacy
/// ButtonSpriteSwap component applies its own pointer-state colors.
/// This is intentionally attached to the button itself so it does not depend
/// on HomeHubUI initialization order.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class StartButtonLabelColorFix : MonoBehaviour
{
    private TMP_Text[] labels;

    private void Awake()
    {
        labels = GetComponentsInChildren<TMP_Text>(true);
        ApplyWhite();
    }

    private void OnEnable()
    {
        ApplyWhite();
    }

    private void LateUpdate()
    {
        ApplyWhite();
    }

    private void ApplyWhite()
    {
        if (labels == null) labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null) labels[i].color = Color.white;
        }

        ButtonSpriteSwap spriteSwap = GetComponent<ButtonSpriteSwap>();
        if (spriteSwap != null)
        {
            spriteSwap.normalColor = Color.white;
            spriteSwap.highlightedColor = Color.white;
            if (spriteSwap.buttonText != null) spriteSwap.buttonText.color = Color.white;
        }
    }
}
