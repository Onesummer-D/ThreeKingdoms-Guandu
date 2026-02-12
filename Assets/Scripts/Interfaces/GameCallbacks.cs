using UnityEngine;
using System;

// 这是你和同学B实际交互的桥梁
public class GameCallbacks : MonoBehaviour
{
    public static GameCallbacks Instance { get; private set; }

    public event Action<bool> OnPuzzleGameCompletedEvent;
    public event Action<int> OnGridGameCompletedEvent;
    public event Action<float> OnValueInputConfirmedEvent;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameCallbacks初始化");
        }
    }

    // === 小游戏完成回调 ===
    // 同学B的拼图游戏完成后调用这个
    public void OnPuzzleGameCompleted(bool success)
    {
        Debug.Log($"🎮 拼图游戏完成: {(success ? "成功" : "失败")}");

        OnPuzzleGameCompletedEvent?.Invoke(success);
    }

    // 走格子游戏完成
    public void OnGridGameCompleted(int successLevel)
    {
        Debug.Log($"🎮 走格子完成: 等级{successLevel}");

        OnGridGameCompletedEvent?.Invoke(successLevel);
    }

    // === UI数值输入回调 ===
    // 同学B的数值输入UI完成后调用
    public void OnValueInputConfirmed(float value)
    {
        Debug.Log($"📊 数值输入确认: {value}");

        OnValueInputConfirmedEvent?.Invoke(value);

    }

    // === 测试接口 ===
    public void TestAllCallbacks()
    {
        Debug.Log("=== 测试所有回调接口 ===");

        OnPuzzleGameCompleted(true);
        OnGridGameCompleted(2);
        OnValueInputConfirmed(2.5f);
    }
}