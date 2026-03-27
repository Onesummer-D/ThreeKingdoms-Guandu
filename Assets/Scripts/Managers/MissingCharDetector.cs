using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MissingCharDetector : MonoBehaviour
{
    [Header("要检测的字体")]
    public TMP_FontAsset[] fontsToCheck;

    void Start()
    {
        // 自动找所有SC开头的字体
        if (fontsToCheck == null || fontsToCheck.Length == 0)
        {
            fontsToCheck = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .Where(f => f.name.Contains("SC-"))
                .ToArray();
        }

        // 收集所有场景中TMP_Text的文字
        HashSet<char> allChars = new HashSet<char>();
        var allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var t in allTexts)
        {
            if (!string.IsNullOrEmpty(t.text))
                foreach (char c in t.text) allChars.Add(c);
        }

        Debug.Log($"场景中共有 {allChars.Count} 个不同字符，开始检测...");

        foreach (var font in fontsToCheck)
        {
            if (font == null) continue;
            int missing = allChars.Count(c => !font.HasCharacter(c, true));
            if (missing > 0)
                Debug.LogWarning($"[{font.name}] 缺失 {missing} 个字符");
            else
                Debug.Log($"<color=green>[{font.name}] 完整！</color>");
        }
    }
}