using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 对话选项的数据结构（用于运行时）
[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextNodeId;
    public float troopEffect;
    public float foodEffect;
    public float strategyEffect;
    public float riskEffect;
    public string miniGameType;
    public string condition;
}

// 对话节点的数据结构（用于运行时）
[System.Serializable]
public class DialogueNode
{
    public int nodeId;
    public string dialogueText;
    public string backgroundImage;
    public string leftCharacter;
    public string rightCharacter;
    public List<DialogueOption> options = new List<DialogueOption>();
}

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    // UI组件引用
    public Text dialogueTextDisplay;
    public Button[] optionButtons;
    public Text resourceTextDisplay;
    public Image backgroundImageDisplay;
    public Image leftCharacterDisplay;
    public Image rightCharacterDisplay;

    // 当前对话节点
    private DialogueNode currentNode;

    // 数据加载状态
    private bool isDataLoaded = false;
    private Dictionary<int, DialogueNode> loadedNodes = new Dictionary<int, DialogueNode>();

    void Awake()
    {
        // 单例模式
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
        // 等待DataLoader初始化
        StartCoroutine(InitializeAfterDataLoader());
    }

    IEnumerator InitializeAfterDataLoader()
    {
        // 等待DataLoader初始化
        yield return new WaitUntil(() => DataLoader.Instance != null);

        // 额外等待一帧确保数据加载完成
        yield return null;

        // 尝试加载数据
        LoadAllDialogueData();

        if (isDataLoaded && loadedNodes.Count > 0)
        {
            Debug.Log($"成功加载 {loadedNodes.Count} 个对话节点，开始游戏");
            ShowDialogueNode(1001);
        }
        else
        {
            Debug.LogError("对话数据加载失败！使用备用测试数据");
            CreateBackupTestData();
            ShowDialogueNode(1001);
        }
    }

    // 从DataLoader加载所有对话数据
    void LoadAllDialogueData()
    {
        if (DataLoader.Instance == null || DataLoader.Instance.dialogueData == null)
        {
            Debug.LogError("DataLoader未初始化或未设置对话数据");
            return;
        }

        loadedNodes.Clear();

        foreach (var nodeData in DataLoader.Instance.dialogueData.dialogueNodes)
        {
            DialogueNode node = ConvertToDialogueNode(nodeData);
            if (node != null)
            {
                loadedNodes[node.nodeId] = node;
                Debug.Log($"加载节点: {node.nodeId}");
            }
        }

        isDataLoaded = loadedNodes.Count > 0;
        Debug.Log($"对话数据加载完成: {loadedNodes.Count}个节点");
    }

    // 将ScriptableObject数据转换为运行时数据
    DialogueNode ConvertToDialogueNode(DialogueNodeData nodeData)
    {
        if (nodeData == null) return null;

        DialogueNode node = new DialogueNode
        {
            nodeId = nodeData.nodeId,
            dialogueText = nodeData.dialogueText,
            backgroundImage = nodeData.backgroundImage,
            leftCharacter = nodeData.leftCharacter,
            rightCharacter = nodeData.rightCharacter
        };

        foreach (var optionData in nodeData.options)
        {
            DialogueOption option = new DialogueOption
            {
                optionText = optionData.optionText,
                nextNodeId = optionData.nextNodeId,
                troopEffect = optionData.troopEffect,
                foodEffect = optionData.foodEffect,
                strategyEffect = optionData.strategyEffect,
                riskEffect = optionData.riskEffect,
                miniGameType = optionData.miniGameType,
                condition = optionData.condition
            };
            node.options.Add(option);
        }

        return node;
    }

    // 备用测试数据（当DataLoader失败时使用）
    void CreateBackupTestData()
    {
        DialogueNode node1 = new DialogueNode
        {
            nodeId = 1001,
            dialogueText = "袁绍初战得利，筑土山放箭，曹军压力巨大。此时应当如何应对？",
            backgroundImage = "bg_battlefield"
        };

        // 选项A
        DialogueOption optionA = new DialogueOption
        {
            optionText = "连夜赶造发石车，以牙还牙",
            nextNodeId = 1002,
            troopEffect = 1.0f,
            miniGameType = "puzzle"
        };
        node1.options.Add(optionA);

        // 选项B
        DialogueOption optionB = new DialogueOption
        {
            optionText = "令将士挖掘地道，奇袭袁营",
            nextNodeId = 1003,
            foodEffect = -1.0f
        };
        node1.options.Add(optionB);

        // 选项C
        DialogueOption optionC = new DialogueOption
        {
            optionText = "佯装败退，设下埋伏",
            nextNodeId = 1004
        };
        node1.options.Add(optionC);

        loadedNodes[node1.nodeId] = node1;

        // 第二个节点
        DialogueNode node2 = new DialogueNode
        {
            nodeId = 1002,
            dialogueText = "你选择了建造发石车。士兵们士气大振！"
        };

        DialogueOption continueOption = new DialogueOption
        {
            optionText = "继续",
            nextNodeId = 1005
        };
        node2.options.Add(continueOption);

        loadedNodes[node2.nodeId] = node2;
    }

    // 根据ID查找对话节点
    DialogueNode FindNodeById(int nodeId)
    {
        if (loadedNodes.ContainsKey(nodeId))
            return loadedNodes[nodeId];

        Debug.LogWarning($"找不到对话节点: {nodeId}");
        return null;
    }

    // 显示指定ID的对话节点
    public void ShowDialogueNode(int nodeId)
    {
        currentNode = FindNodeById(nodeId);

        if (currentNode == null)
        {
            Debug.LogError($"无法显示节点：{nodeId}");
            return;
        }

        // 更新游戏状态
        if (GameData.Instance != null)
            GameData.Instance.currentDialogueNodeId = nodeId;

        // 更新UI显示
        UpdateDialogueDisplay();
        UpdateOptionButtons();
        UpdateResourceDisplay();

        Debug.Log($"显示节点：{nodeId}");
    }

    // 更新对话显示
    void UpdateDialogueDisplay()
    {
        if (dialogueTextDisplay != null)
            dialogueTextDisplay.text = currentNode.dialogueText;

        // 更新背景图（如果有）
        if (backgroundImageDisplay != null && !string.IsNullOrEmpty(currentNode.backgroundImage))
        {
            // 这里需要加载图片资源，暂时先记录
            Debug.Log($"需要加载背景图: {currentNode.backgroundImage}");
        }

        // 更新角色立绘（如果有）
        if (leftCharacterDisplay != null && !string.IsNullOrEmpty(currentNode.leftCharacter))
        {
            Debug.Log($"显示左侧角色: {currentNode.leftCharacter}");
        }

        if (rightCharacterDisplay != null && !string.IsNullOrEmpty(currentNode.rightCharacter))
        {
            Debug.Log($"显示右侧角色: {currentNode.rightCharacter}");
        }
    }

    // 更新选项按钮
    void UpdateOptionButtons()
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentNode.options.Count)
            {
                // 显示并设置按钮文本
                optionButtons[i].gameObject.SetActive(true);
                Text buttonText = optionButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                    buttonText.text = currentNode.options[i].optionText;

                // 绑定点击事件
                int optionIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
            }
            else
            {
                // 隐藏多余的按钮
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // 玩家选择了某个选项
    void OnOptionSelected(int optionIndex)
    {
        if (currentNode == null || optionIndex >= currentNode.options.Count)
            return;

        DialogueOption selectedOption = currentNode.options[optionIndex];
        Debug.Log($"玩家选择了：{selectedOption.optionText}");

        // 检查条件（如果有）
        if (!string.IsNullOrEmpty(selectedOption.condition) && !CheckCondition(selectedOption.condition))
        {
            Debug.Log($"条件不满足: {selectedOption.condition}");
            return;
        }

        // 应用资源效果
        ApplyResourceEffects(selectedOption);

        // 处理小游戏或直接跳转
        if (!string.IsNullOrEmpty(selectedOption.miniGameType))
        {
            Debug.Log($"开始小游戏: {selectedOption.miniGameType}");
            StartMiniGame(selectedOption.miniGameType, selectedOption.nextNodeId);
        }
        else
        {
            ShowDialogueNode(selectedOption.nextNodeId);
        }
    }

    // 检查条件
    bool CheckCondition(string condition)
    {
        // 这里可以扩展复杂的条件检查逻辑
        // 目前只实现简单的数值检查

        if (condition.Contains(">"))
        {
            // 示例: "troop>5"
            var parts = condition.Split('>');
            if (parts.Length == 2)
            {
                string resourceType = parts[0].Trim();
                float requiredValue = float.Parse(parts[1].Trim());

                float currentValue = GetResourceValue(resourceType);
                return currentValue > requiredValue;
            }
        }

        // 默认通过
        return true;
    }

    // 获取资源当前值
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

    // 应用资源效果
    void ApplyResourceEffects(DialogueOption option)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager未初始化");
            return;
        }

        if (option.troopEffect != 0)
            ResourceManager.Instance.ModifyTroop(option.troopEffect);

        if (option.foodEffect != 0)
            ResourceManager.Instance.ModifyFood(option.foodEffect);

        if (option.strategyEffect != 0)
            ResourceManager.Instance.ModifyStrategy(option.strategyEffect);

        if (option.riskEffect != 0)
            ResourceManager.Instance.ModifyRisk(option.riskEffect);
    }

    // 开始小游戏
    void StartMiniGame(string gameType, int nextNodeId)
    {
        Debug.Log($"开始小游戏: {gameType}");

        // 这里实现小游戏逻辑
        // 目前先模拟小游戏完成，直接跳转
        OnMiniGameCompleted(true, nextNodeId);
    }

    // 小游戏完成回调
    void OnMiniGameCompleted(bool success, int nextNodeId)
    {
        if (success)
        {
            ShowDialogueNode(nextNodeId);
        }
        else
        {
            Debug.Log("小游戏失败");
            // 可以显示失败提示或重试
        }
    }

    // 更新资源显示
    void UpdateResourceDisplay()
    {
        if (resourceTextDisplay != null && ResourceManager.Instance != null)
            resourceTextDisplay.text = ResourceManager.Instance.GetResourcesText();
    }

    // 重新加载数据（策划修改后调用）
    public void ReloadDialogueData()
    {
        LoadAllDialogueData();
        Debug.Log("对话数据已重新加载");

        // 重新显示当前节点
        if (currentNode != null)
            ShowDialogueNode(currentNode.nodeId);
    }

    // 调试：打印所有节点
    public void PrintAllNodes()
    {
        Debug.Log("=== 所有对话节点 ===");
        foreach (var kvp in loadedNodes)
        {
            Debug.Log($"节点 {kvp.Key}: {kvp.Value.dialogueText}");
            foreach (var option in kvp.Value.options)
            {
                Debug.Log($"  - {option.optionText} → {option.nextNodeId}");
            }
        }
    }
}