using System;
using System.Collections.Generic;

/// <summary>
/// Evidence-backed run-specific profile, not a personality assessment.
/// Strategic intent and counsel behavior are independent dimensions.
/// </summary>
public sealed class DecisionTendencyResult
{
    public string label;
    public string summary;
    public string consultationLabel;
    public string consultationSummary;
    public int classifiedChoices;
    public List<string> evidence = new List<string>();
    public List<string> outcomeEvidence = new List<string>();
}

public static class BattleReportAnalyzer
{
    private static readonly string[] StyleOrder = { "稳慎", "奇谋", "果决" };

    public static DecisionTendencyResult Analyze(RunHistoryData history)
    {
        DecisionTendencyResult report = new DecisionTendencyResult
        {
            label = "尚待观察",
            summary = "本局尚无足够的关键战术选择，不作固定用兵风格判断。",
            consultationLabel = "尚无军议记录",
            consultationSummary = "本局没有可核验的谋议审核记录，不据此评判主将是否善纳人言。"
        };
        Dictionary<string, int> scores = new Dictionary<string, int>();
        foreach (string name in StyleOrder) scores[name] = 0;
        Dictionary<string, List<string>> evidenceByStyle = new Dictionary<string, List<string>>();
        foreach (string name in StyleOrder) evidenceByStyle[name] = new List<string>();

        List<DecisionRecordData> choices =
            history != null && history.decisions != null ? history.decisions : new List<DecisionRecordData>();
        int narrativeAccepted = 0;
        int narrativeReviewed = 0;
        for (int i = 0; i < choices.Count; i++)
        {
            DecisionRecordData choice = choices[i];
            if (choice == null || !DecisionBehaviorCatalog.TryFind(choice.nodeId, choice.optionIndex,
                out DecisionBehaviorCatalog.Entry tagged))
                continue;

            // A script rewrite can reorder routes without changing the
            // visible label. Refuse incompatible recorded destinations rather
            // than silently assigning the wrong behavior.
            if (choice.nextNodeId > 0 && tagged.nextNodeId != choice.nextNodeId)
                continue;
            if (tagged.counsel == "adopt") narrativeAccepted++;
            if (tagged.counsel == "review") narrativeReviewed++;
            if (string.IsNullOrEmpty(tagged.strategy) || !scores.ContainsKey(tagged.strategy)) continue;
            report.classifiedChoices++;
            scores[tagged.strategy]++;
            evidenceByStyle[tagged.strategy].Add(FormatChoiceEvidence(choice, tagged.action));
        }

        // A single plot choice cannot establish a stable strategic tendency.
        if (report.classifiedChoices >= 2)
        {
            int best = 0, winners = 0;
            string bestStyle = "";
            foreach (string name in StyleOrder)
            {
                if (scores[name] > best)
                {
                    best = scores[name];
                    bestStyle = name;
                    winners = 1;
                }
                else if (scores[name] == best && best > 0) winners++;
            }
            report.label = winners > 1 ? "兼采并用" : bestStyle;
            report.summary = StyleSummary(report.label);
            if (winners == 1)
                AddLast(report.evidence, evidenceByStyle[bestStyle], 2);
            else
            {
                // Include evidence from the tied styles, not an arbitrary
                // tie-breaking order that pretends a dominant preference.
                foreach (string name in StyleOrder)
                {
                    if (scores[name] == best && report.evidence.Count < 2 &&
                        evidenceByStyle[name].Count > 0)
                        report.evidence.Add(evidenceByStyle[name][evidenceByStyle[name].Count - 1]);
                }
            }
        }
        else if (report.classifiedChoices == 1)
        {
            foreach (string name in StyleOrder)
                if (scores[name] > 0) AddLast(report.evidence, evidenceByStyle[name], 1);
        }

        AnalyzeCounsel(history, choices, narrativeAccepted, narrativeReviewed, report);
        AppendOutcomeEvidence(history, report);
        return report;
    }

    private static void AnalyzeCounsel(RunHistoryData history, List<DecisionRecordData> choices,
        int narrativeAccepted, int narrativeReviewed, DecisionTendencyResult report)
    {
        List<AdvisorEchoData> advisors = history != null ? history.advisorEchoes : null;
        if (advisors != null && advisors.Count > 0)
        {
            int implemented = 0;
            int decidedIndependently = 0;
            string implementedEvidence = "";
            string independentEvidence = "";
            string pendingEvidence = "";
            for (int i = 0; i < advisors.Count; i++)
            {
                AdvisorEchoData advisor = advisors[i];
                if (advisor == null) continue;
                DecisionRecordData acted = null;
                for (int j = 0; j < choices.Count; j++)
                {
                    if (choices[j] != null && choices[j].nodeId == advisor.nodeId)
                    {
                        acted = choices[j];
                        break;
                    }
                }

                string who = string.IsNullOrWhiteSpace(advisor.guestLabel) ? "参谋" :
                    "参谋「" + Compact(advisor.guestLabel, 12) + "」";
                if (acted == null)
                {
                    pendingEvidence = who + "提出建议，尚无该节点的正式决策记录";
                    continue;
                }
                if (advisor.accepted && acted.optionIndex == advisor.optionIndex)
                {
                    implemented++;
                    implementedEvidence = who + "的建议经认可并落实为实际选择";
                }
                else
                {
                    decidedIndependently++;
                    independentEvidence = !advisor.accepted && acted.optionIndex == advisor.optionIndex
                        ? who + "的建议未获认可，但后续选择恰与建议相同"
                        : who + "参与军议，主将最终另作决断";
                }
            }
            if (implemented > 0)
            {
                report.consultationLabel = "纳谏践行";
                report.consultationSummary = "有参谋意见经主将认可，并与本局实际选择相符。";
            }
            else if (decidedIndependently > 0)
            {
                report.consultationLabel = "审议自决";
                report.consultationSummary = "主将听取了参谋建议，最终依自己的判断作出选择；这不是对决策好坏的评价。";
            }
            else
            {
                report.consultationLabel = "议而未决";
                report.consultationSummary = "参谋已提出建议，但尚无对应决策记录，不推断建议是否得到执行。";
            }
            string relevantEvidence = implemented > 0 ? implementedEvidence :
                decidedIndependently > 0 ? independentEvidence : pendingEvidence;
            if (!string.IsNullOrWhiteSpace(relevantEvidence)) report.evidence.Add(relevantEvidence);
            return;
        }

        // Historical in-game counsel is a separate, verifiable source when
        // no real guest has submitted a review. Never pretend a guest exists.
        if (narrativeAccepted > 0)
        {
            report.consultationLabel = "纳谏践行";
            report.consultationSummary = "主将已在剧情中采纳荀彧劝守的意见，并作出对应选择。";
            report.evidence.Add("第二幕 · 采纳荀彧守官渡之议");
        }
        else if (narrativeReviewed > 0)
        {
            report.consultationLabel = "审议自决";
            report.consultationSummary = "主将收到荀彧劝守的意见，最终选择了其它部署。";
            report.evidence.Add("第二幕 · 已收到荀彧书信，最终另择方案");
        }
    }

    private static string StyleSummary(string style)
    {
        switch (style)
        {
            case "稳慎": return "本局多次优先考虑守住阵地、保障退路与控制战局风险。";
            case "奇谋": return "本局多次选择器械、迂回或改变交锋方式，以寻找战局突破口。";
            case "果决": return "本局多次在关键时刻迅速主动部署，集中力量付诸行动。";
            case "兼采并用": return "本局采用过不同用兵方式，尚无单一风格明显占优。";
            default: return "本局尚无足够的关键战术选择，不作固定用兵风格判断。";
        }
    }

    private static string FormatChoiceEvidence(DecisionRecordData choice, string behavior)
    {
        string when = choice.chapterIndex >= 0
            ? "第" + (choice.chapterIndex + 1) + "幕" : "支线";
        string result = when + " · " + behavior;
        if (choice.before == null || choice.after == null) return result;
        List<string> changes = new List<string>();
        AddDelta(changes, "兵力", choice.after.troop - choice.before.troop);
        AddDelta(changes, "粮草", choice.after.food - choice.before.food);
        AddDelta(changes, "计策", choice.after.strategy - choice.before.strategy);
        AddDelta(changes, "风险", choice.after.risk - choice.before.risk);
        if (changes.Count > 0)
        {
            if (changes.Count > 2) changes.RemoveRange(2, changes.Count - 2);
            result += "（实际：" + string.Join("、", changes.ToArray()) + "）";
        }
        return result;
    }

    private static void AddDelta(List<string> list, string name, float amount)
    {
        if (Math.Abs(amount) < 0.5f) return;
        list.Add(name + (amount > 0 ? "+" : "") + Math.Round(amount).ToString("0"));
    }

    private static void AppendOutcomeEvidence(RunHistoryData history, DecisionTendencyResult report)
    {
        if (history == null) return;
        if (history.miniGames != null)
        {
            for (int i = history.miniGames.Count - 1; i >= 0; i--)
            {
                MiniGameTraceData game = history.miniGames[i];
                if (game == null || !game.completed) continue;
                report.outcomeEvidence.Add("小游戏「" +
                    FriendlyMiniGameName(game.gameType) + "」：" + (game.success ? "成功" : "失败"));
                break; // Most recent completed game, not an invented composite
            }
        }
        if (history.completed && history.ending != null &&
            !string.IsNullOrWhiteSpace(history.ending.endingName))
            report.outcomeEvidence.Add("本局结局：" + Compact(history.ending.endingName, 36));
    }

    private static string FriendlyMiniGameName(string gameType)
    {
        switch ((gameType ?? "").ToLowerInvariant())
        {
            case "puzzle": return "战术拼图";
            case "digtunnel": return "地道攻防";
            case "slider": return "伏击布阵";
            case "grid": return "夜袭行军";
            default: return "战术挑战";
        }
    }

    private static void AddLast(List<string> output, List<string> input, int maximum)
    {
        int start = Math.Max(0, input.Count - maximum);
        for (int i = start; i < input.Count; i++) output.Add(input[i]);
    }

    private static string Compact(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "未记录";
        string singleLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
        return singleLine.Length <= max ? singleLine : singleLine.Substring(0, max - 1) + "…";
    }
}
