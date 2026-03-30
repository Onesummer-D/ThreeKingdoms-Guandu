using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    public event Action<int, DialogueOption> OnOptionSelected;
    public event Action<int> OnDialogueNodeShown;

    public DialogueNode CurrentNode { get { return currentNode; } }
    public bool IsShowing { get; private set; }

    private DialogueNode currentNode;
    private bool isDataLoaded = false;
    private Dictionary<int, DialogueNode> loadedNodes = new Dictionary<int, DialogueNode>();

    [Header("BGM自动配置（按节点ID范围）")]
    public AudioClip bgmBackgroundIntro;    // 100001-100005
    public AudioClip bgmPlot1;              // 100101-100199
    public AudioClip bgmPlot2;              // 200001-200999
    public AudioClip bgmPlot3;              // 300001-300999
    public AudioClip bgmPlot4Normal;        // 400001-400899（前半）
    public AudioClip bgmPlot4Battle;        // 400900-400999（后半，接近剧情点5）
    public AudioClip bgmPlot5Battle;        // 500101-500199（战斗）
    public AudioClip bgmPlot5Victory;       // 500200-500299（胜利/结局）
    public AudioClip bgmIfFailure;          // if线1/2/3/6
    public AudioClip bgmIfVictory;          // if线4
    public AudioClip bgmIfFinalVictory;     // if线5（最后的胜利）
    public AudioClip bgmWarBackground;      // 首页/菜单

    AudioClip GetAutoBGM(int nodeId)
    {
        Debug.Log($"[BGM调试] 节点{nodeId}请求BGM");

        // 背景介绍
        if (nodeId >= 100001 && nodeId <= 100005) return bgmBackgroundIntro;
        // 剧情点一
        if (nodeId >= 100101 && nodeId <= 100199) return bgmPlot1;
        // 剧情点二 (正常流程)
        if (nodeId >= 200001 && nodeId <= 200308) return bgmPlot2;

        // ✅ 修复：200401-200409沿用剧情点二BGM，不被判定为if线
        if (nodeId >= 200401 && nodeId <= 200409) return bgmPlot2;
        // if线1：从200309开始（排除200401-200409）
        if (nodeId >= 200309 && nodeId <= 200999) return bgmIfFailure;

        // 剧情点三 (正常流程)
        if (nodeId >= 300001 && nodeId <= 300409) return bgmPlot3;
        // if线2：从300410开始
        if (nodeId >= 300410 && nodeId <= 300999) return bgmIfFailure;

        // 剧情点四：400101-400107铺垫
        if (nodeId >= 400001 && nodeId <= 400107) return bgmPlot4Normal;
        // 剧情点四-战斗分支
        if (nodeId >= 400200 && nodeId <= 400999) return bgmPlot4Battle;

        // 剧情点五-战斗 (500101-500199)
        if (nodeId >= 500101 && nodeId <= 500199) return bgmPlot5Battle;

        // ✅ 修改：500200-500308全部用战争背景音（包括500201火烧乌巢节点）
        if (nodeId >= 500200 && nodeId <= 500308) return bgmWarBackground;

        // if线3 (500309-500316)
        if (nodeId >= 500309 && nodeId <= 500316) return bgmIfFailure;
        // if线4 (500317-500406)
        if (nodeId >= 500317 && nodeId <= 500406) return bgmIfVictory;
        // if线5 (500407-500413)
        if (nodeId >= 500407 && nodeId <= 500413) return bgmPlot5Victory;
        // if线6 (500414+)
        if (nodeId >= 500414 && nodeId <= 500999) return bgmIfFailure;

        return null;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("DialogueSystem初始化完成！");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    IEnumerator InitializeWhenReady()
    {
        // 等待DataLoader就绪（最多等3秒）
        float timeout = 3f;
        while (DataLoader.Instance == null && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (DataLoader.Instance == null)
        {
            Debug.LogError("DataLoader未找到！");
            yield break;
        }

        LoadAllDialogueData();

        if (isDataLoaded && loadedNodes.Count > 0)
        {
            Debug.Log($"<color=green>成功加载 {loadedNodes.Count} 个对话节点</color>");
        }
        else
        {
            Debug.LogError("对话数据加载失败！");
        }
    }

    void LoadAllDialogueData()
    {
        if (DataLoader.Instance == null || DataLoader.Instance.dialogueData == null)
        {
            Debug.LogError("DataLoader未初始化或dialogueData为null");
            return;
        }

        loadedNodes.Clear();

        foreach (var node in DataLoader.Instance.dialogueData.nodes)
        {
            if (node != null && !loadedNodes.ContainsKey(node.nodeId))
            {
                loadedNodes[node.nodeId] = node;
            }
            else if (node != null)
            {
                Debug.LogWarning($"重复节点ID: {node.nodeId}");
            }
        }

        isDataLoaded = loadedNodes.Count > 0;
        Debug.Log($"对话数据加载完成: {loadedNodes.Count}个节点");
    }

    DialogueNode FindNodeById(int nodeId)
    {
        if (loadedNodes.ContainsKey(nodeId))
            return loadedNodes[nodeId];

        Debug.LogError($"<color=red>找不到对话节点: {nodeId}</color>");
        return null;
    }

    public void ShowDialogueNode(int nodeId)
    {
        Debug.Log($"<color=cyan>DialogueSystem: 请求显示节点 {nodeId}</color>");

        currentNode = FindNodeById(nodeId);

        if (currentNode == null)
        {
            Debug.LogError($"无法显示节点：{nodeId}");
            return;
        }

        IsShowing = true;

        // 自动匹配BGM（优先用手动配置的，没有则自动匹配）
        AudioClip targetBGM = currentNode.bgmClip;

        // 如果节点没手动填BGM，且不是小游戏节点，则自动匹配
        if (targetBGM == null && !IsMiniGameNode(nodeId))
        {
            targetBGM = GetAutoBGM(nodeId);
        }

        if (currentNode.sfxOnEnter != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(currentNode.sfxOnEnter);
        }

        // 播放BGM（小游戏节点不播放）
        if (targetBGM != null && AudioManager.Instance != null && !IsMiniGameNode(nodeId))
        {
            AudioManager.Instance.PlayBGM(targetBGM, currentNode.loopBGM, currentNode.stopPreviousBGM);
            Debug.Log($"<color=magenta>播放BGM: {targetBGM.name}</color>");
        }
        else if (IsMiniGameNode(nodeId) && AudioManager.Instance != null)
        {
            // 小游戏节点：停止BGM（让小游戏用自己的音效）
            AudioManager.Instance.PauseBGM();
            Debug.Log("<color=magenta>进入小游戏，暂停BGM</color>");
        }

        // 如果是勋章/称号节点，不切换BGM（保持当前）
        if (nodeId == 500215) // 假设勋章节点是500215
        {
            Debug.Log("<color=magenta>勋章节点，保持当前BGM</color>");
            // 不调用PlayBGM，保持现状
        }
        else if (targetBGM != null && AudioManager.Instance != null && !IsMiniGameNode(nodeId))
        {
            AudioManager.Instance.PlayBGM(targetBGM, currentNode.loopBGM, currentNode.stopPreviousBGM);
        }

        // 播放进入音效（500201火烧、500215称号等）
        if (currentNode.sfxOnEnter != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(currentNode.sfxOnEnter);
            Debug.Log($"<color=magenta>播放音效: {currentNode.sfxOnEnter.name}</color>");
        }

        OnDialogueNodeShown?.Invoke(nodeId);
        Debug.Log($"<color=green>DialogueSystem：成功触发节点 {nodeId} 显示事件</color>");
    }

    // 判断是否是特殊节点（小游戏/动画）
    bool IsMiniGameNode(int nodeId)
    {
        return false;
    }

    public void HandleOptionSelected(int optionIndex)
    {
        if (currentNode == null || currentNode.options == null || optionIndex >= currentNode.options.Count)
        {
            Debug.LogWarning($"无效选项索引: {optionIndex}");
            return;
        }

        DialogueOption selectedOption = currentNode.options[optionIndex];
        Debug.Log($"<color=yellow>玩家选择了[{optionIndex}]: {selectedOption.optionText}</color>");

        // 播放选项点击音
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(AudioManager.Instance.defaultButtonClick);
        }

        if (!string.IsNullOrEmpty(selectedOption.condition) && !CheckCondition(selectedOption.condition))
        {
            Debug.Log($"条件不满足: {selectedOption.condition}");
            return;
        }

        ApplyResourceEffects(selectedOption);

        // 播放数值变化音效
        if (AudioManager.Instance != null)
        {
            float totalChange = selectedOption.troopEffect + selectedOption.foodEffect +
                               selectedOption.strategyEffect + selectedOption.riskEffect;
            if (totalChange != 0)
            {
                AudioManager.Instance.PlayValueChange(totalChange);
            }
        }

        OnOptionSelected?.Invoke(optionIndex, selectedOption);
    }

    bool CheckCondition(string condition)
    {
        // 简单条件解析，可根据需要扩展
        if (condition.Contains(">"))
        {
            var parts = condition.Split('>');
            if (parts.Length == 2)
            {
                string resourceType = parts[0].Trim();
                float requiredValue = float.Parse(parts[1].Trim());
                float currentValue = GetResourceValue(resourceType);
                return currentValue > requiredValue;
            }
        }
        else if (condition.Contains("<"))
        {
            var parts = condition.Split('<');
            if (parts.Length == 2)
            {
                string resourceType = parts[0].Trim();
                float requiredValue = float.Parse(parts[1].Trim());
                float currentValue = GetResourceValue(resourceType);
                return currentValue < requiredValue;
            }
        }
        return true;
    }

    float GetResourceValue(string resourceType)
    {
        if (ResourceManager.Instance == null) return 0;

        switch (resourceType.ToLower())
        {
            case "troop": return ResourceManager.Instance.GetTroop();
            case "food": return ResourceManager.Instance.GetFood();
            case "strategy": return ResourceManager.Instance.GetStrategy();
            case "risk": return ResourceManager.Instance.GetRisk();
            default: return 0;
        }
    }

    void ApplyResourceEffects(DialogueOption option)
    {
        if (ResourceManager.Instance == null) return;

        // 先处理减少（负值）
        if (option.troopEffect < 0)
        {
            ResourceManager.Instance.ModifyTroop(option.troopEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(-1);
            Debug.Log($"资源变化 - 兵力: {option.troopEffect}");
        }
        if (option.foodEffect < 0)
        {
            ResourceManager.Instance.ModifyFood(option.foodEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(-1);
            Debug.Log($"资源变化 - 粮草: {option.foodEffect}");
        }
        if (option.strategyEffect < 0)
        {
            ResourceManager.Instance.ModifyStrategy(option.strategyEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(-1);
            Debug.Log($"资源变化 - 计策: {option.strategyEffect}");
        }
        if (option.riskEffect < 0)
        {
            ResourceManager.Instance.ModifyRisk(option.riskEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(-1);
            Debug.Log($"资源变化 - 风险: {option.riskEffect}");
        }

        // 再处理增加（正值）
        if (option.troopEffect > 0)
        {
            ResourceManager.Instance.ModifyTroop(option.troopEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(1);
            Debug.Log($"资源变化 - 兵力: +{option.troopEffect}");
        }
        if (option.foodEffect > 0)
        {
            ResourceManager.Instance.ModifyFood(option.foodEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(1);
            Debug.Log($"资源变化 - 粮草: +{option.foodEffect}");
        }
        if (option.strategyEffect > 0)
        {
            ResourceManager.Instance.ModifyStrategy(option.strategyEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(1);
            Debug.Log($"资源变化 - 计策: +{option.strategyEffect}");
        }
        if (option.riskEffect > 0)
        {
            ResourceManager.Instance.ModifyRisk(option.riskEffect);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayValueChange(1);
            Debug.Log($"资源变化 - 风险: +{option.riskEffect}");
        }
    }

    public void StartDialogue()
    {
        if (loadedNodes.Count > 0)
        {
            ShowDialogueNode(100001);
        }
        else
        {
            Debug.LogError("DialogueSystem: 没有加载对话数据");
        }
    }

    public List<DialogueOption> GetCurrentOptions()
    {
        return currentNode != null ? currentNode.options : new List<DialogueOption>();
    }

    // 调试辅助：打印当前节点信息
    [ContextMenu("打印当前节点信息")]
    void DebugCurrentNode()
    {
        if (currentNode == null)
        {
            Debug.Log("当前没有节点");
            return;
        }

        Debug.Log($"当前节点: {currentNode.nodeId}");
        Debug.Log($"文案: {currentNode.dialogueText}");
        Debug.Log($"背景: {(currentNode.backgroundImage ? currentNode.backgroundImage.name : "null")}");
        Debug.Log($"头像: {currentNode.avatarCharacter}");
        Debug.Log($"左人物: {currentNode.leftCharacter}");
        Debug.Log($"右人物: {currentNode.rightCharacter}");
        Debug.Log($"选项数: {(currentNode.options != null ? currentNode.options.Count : 0)}");
    }
}