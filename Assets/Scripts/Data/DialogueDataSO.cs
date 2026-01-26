using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueOptionData
{
    [TextArea(1, 2)] public string optionText = "选项文本";
    public int nextNodeId = 1001;
    [Range(-5, 5)] public float troopEffect = 0;
    [Range(-5, 5)] public float foodEffect = 0;
    [Range(-5, 5)] public float strategyEffect = 0;
    [Range(-5, 5)] public float riskEffect = 0;
    public string miniGameType = "";
    public string condition = "";
}

[System.Serializable]
public class DialogueNodeData
{
    public int nodeId = 1001;
    [TextArea(3, 5)] public string dialogueText = "对话内容...";
    public string backgroundImage = "";
    public string leftCharacter = "";
    public string rightCharacter = "";
    public List<DialogueOptionData> options = new List<DialogueOptionData>();
}

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "三国游戏/对话数据")]
public class DialogueDataSO : ScriptableObject
{
    public string battleName = "官渡之战";
    public List<DialogueNodeData> dialogueNodes = new List<DialogueNodeData>();
}