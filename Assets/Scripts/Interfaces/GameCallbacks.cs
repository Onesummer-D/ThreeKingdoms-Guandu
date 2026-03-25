using UnityEngine;
using System;

public class GameCallbacks : MonoBehaviour
{
    public static GameCallbacks Instance { get; private set; }

    public event Action<bool> OnPuzzleGameCompletedEvent;
    public event Action<bool> OnGridGameCompletedEvent;  // 只保留这一个！
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

    // 拼图游戏完成
    public void OnPuzzleGameCompleted(bool success)
    {
        Debug.Log($"🎮 拼图游戏完成: {(success ? "成功" : "失败")}");
        OnPuzzleGameCompletedEvent?.Invoke(success);
    }

    // 走格子游戏完成
    public void OnGridGameCompleted(bool isWin)
    {
        Debug.Log($"🎮 走格子游戏完成: {(isWin ? "胜利" : "失败")}");
        OnGridGameCompletedEvent?.Invoke(isWin);
    }

    // UI数值输入回调
    public void OnValueInputConfirmed(float value)
    {
        Debug.Log($"📊 数值输入确认: {value}");
        OnValueInputConfirmedEvent?.Invoke(value);
    }

    // 测试接口
    public void TestAllCallbacks()
    {
        Debug.Log("=== 测试所有回调接口 ===");
        OnPuzzleGameCompleted(true);
        OnGridGameCompleted(true);
        OnValueInputConfirmed(2.5f);
    }
}