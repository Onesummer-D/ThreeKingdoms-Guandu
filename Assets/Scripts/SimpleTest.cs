using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🟢 SimpleTest脚本的Start方法被调用！");

        // 直接测试你的管理器
        if (PlotPointManager.Instance != null)
        {
            Debug.Log("✅ PlotPointManager存在！");
            PlotPointManager.Instance.HandleOptionA_Puzzle();
        }
        else
        {
            Debug.LogError("❌ PlotPointManager不存在！");
        }
    }
}