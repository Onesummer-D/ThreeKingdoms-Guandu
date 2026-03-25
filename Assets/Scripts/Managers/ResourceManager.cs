using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 资源类型枚举
public enum ResourceType
{
    Troop,      // 兵力
    Food,       // 粮草
    Strategy,   // 计策成功率
    Risk        // 风险
}

// 资源变化事件的数据
public class ResourceChangeEvent
{
    public ResourceType resourceType;  // 哪种资源
    public float oldValue;             // 旧值
    public float newValue;             // 新值
    public float changeAmount;         // 变化量
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // ========== 修改：初始值 ==========
    private float troopStrength = 50.0f;      // 兵力初始50
    private float foodSupply = 40.0f;         // 粮草初始40
    private float strategyChance = 50.0f;     // 计策成功率初始50
    private float riskLevel = 50.0f;          // 风险初始50

    // ========== 修改：上限为100 ==========
    public const float MAX_TROOP = 100.0f;
    public const float MAX_FOOD = 100.0f;
    public const float MAX_STRATEGY = 100.0f;
    public const float MAX_RISK = 100.0f;

    // 事件系统
    public static event Action<ResourceChangeEvent> OnResourceChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ResourceManager初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 获取资源值
    public float GetTroop() { return troopStrength; }
    public float GetFood() { return foodSupply; }
    public float GetStrategy() { return strategyChance; }
    public float GetRisk() { return riskLevel; }

    // 修改资源值（核心）
    public void ModifyResource(ResourceType type, float amount)
    {
        float oldValue = 0;
        float newValue = 0;

        switch (type)
        {
            case ResourceType.Troop:
                oldValue = troopStrength;
                troopStrength = Mathf.Clamp(troopStrength + amount, 0, MAX_TROOP);
                newValue = troopStrength;
                break;

            case ResourceType.Food:
                oldValue = foodSupply;
                foodSupply = Mathf.Clamp(foodSupply + amount, 0, MAX_FOOD);
                newValue = foodSupply;
                break;

            case ResourceType.Strategy:
                oldValue = strategyChance;
                strategyChance = Mathf.Clamp(strategyChance + amount, 0, MAX_STRATEGY);
                newValue = strategyChance;
                break;

            case ResourceType.Risk:
                oldValue = riskLevel;
                riskLevel = Mathf.Clamp(riskLevel + amount, 0, MAX_RISK);
                newValue = riskLevel;
                break;
        }

        // 触发事件
        if (OnResourceChanged != null)
        {
            ResourceChangeEvent changeEvent = new ResourceChangeEvent
            {
                resourceType = type,
                oldValue = oldValue,
                newValue = newValue,
                changeAmount = amount
            };
            OnResourceChanged.Invoke(changeEvent);
        }

        Debug.Log($"{type} 变化: {amount:F1}, 当前: {newValue:F1}");
    }

    // 便捷方法
    public void ModifyTroop(float amount) { ModifyResource(ResourceType.Troop, amount); }
    public void ModifyFood(float amount) { ModifyResource(ResourceType.Food, amount); }
    public void ModifyStrategy(float amount) { ModifyResource(ResourceType.Strategy, amount); }
    public void ModifyRisk(float amount) { ModifyResource(ResourceType.Risk, amount); }

    // 检查资源
    public bool HasEnoughTroop(float required) { return troopStrength >= required; }
    public bool HasEnoughFood(float required) { return foodSupply >= required; }

    // 获取资源状态文本
    public string GetResourcesText()
    {
        return $"兵力: {troopStrength:F0}/{MAX_TROOP:F0}\n" +
               $"粮草: {foodSupply:F0}/{MAX_FOOD:F0}\n" +
               $"计策: {strategyChance:F0}/{MAX_STRATEGY:F0}\n" +
               $"风险: {riskLevel:F0}/{MAX_RISK:F0}";
    }

    // ========== 修改：重置方法 ==========
    public void ResetAllResources()
    {
        troopStrength = 50.0f;
        foodSupply = 40.0f;
        strategyChance = 50.0f;
        riskLevel = 50.0f;
        Debug.Log("所有资源已重置为初始值");
    }
}