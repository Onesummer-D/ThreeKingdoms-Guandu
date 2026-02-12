using UnityEngine;

public class FinalTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== 第二周最终测试 ===");

        // 等待所有管理器初始化
        Invoke("RunCompleteTest", 1.0f);
    }

    void RunCompleteTest()
    {
        // 第一阶段：测试所有剧情点
        TestAllPlotPoints();

        // 第二阶段：测试IF线系统
        Invoke("TestIfLines", 8.0f);

        // 第三阶段：测试结局系统
        Invoke("TestEndings", 12.0f);

        // 第四阶段：测试评分系统
        Invoke("TestScoring", 16.0f);

        // 第五阶段：测试回调接口
        Invoke("TestCallbacks", 20.0f);
    }

    void TestAllPlotPoints()
    {
        Debug.Log("--- 测试所有剧情点 ---");

        // 剧情点1
        PlotPointManager.Instance.HandleOptionA_Puzzle();
        PlotPointManager.Instance.HandleOptionB_DigTunnel();
        PlotPointManager.Instance.HandleOptionC_Ambush(2.0f);

        // 剧情点2
        Invoke("TestPlotPoint2", 3.0f);
    }

    void TestPlotPoint2()
    {
        PlotPointManager.Instance.HandlePlotPoint2_OptionB_Retreat();
        Invoke("TestPlotPoint3", 2.0f);
    }

    void TestPlotPoint3()
    {
        // 降低计策以触发IF2
        ResourceManager.Instance.ModifyStrategy(-8.0f);
        PlotPointManager.Instance.HandlePlotPoint3_OptionC_Suspect();

        // 测试另外两个选项
        Invoke("TestPlotPoint3Others", 2.0f);
    }

    void TestPlotPoint3Others()
    {
        PlotPointManager.Instance.HandlePlotPoint3_OptionA_Honest();
        PlotPointManager.Instance.HandlePlotPoint3_OptionB_Lie();

        // 剧情点4
        Invoke("TestPlotPoint4", 2.0f);
    }

    void TestPlotPoint4()
    {
        PlotPointManager.Instance.HandlePlotPoint4_OptionA_LeadPersonally();
        Invoke("TestPlotPoint5", 3.0f);
    }

    void TestPlotPoint5()
    {
        Debug.Log("--- 测试剧情点5 ---");

        // 测试三个选项
        PlotPointManager.Instance.HandlePlotPoint5_OptionA_FocusOnWuchao();
        PlotPointManager.Instance.HandlePlotPoint5_OptionB_SplitForces();
        PlotPointManager.Instance.HandlePlotPoint5_OptionC_SurroundWei();
    }

    void TestIfLines()
    {
        Debug.Log("--- 测试IF线系统 ---");

        if (IfLineManager.Instance != null)
        {
            IfLineManager.Instance.TestAllIfLines();
        }

        if (GameData.Instance != null)
        {
            Debug.Log($"总解锁IF线: {GameData.Instance.unlockedIfLines.Count}条");
        }
    }

    void TestEndings()
    {
        Debug.Log("--- 测试结局系统 ---");

        if (EndingManager.Instance != null)
        {
            // 测试触发各种结局
            EndingManager.Instance.TriggerEnding("IF1");
            EndingManager.Instance.TriggerEnding("IF2");
            EndingManager.Instance.TriggerEnding("IF5");
        }
    }

    void TestScoring()
    {
        Debug.Log("--- 测试评分系统 ---");

        if (ScoringSystem.Instance != null)
        {
            ScoringSystem.Instance.TestScoring();
        }
    }

    void TestCallbacks()
    {
        Debug.Log("--- 测试回调接口 ---");

        if (GameCallbacks.Instance != null)
        {
            GameCallbacks.Instance.TestAllCallbacks();
        }

        Debug.Log("✅ 第二周所有功能测试完成！");
    }
}