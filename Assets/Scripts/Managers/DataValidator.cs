using UnityEngine;

public class DataValidator : MonoBehaviour
{
    public static DataValidator Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("DataValidator初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === 验证输入值范围 ===
    public bool ValidateInputValue(float inputValue, float min, float max, string valueName)
    {
        if (inputValue < min)
        {
            Debug.LogError($"❌ {valueName} 输入值 {inputValue} 小于最小值 {min}");
            return false;
        }

        if (inputValue > max)
        {
            Debug.LogError($"❌ {valueName} 输入值 {inputValue} 大于最大值 {max}");
            return false;
        }

        Debug.Log($"✅ {valueName} 输入值 {inputValue} 在范围 [{min}, {max}] 内");
        return true;
    }

    // === 检查资源是否充足 ===
    public bool ValidateResourceSufficiency(float requiredTroop, float requiredFood)
    {
        bool hasEnough = true;

        if (ResourceManager.Instance != null)
        {
            if (!ResourceManager.Instance.HasEnoughTroop(requiredTroop))
            {
                Debug.LogError($"❌ 兵力不足！需要 {requiredTroop}，当前 {ResourceManager.Instance.GetTroop()}");
                hasEnough = false;
            }

            if (!ResourceManager.Instance.HasEnoughFood(requiredFood))
            {
                Debug.LogError($"❌ 粮草不足！需要 {requiredFood}，当前 {ResourceManager.Instance.GetFood()}");
                hasEnough = false;
            }
        }

        if (hasEnough)
        {
            Debug.Log($"✅ 资源充足：兵力 {requiredTroop}+，粮草 {requiredFood}+");
        }

        return hasEnough;
    }

    // === 检查条件是否满足 ===
    public bool ValidateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return true; // 无条件限制
        }

        // 解析条件字符串
        // 格式示例: "troop>5", "food<3", "strategy>=2", "IF1_unlocked"

        if (condition.Contains("_unlocked"))
        {
            // 检查IF线是否解锁
            string ifLineId = condition.Replace("_unlocked", "");
            if (GameData.Instance != null)
            {
                bool isUnlocked = GameData.Instance.HasUnlockedIfLine(ifLineId);
                Debug.Log($"检查条件 {condition}: {(isUnlocked ? "✅ 已解锁" : "❌ 未解锁")}");
                return isUnlocked;
            }
        }

        if (condition.Contains(">="))
        {
            var parts = condition.Split(new string[] { ">=" }, System.StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string resource = parts[0].Trim();
                float required = float.Parse(parts[1].Trim());
                float current = GetResourceValue(resource);

                bool result = current >= required;
                Debug.Log($"检查 {resource}>={required}: 当前{current} {(result ? "✅ 满足" : "❌ 不满足")}");
                return result;
            }
        }
        else if (condition.Contains("<="))
        {
            var parts = condition.Split(new string[] { "<=" }, System.StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string resource = parts[0].Trim();
                float required = float.Parse(parts[1].Trim());
                float current = GetResourceValue(resource);

                bool result = current <= required;
                Debug.Log($"检查 {resource}<={required}: 当前{current} {(result ? "✅ 满足" : "❌ 不满足")}");
                return result;
            }
        }
        else if (condition.Contains(">"))
        {
            var parts = condition.Split('>');
            if (parts.Length == 2)
            {
                string resource = parts[0].Trim();
                float required = float.Parse(parts[1].Trim());
                float current = GetResourceValue(resource);

                bool result = current > required;
                Debug.Log($"检查 {resource}>{required}: 当前{current} {(result ? "✅ 满足" : "❌ 不满足")}");
                return result;
            }
        }
        else if (condition.Contains("<"))
        {
            var parts = condition.Split('<');
            if (parts.Length == 2)
            {
                string resource = parts[0].Trim();
                float required = float.Parse(parts[1].Trim());
                float current = GetResourceValue(resource);

                bool result = current < required;
                Debug.Log($"检查 {resource}<{required}: 当前{current} {(result ? "✅ 满足" : "❌ 不满足")}");
                return result;
            }
        }

        Debug.LogWarning($"无法识别的条件格式: {condition}");
        return false;
    }

    // 获取资源值
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

    // === 测试函数 ===
    public void RunAllTests()
    {
        Debug.Log("=== 数据验证系统测试 ===");

        // 测试1：输入值验证
        TestInputValidation();

        // 测试2：资源充足性验证
        TestResourceValidation();

        // 测试3：条件验证
        TestConditionValidation();
    }

    void TestInputValidation()
    {
        Debug.Log("--- 测试输入值验证 ---");
        ValidateInputValue(2.5f, 1.0f, 5.0f, "兵力调整");
        ValidateInputValue(10.0f, 1.0f, 5.0f, "异常兵力");
        ValidateInputValue(0.5f, 1.0f, 5.0f, "过低兵力");
    }

    void TestResourceValidation()
    {
        Debug.Log("--- 测试资源充足性 ---");
        ValidateResourceSufficiency(3.0f, 2.0f);
        ValidateResourceSufficiency(20.0f, 15.0f); // 应该失败
    }

    void TestConditionValidation()
    {
        Debug.Log("--- 测试条件验证 ---");

        // 先设置一些资源值
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyTroop(8.0f);
            ResourceManager.Instance.ModifyFood(4.0f);
        }

        // 测试数值条件
        ValidateCondition("troop>5");
        ValidateCondition("food<3");
        ValidateCondition("strategy>10");

        // 测试IF线条件
        if (GameData.Instance != null)
        {
            GameData.Instance.UnlockIfLine("IF1");
            ValidateCondition("IF1_unlocked");
            ValidateCondition("IF2_unlocked");
        }
    }
}