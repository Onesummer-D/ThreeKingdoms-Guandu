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
        // 自动获取Image
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        // 设置初始状态
        buttonImage.sprite = normalSprite;
        if (buttonText != null)
            buttonText.color = normalColor;
    }

    // 鼠标进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonImage.sprite = highlightedSprite;
        if (buttonText != null)
            buttonText.color = highlightedColor;
    }

    // 鼠标离开
    public void OnPointerExit(PointerEventData eventData)
    {
        buttonImage.sprite = normalSprite;
        if (buttonText != null)
            buttonText.color = normalColor;
    }
}