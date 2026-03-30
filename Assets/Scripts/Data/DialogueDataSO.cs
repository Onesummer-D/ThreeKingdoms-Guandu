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

    // 人物显示（严格按你之前的规则！）
    public string avatarCharacter = "";     // 头像
    public string leftCharacter = "";       // 左侧立绘（袁绍/许攸等）
    public string rightCharacter = "";      // 右侧立绘（曹操固定站这里！）

    public List<DialogueOption> options = new List<DialogueOption>();
    public int nextNodeId;  // 无选项时的下一个节点

    // 🏆 彩蛋称号设置（你原有的）
    [Header("彩蛋称号设置")]
    public Sprite achievementSprite;
    public Vector2 achievementPosition = new Vector2(0, 300);
    public float achievementScale = 1f;
    public bool playPopAnimation = true;

    // ========== 新增：音频配置（关键！）==========
    [Header("音频 - BGM（背景音乐）")]
    [Tooltip("剧情点BGM，1-3分钟的长音乐，不填则不换BGM")]
    public AudioClip bgmClip;

    [Tooltip("是否循环播放（铺垫剧情true，特殊动画false）")]
    public bool loopBGM = true;

    [Tooltip("是否停止之前的BGM（一般勾上）")]
    public bool stopPreviousBGM = true;

    [Header("音频 - 进入音效（SFX）")]
    [Tooltip("进入节点时播放的音效，如500215称号音、500201火烧音")]
    public AudioClip sfxOnEnter;
    // ============================================
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