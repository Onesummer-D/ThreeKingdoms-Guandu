using System.Collections.Generic;
using UnityEngine;

public class DataLoader : MonoBehaviour
{
    public static DataLoader Instance { get; private set; }

    [Header("数据文件引用")]
    public DialogueDataSO dialogueData;

    private Dictionary<int, DialogueNodeData> nodeDictionary = new Dictionary<int, DialogueNodeData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDialogueData();
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
        foreach (var node in dialogueData.dialogueNodes)
        {
            nodeDictionary[node.nodeId] = node;
        }
        Debug.Log($"DataLoader: 加载了 {nodeDictionary.Count} 个节点");
    }

    public DialogueNodeData GetNodeById(int nodeId)
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