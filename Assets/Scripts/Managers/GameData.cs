using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    // 不再直接存储资源值，而是通过ResourceManager
    public int currentDialogueNodeId = 1001;
    public bool isGameRunning = true;

    // 存储解锁的IF线
    public List<string> unlockedIfLines = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化ResourceManager（如果还没有）
            if (ResourceManager.Instance == null)
            {
                gameObject.AddComponent<ResourceManager>();
            }

            Debug.Log("GameData初始化完成！");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 这些方法现在转发给ResourceManager
    public void ModifyTroop(float value)
    {
        ResourceManager.Instance.ModifyTroop(value);
    }

    public void ModifyFood(float value)
    {
        ResourceManager.Instance.ModifyFood(value);
    }

    public void ModifyStrategy(float value)
    {
        ResourceManager.Instance.ModifyStrategy(value);
    }

    public void ModifyRisk(float value)
    {
        ResourceManager.Instance.ModifyRisk(value);
    }

    // 获取资源文本
    public string GetResourceText()
    {
        return ResourceManager.Instance.GetResourcesText();
    }

    // 解锁IF线
    public void UnlockIfLine(string ifLineId)
    {
        if (!unlockedIfLines.Contains(ifLineId))
        {
            unlockedIfLines.Add(ifLineId);
            Debug.Log($"解锁IF线: {ifLineId}");
        }
    }

    // 检查是否已解锁IF线
    public bool HasUnlockedIfLine(string ifLineId)
    {
        return unlockedIfLines.Contains(ifLineId);
    }
}