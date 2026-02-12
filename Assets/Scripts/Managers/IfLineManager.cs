using UnityEngine;
using System.Collections.Generic;

public class IfLineManager : MonoBehaviour
{
    public static IfLineManager Instance { get; private set; }

    // 所有IF线定义
    public enum IfLineType
    {
        None,
        IF1_半途而废,      // 粮草将尽时撤退
        IF2_错失良机,      // 怀疑许攸
        IF3_元气大伤,      // 乌巢分兵回援（资源不足）
        IF4_以退为进,      // 乌巢分兵回援（资源足够）
        IF5_完美结局,      // 激进策略成功
        IF6_惨败结局       // 激进策略失败
    }

    // 已解锁的IF线
    private List<IfLineType> unlockedIfLines = new List<IfLineType>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("IfLineManager初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 解锁IF线
    public void UnlockIfLine(IfLineType ifLine)
    {
        if (!unlockedIfLines.Contains(ifLine))
        {
            unlockedIfLines.Add(ifLine);
            Debug.Log($"🎯 解锁IF线：{ifLine}");

            // 保存到GameData（兼容你已有的系统）
            if (GameData.Instance != null)
            {
                GameData.Instance.UnlockIfLine(ifLine.ToString());
            }

            // 可以触发UI显示
            // UIManager.Instance.ShowIfLineUnlocked(ifLine.ToString());
        }
    }

    // 检查是否已解锁
    public bool HasUnlocked(IfLineType ifLine)
    {
        return unlockedIfLines.Contains(ifLine);
    }

    // 获取所有已解锁的IF线
    public List<string> GetAllUnlockedIfLines()
    {
        List<string> result = new List<string>();
        foreach (var line in unlockedIfLines)
        {
            result.Add(line.ToString());
        }
        return result;
    }

    // 检查IF线触发条件
    public void CheckIfLineConditions()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        // IF3：元气大伤（资源不足）
        if (rm.GetTroop() < 5.0f && rm.GetFood() < 5.0f && !HasUnlocked(IfLineType.IF3_元气大伤))
        {
            UnlockIfLine(IfLineType.IF3_元气大伤);
        }

        // IF4：以退为进（资源足够）
        if (rm.GetTroop() > 8.0f && rm.GetFood() > 8.0f && !HasUnlocked(IfLineType.IF4_以退为进))
        {
            UnlockIfLine(IfLineType.IF4_以退为进);
        }
    }

    // 测试函数
    public void TestAllIfLines()
    {
        Debug.Log("=== 测试所有IF线 ===");

        // 解锁所有IF线（测试用）
        UnlockIfLine(IfLineType.IF1_半途而废);
        UnlockIfLine(IfLineType.IF2_错失良机);
        UnlockIfLine(IfLineType.IF3_元气大伤);

        Debug.Log($"已解锁 {unlockedIfLines.Count} 条IF线");
        foreach (var line in unlockedIfLines)
        {
            Debug.Log($" - {line}");
        }
    }
}