using UnityEngine;
using UnityEditor;
using TMPro;

public class AssignFourFonts : EditorWindow
{
    public TMP_FontAsset regular;   // 正文
    public TMP_FontAsset medium;    // 按钮
    public TMP_FontAsset bold;      // 数值
    public TMP_FontAsset black;     // 称号

    [MenuItem("Tools/一键赋值四种宋体")]
    static void Open() => GetWindow<AssignFourFonts>("赋值四种宋体");

    void OnGUI()
    {
        EditorGUILayout.LabelField("把四种宋体赋值给对应UI元素", EditorStyles.boldLabel);
        regular = (TMP_FontAsset)EditorGUILayout.ObjectField("Regular(正文)", regular, typeof(TMP_FontAsset), false);
        medium = (TMP_FontAsset)EditorGUILayout.ObjectField("Medium(按钮)", medium, typeof(TMP_FontAsset), false);
        bold = (TMP_FontAsset)EditorGUILayout.ObjectField("Bold(数值)", bold, typeof(TMP_FontAsset), false);
        black = (TMP_FontAsset)EditorGUILayout.ObjectField("Black(称号)", black, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("🔥 开始赋值", GUILayout.Height(40)))
        {
            AssignAll();
        }
    }

    void AssignAll()
    {
        int r = 0, m = 0, b = 0, bl = 0;
        var texts = FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (var text in texts)
        {
            Undo.RecordObject(text, "Assign Font");
            string name = text.gameObject.name.ToLower();
            var parent = text.transform.parent;
            string pName = parent ? parent.name.ToLower() : "";

            // 按钮/选项 -> Medium
            if (name.Contains("button") || name.Contains("option") || name.Contains("btn") ||
                name.Contains("选项") || pName.Contains("button"))
            {
                if (medium) { text.font = medium; m++; }
            }
            // 数值/变化 -> Bold  
            else if (name.Contains("value") || name.Contains("num") || name.Contains("change") ||
                     name.Contains("兵力") || name.Contains("粮草") || name.Contains("troop") || name.Contains("food"))
            {
                if (bold) { text.font = bold; b++; }
            }
            // 称号/标题 -> Black
            else if (name.Contains("title") || name.Contains("badge") || name.Contains("achievement") ||
                     name.Contains("称号") || name.Contains("徽章"))
            {
                if (black) { text.font = black; bl++; }
            }
            // 其他全部 -> Regular（包括DialogueText）
            else
            {
                if (regular) { text.font = regular; r++; }
            }

            // 字号+4（解决宋体视觉偏小）
            if (text.fontSize > 0 && text.fontSize < 30)
            {
                text.fontSize += 4;
            }

            EditorUtility.SetDirty(text);
        }

        EditorUtility.DisplayDialog("完成",
            $"Regular: {r}\nMedium: {m}\nBold: {b}\nBlack: {bl}\n总计: {r + m + b + bl}", "确定");
    }
}