using System;
using System.Collections.Generic;

public sealed class DecisionTendencyResult
{
    public string label;
    public string summary;
    public List<string> evidence = new List<string>();
}

public static class BattleReportAnalyzer
{
    private static readonly string[] TendencyOrder = { "稳慎", "奇谋", "果决", "纳谏" };

    public static DecisionTendencyResult Analyze(RunHistoryData history)
    {
        Dictionary<string, int> scores = new Dictionary<string, int>
        {
            { "稳慎", 0 }, { "奇谋", 0 }, { "果决", 0 }, { "纳谏", 0 }
        };
        Dictionary<string, List<string>> evidence = new Dictionary<string, List<string>>
        {
            { "稳慎", new List<string>() }, { "奇谋", new List<string>() },
            { "果决", new List<string>() }, { "纳谏", new List<string>() }
        };

        if (history != null && history.decisions != null)
        {
            for (int i = 0; i < history.decisions.Count; i++)
            {
                DecisionRecordData decision = history.decisions[i];
                if (decision == null || string.IsNullOrWhiteSpace(decision.optionText)) continue;
                foreach (string tendency in Classify(decision))
                {
                    scores[tendency]++;
                    if (evidence[tendency].Count < 3)
                        evidence[tendency].Add("第" + (decision.chapterIndex + 1) + "幕 · " + Compact(decision.optionText, 18));
                }
            }
        }

        string best = "";
        int bestScore = 0;
        for (int i = 0; i < TendencyOrder.Length; i++)
        {
            string candidate = TendencyOrder[i];
            if (scores[candidate] > bestScore)
            {
                best = candidate;
                bestScore = scores[candidate];
            }
        }

        if (bestScore == 0)
        {
            return new DecisionTendencyResult
            {
                label = "尚待观察",
                summary = "本局尚无足够的实际选择，无法归纳稳定倾向。"
            };
        }

        DecisionTendencyResult result = new DecisionTendencyResult
        {
            label = best,
            summary = GetSummary(best)
        };
        result.evidence.AddRange(evidence[best]);
        return result;
    }

    private static IEnumerable<string> Classify(DecisionRecordData decision)
    {
        string text = decision.optionText ?? "";
        HashSet<string> result = new HashSet<string>();
        if (ContainsAny(text, "坚守", "回守", "暂回", "撤退", "试探", "保全")) result.Add("稳慎");
        if (ContainsAny(text, "地道", "埋伏", "奇袭", "乌巢", "直取")) result.Add("奇谋");
        if (ContainsAny(text, "亲自", "全力", "直取", "斩首", "以牙还牙")) result.Add("果决");
        if (ContainsAny(text, "采纳", "坦诚", "推心置腹", "徐晃", "张辽", "遣将")) result.Add("纳谏");

        // Stable anchor fallbacks keep the explanation deterministic even if
        // copy is later shortened without changing the underlying choice.
        if (result.Count == 0)
        {
            if (decision.nodeId == 1001) result.Add(decision.optionIndex == 0 ? "果决" : "奇谋");
            else if (decision.nodeId == 2001) result.Add("稳慎");
            else if (decision.nodeId == 3001) result.Add(decision.optionIndex == 0 ? "纳谏" : decision.optionIndex == 1 ? "稳慎" : "果决");
            else if (decision.nodeId == 4001) result.Add(decision.optionIndex == 0 ? "果决" : "纳谏");
            else if (decision.nodeId == 5001) result.Add(decision.optionIndex == 1 ? "稳慎" : "奇谋");
        }
        return result;
    }

    private static string GetSummary(string tendency)
    {
        switch (tendency)
        {
            case "稳慎": return "你更常先守住兵力、粮道与退路，再等待下一次出手机会。";
            case "奇谋": return "你更愿意改变常规交锋方式，用路线、情报与出其不意寻找破口。";
            case "果决": return "你在关键时刻偏向迅速集中力量，把窗口直接转化为行动。";
            case "纳谏": return "你多次把他人的情报与能力纳入判断，善于借人之长完成部署。";
            default: return "本局选择形成了独特但尚不稳定的决策轨迹。";
        }
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
            if (value.IndexOf(needles[i], StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    private static string Compact(string value, int maxCharacters)
    {
        string line = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return line.Length <= maxCharacters ? line : line.Substring(0, maxCharacters - 1) + "…";
    }
}
