using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextNodeId;

    // 资源效果
    public float troopEffect = 0;
    public float foodEffect = 0;
    public float strategyEffect = 0;
    public float riskEffect = 0;

    // 小游戏类型
    public string miniGameType = ""; // "puzzle", "slider", "grid", "digtunnel"

    // ========== 新增：条件字段 ==========
    public string condition = ""; // 条件表达式，如 "troop>5"
    // ==================================
}

[System.Serializable]
public class DialogueNode
{
    public int nodeId;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite backgroundImage;
    public bool isBackgroundIntro = false;

    // 人物显示
    public string avatarCharacter = "";     // 头像
    public string leftCharacter = "";       // 左侧立绘
    public string rightCharacter = "";      // 右侧立绘

    public List<DialogueOption> options = new List<DialogueOption>();
    public int nextNodeId;  // 无选项时的下一个节点
}

[CreateAssetMenu(fileName = "DialogueDataSO", menuName = "Game/Dialogue Data")]
public class DialogueDataSO : ScriptableObject
{
    public List<DialogueNode> nodes = new List<DialogueNode>();

    public DialogueNode GetNode(int nodeId)
    {
        return nodes.Find(n => n.nodeId == nodeId);
    }
}