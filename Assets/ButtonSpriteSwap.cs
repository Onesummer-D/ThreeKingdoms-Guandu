using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSpriteSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("图片引用")]
    public Image buttonImage;
    public Sprite normalSprite;      // 未选中
    public Sprite highlightedSprite; // 选中

    [Header("文字颜色（可选）")]
    public TMP_Text buttonText;
    public Color normalColor = Color.black;
    public Color highlightedColor = Color.white;

    void Start()
    {
        ResetToNormal();
    }

    // Choice buttons are reused across many dialogue nodes. Unity does not
    // necessarily dispatch PointerExit when the old node hides a hovered
    // button, so an old grey highlight could persist into the next node.
    void OnEnable()
    {
        ResetToNormal();
    }

    void OnDisable()
    {
        ResetToNormal();
    }

    public void ResetToNormal()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
            buttonImage.color = Color.white;
        }
        if (buttonText != null)
            buttonText.color = normalColor;
    }

    // 鼠标进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        Button button = GetComponent<Button>();
        if (button != null && !button.IsInteractable()) return;
        if (buttonImage != null && highlightedSprite != null)
            buttonImage.sprite = highlightedSprite;
        if (buttonText != null)
            buttonText.color = highlightedColor;
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetToNormal();
    }
}