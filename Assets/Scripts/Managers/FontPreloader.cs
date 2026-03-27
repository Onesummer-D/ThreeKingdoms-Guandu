using UnityEngine;
using TMPro;
using System.Linq;

public class FontPreloader : MonoBehaviour
{
    [TextArea(3, 5)]
    public string preloadText = "曹操袁绍许攸官渡之战建安五年兵力粮草策略风险度0123456789";

    void Start()
    {
        // 找到SC-Regular（Dynamic字体）
        var font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(f => f.name == "SC-Regular SDF");

        if (font != null)
        {
            // 使用正确的API：TryAddCharacters（复数形式，带s）
            font.TryAddCharacters(preloadText);
            Debug.Log("字体预加载完成");
        }
        else
        {
            Debug.LogWarning("找不到SC-Regular SDF字体");
        }
    }
}