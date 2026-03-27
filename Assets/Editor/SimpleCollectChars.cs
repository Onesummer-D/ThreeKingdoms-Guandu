using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CompleteCollectChars : EditorWindow
{
    [MenuItem("Tools/收集所有必需字符（完整版）")]
    static void Collect()
    {
        var guids = AssetDatabase.FindAssets("t:DialogueDataSO");
        HashSet<char> chars = new HashSet<char>();
        int totalNodes = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<DialogueDataSO>(path);
            if (so == null || so.nodes == null) continue;

            foreach (var node in so.nodes)
            {
                totalNodes++;
                // 收集所有字符，不只是汉字
                if (!string.IsNullOrEmpty(node.dialogueText))
                {
                    foreach (char c in node.dialogueText)
                        chars.Add(c); // 全都要，不限于汉字范围
                }

                if (node.options != null)
                {
                    foreach (var opt in node.options)
                    {
                        if (!string.IsNullOrEmpty(opt.optionText))
                        {
                            foreach (char c in opt.optionText)
                                chars.Add(c);
                        }
                    }
                }
            }
        }

        // ==================== 关键补充 ====================
        // 1. 数字（全角+半角）
        for (char c = '0'; c <= '9'; c++) chars.Add(c);
        "０１２３４５６７８９".ToList().ForEach(c => chars.Add(c));

        // 2. 空格（半角+全角）
        chars.Add(' ');   // \u0020 半角空格（关键！）
        chars.Add('　');  // \u3000 全角空格

        // 3. 英文字母（大小写）
        for (char c = 'A'; c <= 'Z'; c++) chars.Add(c);
        for (char c = 'a'; c <= 'z'; c++) chars.Add(c);

        // 4. 中文标点（全角）
        string cnPunct = "，。！？、：；（）【】《》\"\"''「」『』—～…";
        foreach (char c in cnPunct) chars.Add(c);

        // 5. 英文标点（半角，TMP下划线需要这些）
        string enPunct = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
        foreach (char c in enPunct) chars.Add(c);

        // 6. TMP特殊字符（下划线、连字符等）
        chars.Add('\u005F'); // _
        chars.Add('\u002D'); // -
        chars.Add('\u2014'); // — 破折号
        chars.Add('\u201C'); // " 左弯引号
        chars.Add('\u201D'); // " 右弯引号

        // 7. 常见缺字补救（如果你知道某些字肯定会出现）
        string commonMissing = "源筑曹袁绍操攸兵粮草策略风险度";
        foreach (char c in commonMissing) chars.Add(c);
        // =================================================

        // 生成结果
        var sorted = chars.OrderBy(c => c).ToArray();
        string result = new string(sorted);

        // 同时生成一个紧凑版（无空格换行，适合粘贴）
        string compact = result.Replace(" ", "").Replace("\n", "").Replace("\r", "");

        // 保存到剪贴板
        EditorGUIUtility.systemCopyBuffer = compact;

        Debug.Log($"✅ 已扫描 {totalNodes} 个节点");
        Debug.Log($"📊 统计：{chars.Count} 个独特字符");
        Debug.Log($"🔤 包含数字：{Enumerable.Range(0, 10).All(i => chars.Contains((char)('0' + i)))}");
        Debug.Log($"📝 前100字符预览：{new string(sorted.Take(100).ToArray())}");
        Debug.Log($"💾 已复制到剪贴板（长度：{compact.Length}）");

        EditorUtility.DisplayDialog("字符收集完成",
            $"共收集 {chars.Count} 个独特字符\n" +
            $"✓ 数字0-9\n" +
            $"✓ 空格（半角+全角）\n" +
            $"✓ 英文大小写\n" +
            $"✓ 中文标点\n" +
            $"✓ 英文标点（含TMP下划线字符）\n\n" +
            "已自动复制到剪贴板，去Font Asset Creator粘贴生成吧！", "确定");
    }
}