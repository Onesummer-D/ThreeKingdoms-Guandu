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

        // 触发事件通知UI更新
        OnDialogueNodeShown?.Invoke(nodeId);

        Debug.Log($"<color=green>DialogueSystem：成功触发节点 {nodeId} 显示事件</color>");
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

        if (!string.IsNullOrEmpty(selectedOption.condition) && !CheckCondition(selectedOption.condition))
        {
            Debug.Log($"条件不满足: {selectedOption.condition}");
            return;
        }

        ApplyResourceEffects(selectedOption);
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

        if (option.troopEffect != 0)
        {
            ResourceManager.Instance.ModifyTroop(option.troopEffect);
            Debug.Log($"资源变化 - 兵力: {option.troopEffect:+#;-#;0}");
        }
        if (option.foodEffect != 0)
        {
            ResourceManager.Instance.ModifyFood(option.foodEffect);
            Debug.Log($"资源变化 - 粮草: {option.foodEffect:+#;-#;0}");
        }
        if (option.strategyEffect != 0)
        {
            ResourceManager.Instance.ModifyStrategy(option.strategyEffect);
            Debug.Log($"资源变化 - 计策: {option.strategyEffect:+#;-#;0}");
        }
        if (option.riskEffect != 0)
        {
            ResourceManager.Instance.ModifyRisk(option.riskEffect);
            Debug.Log($"资源变化 - 风险: {option.riskEffect:+#;-#;0}");
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