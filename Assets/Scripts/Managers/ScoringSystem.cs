using UnityEngine;
using System.Collections.Generic;

public class ScoringSystem : MonoBehaviour
{
    public static ScoringSystem Instance { get; private set; }

    // 评分权重配置
    [Header("评分权重")]
    public float troopWeight = 10.0f;      // 兵力权重
    public float foodWeight = 8.0f;        // 粮草权重  
    public float strategyWeight = 15.0f;   // 计策权重
    public float riskWeight = -5.0f;       // 风险权重（负分）
    public float ifLineBonus = 50.0f;      // IF线解锁奖励

    [Header("评分等级")]
    public float sScore = 900;    // S级
    public float aScore = 800;    // A级
    public float bScore = 700;    // B级
    public float cScore = 600;    // C级

    // 各结局的历史最高分
    private Dictionary<string, float> highScores = new Dictionary<string, float>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ScoringSystem初始化完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === 计算当前游戏的总分 ===
    public float CalculateTotalScore()
    {
        if (ResourceManager.Instance == null || GameData.Instance == null)
        {
            Debug.LogError("缺少必要的管理器");
            return 0;
        }

        float score = 0;

        // 1. 资源分
        score += ResourceManager.Instance.GetTroop() * troopWeight;
        score += ResourceManager.Instance.GetFood() * foodWeight;
        score += ResourceManager.Instance.GetStrategy() * strategyWeight;
        score += ResourceManager.Instance.GetRisk() * riskWeight; // 风险是负分

        // 2. IF线解锁奖励
        int unlockedIfLines = GameData.Instance.unlockedIfLines.Count;
        score += unlockedIfLines * ifLineBonus;

        // 3. 游戏完成度奖励（完成的剧情点越多分越高）
        if (GameData.Instance.currentDialogueNodeId > 1000)
        {
            float progressBonus = (GameData.Instance.currentDialogueNodeId - 1000) / 10.0f * 20.0f;
            score += progressBonus;
        }

        // 4. 确保最低分
        score = Mathf.Max(score, 0);

        Debug.Log($"📊 当前游戏评分: {score:F0}分");
        Debug.Log($"  资源分: {ResourceManager.Instance.GetTroop() * troopWeight + ResourceManager.Instance.GetFood() * foodWeight + ResourceManager.Instance.GetStrategy() * strategyWeight:F0}");
        Debug.Log($"  IF线奖励: +{unlockedIfLines * ifLineBonus} ({unlockedIfLines}条)");

        return score;
    }

    // === 计算结局评分（基于总分+结局类型） ===
    public float CalculateEndingScore(string endingId, float baseScore)
    {
        float endingMultiplier = GetEndingMultiplier(endingId);
        float finalScore = baseScore * endingMultiplier;

        Debug.Log($"🎯 结局评分: {baseScore:F0} × {endingMultiplier:F2} = {finalScore:F0}分");

        // 保存最高分
        SaveHighScore(endingId, finalScore);

        return finalScore;
    }

    // 不同结局的分数倍率
    float GetEndingMultiplier(string endingId)
    {
        switch (endingId.ToUpper())
        {
            case "IF5": // 完美结局
                return 1.5f;
            case "HISTORICAL": // 史实结局
                return 1.2f;
            case "IF1": // 半途而废
            case "IF2": // 错失良机
                return 0.7f;
            case "IF6": // 惨败结局
                return 0.5f;
            default:
                return 1.0f;
        }
    }

    // === 获取评分等级 ===
    public string GetScoreRank(float score)
    {
        if (score >= sScore) return "S";
        if (score >= aScore) return "A";
        if (score >= bScore) return "B";
        if (score >= cScore) return "C";
        return "D";
    }

    // === 保存最高分 ===
    void SaveHighScore(string endingId, float score)
    {
        string key = $"HighScore_{endingId}";

        if (!highScores.ContainsKey(endingId))
        {
            highScores[endingId] = score;
        }
        else if (score > highScores[endingId])
        {
            highScores[endingId] = score;
            Debug.Log($"🎖️ 刷新纪录！{endingId} 最高分: {score:F0}");
        }

        // 保存到PlayerPrefs（实际游戏中用）
        PlayerPrefs.SetFloat(key, score);
        PlayerPrefs.Save();
    }

    // === 显示评分报告 ===
    public void ShowScoreReport(string endingId)
    {
        float totalScore = CalculateTotalScore();
        float endingScore = CalculateEndingScore(endingId, totalScore);
        string rank = GetScoreRank(endingScore);

        Debug.Log("═══════════════════════════════════");
        Debug.Log("           评分报告");
        Debug.Log("═══════════════════════════════════");
        Debug.Log($"结局: {endingId}");
        Debug.Log($"总分: {endingScore:F0}");
        Debug.Log($"评级: {rank}");
        Debug.Log($"兵力: {ResourceManager.Instance.GetTroop():F1} × {troopWeight} = {ResourceManager.Instance.GetTroop() * troopWeight:F0}");
        Debug.Log($"粮草: {ResourceManager.Instance.GetFood():F1} × {foodWeight} = {ResourceManager.Instance.GetFood() * foodWeight:F0}");
        Debug.Log($"计策: {ResourceManager.Instance.GetStrategy():F1} × {strategyWeight} = {ResourceManager.Instance.GetStrategy() * strategyWeight:F0}");
        Debug.Log($"风险: {ResourceManager.Instance.GetRisk():F1} × {riskWeight} = {ResourceManager.Instance.GetRisk() * riskWeight:F0}");
        Debug.Log($"IF线: {GameData.Instance.unlockedIfLines.Count}条 × {ifLineBonus} = {GameData.Instance.unlockedIfLines.Count * ifLineBonus:F0}");
        Debug.Log("═══════════════════════════════════");
    }

    // === 测试函数 ===
    public void TestScoring()
    {
        Debug.Log("=== 评分系统测试 ===");

        // 设置测试数据
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ModifyTroop(15.0f);
            ResourceManager.Instance.ModifyFood(12.0f);
            ResourceManager.Instance.ModifyStrategy(8.0f);
            ResourceManager.Instance.ModifyRisk(3.0f);
        }

        if (GameData.Instance != null)
        {
            GameData.Instance.UnlockIfLine("IF1");
            GameData.Instance.UnlockIfLine("IF3");
        }

        // 测试评分
        ShowScoreReport("IF5");
    }
}