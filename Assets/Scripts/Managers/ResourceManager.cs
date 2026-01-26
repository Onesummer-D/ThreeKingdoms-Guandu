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
    // 单例模式
    public static ResourceManager Instance { get; private set; }

    // 当前资源值
    private float troopStrength = 10.0f;
    private float foodSupply = 10.0f;
    private float strategyChance = 5.0f;
    private float riskLevel = 0.0f;

    // 资源上限
    public const float MAX_TROOP = 20.0f;
    public const float MAX_FOOD = 20.0f;
    public const float MAX_STRATEGY = 10.0f;
    public const float MAX_RISK = 10.0f;

    // 事件系统：当资源变化时触发
    public static event Action<ResourceChangeEvent> OnResourceChanged;

    void Awake()
    {
        // 单例模式
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

    // === 获取资源值的方法 ===
    public float GetTroop() { return troopStrength; }
    public float GetFood() { return foodSupply; }
    public float GetStrategy() { return strategyChance; }
    public float GetRisk() { return riskLevel; }

    // === 修改资源值的方法（核心） ===
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

        // 触发资源变化事件
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

    // === 便捷方法 ===
    public void ModifyTroop(float amount) { ModifyResource(ResourceType.Troop, amount); }
    public void ModifyFood(float amount) { ModifyResource(ResourceType.Food, amount); }
    public void ModifyStrategy(float amount) { ModifyResource(ResourceType.Strategy, amount); }
    public void ModifyRisk(float amount) { ModifyResource(ResourceType.Risk, amount); }

    // === 检查资源是否足够 ===
    public bool HasEnoughTroop(float required) { return troopStrength >= required; }
    public bool HasEnoughFood(float required) { return foodSupply >= required; }

    // === 获取资源状态文本 ===
    public string GetResourcesText()
    {
        return $"兵力: {troopStrength:F1}/{MAX_TROOP}\n" +
               $"粮草: {foodSupply:F1}/{MAX_FOOD}\n" +
               $"计策: {strategyChance:F1}/{MAX_STRATEGY}\n" +
               $"风险: {riskLevel:F1}/{MAX_RISK}";
    }

    // === 重置所有资源 ===
    public void ResetAllResources()
    {
        troopStrength = 10.0f;
        foodSupply = 10.0f;
        strategyChance = 5.0f;
        riskLevel = 0.0f;
        Debug.Log("所有资源已重置");
    }
}