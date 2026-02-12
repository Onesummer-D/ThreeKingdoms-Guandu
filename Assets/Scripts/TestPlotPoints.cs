using UnityEngine;

public class TestPlotPoints : MonoBehaviour
{
    void Start()
    {
        // 等一帧让所有管理器初始化
        Invoke("RunTests", 0.5f);

        // 新增：1.5秒后测试剧情点2
        Invoke("TestPlotPoint2", 1.5f);

        // 新增：2秒后测试剧情点3
        Invoke("TestPlotPoint3", 2.0f);
    }

    void RunTests()
    {
        Debug.Log("=== 开始测试剧情点功能 ===");

        if (PlotPointManager.Instance == null)
        {
            Debug.LogError("找不到PlotPointManager！");
            return;
        }

        // 测试选项A
        PlotPointManager.Instance.HandleOptionA_Puzzle();

        // 等3秒让拼图"完成"
        Invoke("TestOptionB", 3.0f);
    }

    void TestOptionB()
    {
        Debug.Log("--- 测试选项B ---");
        PlotPointManager.Instance.HandleOptionB_DigTunnel();

        Invoke("TestOptionC", 1.0f);
    }

    void TestOptionC()
    {
        Debug.Log("--- 测试选项C ---");

        // 测试合理输入
        PlotPointManager.Instance.HandleOptionC_Ambush(2.0f);

        Invoke("TestOptionC_Bad", 1.0f);
    }

    void TestOptionC_Bad()
    {
        Debug.Log("--- 测试选项C（不合理输入）---");
        PlotPointManager.Instance.HandleOptionC_Ambush(10.0f);

        Debug.Log("=== 所有测试完成 ===");
    }

    void TestPlotPoint2()
    {
        Debug.Log("=== 测试剧情点2 ===");

        // 测试选项A：坚守
        PlotPointManager.Instance.HandlePlotPoint2_OptionA_Stay();

        Invoke("TestOptionBAndIF", 1.0f);
    }

    void TestOptionBAndIF()
    {
        // 测试选项B：撤退（触发IF1）
        PlotPointManager.Instance.HandlePlotPoint2_OptionB_Retreat();

        // 测试视角切换
        Invoke("TestPerspectiveSwitch", 1.5f);
    }

    void TestPerspectiveSwitch()
    {
        PlotPointManager.Instance.HandlePerspectiveSwitch_XuYou();

        // 测试IF线管理器
        Invoke("TestIfLineManager", 1.0f);
    }

    void TestIfLineManager()
    {
        if (IfLineManager.Instance != null)
        {
            IfLineManager.Instance.TestAllIfLines();
        }

        // 测试结局管理器
        Invoke("TestEndingManager", 2.0f);
    }

    void TestEndingManager()
    {
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.TestAllEndings();
        }

        Debug.Log("=== 剧情点2测试完成 ===");
    }

    void TestPlotPoint3()
    {
        Debug.Log("=== 测试剧情点3：许攸夜访 ===");

        // 先确保有足够的计策值
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyStrategy(-10.0f); // 故意降低
            Debug.Log("故意降低计策值以测试IF线2触发");
        }

        // 测试选项C（应该触发IF线2）
        Invoke("TestOptionC_TriggerIF2", 1.0f);
    }

    void TestOptionC_TriggerIF2()
    {
        Debug.Log("--- 测试选项C：怀疑许攸 ---");
        PlotPointManager.Instance.HandlePlotPoint3_OptionC_Suspect();

        Invoke("TestOtherOptions", 2.0f);
    }

    void TestOtherOptions()
    {
        Debug.Log("--- 测试选项A：说实话 ---");
        PlotPointManager.Instance.HandlePlotPoint3_OptionA_Honest();

        Invoke("TestOptionB_Lie", 1.5f);
    }

    void TestOptionB_Lie()
    {
        Debug.Log("--- 测试选项B：说谎 ---");
        PlotPointManager.Instance.HandlePlotPoint3_OptionB_Lie();

        // 测试数据验证系统
        Invoke("TestDataValidator", 1.5f);
    }

    void TestDataValidator()
    {
        Debug.Log("=== 测试数据验证系统 ===");

        if (DataValidator.Instance != null)
        {
            DataValidator.Instance.RunAllTests();
        }

        Debug.Log("=== 剧情点3测试完成 ===");
    }
}