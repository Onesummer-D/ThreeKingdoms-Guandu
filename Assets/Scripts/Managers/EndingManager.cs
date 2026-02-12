using UnityEngine;
using System.Collections.Generic;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    // 结局类型
    public enum EndingType
    {
        史实胜利,
        半途而废,    // IF1
        错失良机,    // IF2
        元气大伤,    // IF3
        以退为进,    // IF4
        完美结局,    // IF5
        惨败结局     // IF6
    }

    // 已触发的结局
    private List<EndingType> triggeredEndings = new List<EndingType>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("EndingManager初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 触发结局
    public void TriggerEnding(EndingType ending)
    {
        if (!triggeredEndings.Contains(ending))
        {
            triggeredEndings.Add(ending);
            Debug.Log($"🎬 触发结局：{ending}");

            // 显示结局描述
            ShowEndingDescription(ending);

            // 停止游戏
            if (GameData.Instance != null)
            {
                GameData.Instance.isGameRunning = false;
            }
        }
    }

    // 通过IF线ID触发结局
    public void TriggerEnding(string ifLineId)
    {
        Debug.Log($"尝试通过IF线触发结局：{ifLineId}");

        switch (ifLineId.ToUpper())
        {
            case "IF1": TriggerEnding(EndingType.半途而废); break;
            case "IF2": TriggerEnding(EndingType.错失良机); break;
            case "IF3": TriggerEnding(EndingType.元气大伤); break;
            case "IF4": TriggerEnding(EndingType.以退为进); break;
            case "IF5": TriggerEnding(EndingType.完美结局); break;
            case "IF6": TriggerEnding(EndingType.惨败结局); break;
            case "HISTORICAL": 
            case "史实胜利":    
                TriggerEnding(EndingType.史实胜利);
                break;
            default:
                Debug.LogWarning($"未知的IF线ID：{ifLineId}");
                // 可以默认为史实胜利
                TriggerEnding(EndingType.史实胜利);
                break;
        }
    }

    // 显示结局描述
    void ShowEndingDescription(EndingType ending)
    {
        string description = "";

        switch (ending)
        {
            case EndingType.史实胜利:
                description = "你成功重现了官渡之战的胜利！曹操以少胜多，奠定了统一北方的基础。";
                break;
            case EndingType.半途而废:
                description = "你选择了撤退，但袁绍趁机掩杀，曹军损失惨重，错失了击败袁绍的最佳时机。";
                break;
            case EndingType.错失良机:
                description = "你怀疑许攸的诚意，错失了关键情报，最终因粮草耗尽而兵败。";
                break;
            case EndingType.完美结局:
                description = "完美的战略部署！曹操提前数年统一北方，加速了天下统一的进程。";
                break;
            default:
                description = $"结局：{ending}。具体的剧情描述需要文案同学补充。";
                break;
        }

        Debug.Log($"═══════════════════════════════════");
        Debug.Log($"            《{ending}》");
        Debug.Log($"═══════════════════════════════════");
        Debug.Log(description);
        Debug.Log($"═══════════════════════════════════");

        // 这里将来要调用UI显示结局
        // UIManager.Instance.ShowEnding(description);
    }

    // 根据资源检查结局（在剧情点5之后调用）
    public void CheckResourceBasedEndings()
    {
        var rm = ResourceManager.Instance;
        if (rm == null) return;

        // 检查完美结局（IF5）
        if (rm.GetTroop() > 15.0f && rm.GetFood() > 15.0f && rm.GetRisk() > 7.0f)
        {
            TriggerEnding(EndingType.完美结局);
        }
        // 检查惨败结局（IF6）
        else if (rm.GetRisk() > 8.0f && rm.GetTroop() < 5.0f && rm.GetFood() < 5.0f)
        {
            TriggerEnding(EndingType.惨败结局);
        }
        // 检查史实结局
        else if (rm.GetTroop() > 8.0f && rm.GetFood() > 5.0f && rm.GetStrategy() > 6.0f)
        {
            TriggerEnding(EndingType.史实胜利);
        }
    }

    // 测试函数
    public void TestAllEndings()
    {
        Debug.Log("=== 测试所有结局 ===");

        TriggerEnding(EndingType.史实胜利);

        // 等待2秒再触发下一个
        Invoke("TestIfLineEndings", 2.0f);
    }

    void TestIfLineEndings()
    {
        TriggerEnding(EndingType.半途而废);
        Invoke("TestAnotherEnding", 1.5f);
    }

    void TestAnotherEnding()
    {
        TriggerEnding(EndingType.完美结局);
    }
}