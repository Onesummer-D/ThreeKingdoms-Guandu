using System.Collections.Generic;
using UnityEngine;

public class DataLoader : MonoBehaviour
{
    public static DataLoader Instance { get; private set; }

    [Header("数据文件引用")]
    public DialogueDataSO dialogueData;

    // 改成 DialogueNode，不是 DialogueNodeData
    private Dictionary<int, DialogueNode> nodeDictionary = new Dictionary<int, DialogueNode>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDialogueData();
            Debug.Log("DataLoader单例初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadDialogueData()
    {
        if (dialogueData == null)
        {
            Debug.LogError("未设置对话数据文件！");
            return;
        }

        nodeDictionary.Clear();
        // 改成 dialogueData.nodes，不是 dialogueData.dialogueNodes
        foreach (var node in dialogueData.nodes)
        {
            nodeDictionary[node.nodeId] = node;
        }
        Debug.Log($"DataLoader: 加载了 {nodeDictionary.Count} 个节点");
    }

    // 返回类型改成 DialogueNode
    public DialogueNode GetNodeById(int nodeId)
    {
        if (nodeDictionary.ContainsKey(nodeId))
            return nodeDictionary[nodeId];

        Debug.LogWarning($"DataLoader: 找不到节点 {nodeId}");
        return null;
    }

    public void ReloadData()
    {
        LoadDialogueData();
    }
}