using UnityEngine;
using TMPro;

public class ClickHintAnimator : MonoBehaviour
{
    [Header("闪烁设置")]
    public float fadeSpeed = 1.5f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;

    private TMP_Text hintText;
    private bool isAnimating = false;

    void Start()
    {
        // 先在当前物体找
        hintText = GetComponent<TMP_Text>();

        // 如果当前物体没有，在子物体里找
        if (hintText == null)
        {
            hintText = GetComponentInChildren<TMP_Text>();
        }

        // 如果还是没有，报错
        if (hintText == null)
        {
            Debug.LogError("找不到 TMP_Text 组件！请确保ClickHint或其子物体有TMP_Text组件");
        }
        else
        {
            Debug.Log($"找到TMP_Text: {hintText.gameObject.name}");
        }
    }

    void Update()
    {
        if (isAnimating && hintText != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha,
                (Mathf.Sin(Time.time * fadeSpeed) + 1f) / 2f);

            Color color = hintText.color;
            color.a = alpha;
            hintText.color = color;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        isAnimating = true;
    }

    public void Hide()
    {
        isAnimating = false;
        gameObject.SetActive(false);
    }
}