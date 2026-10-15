using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

// Minimum Unity dependencies. The actual catalog and analyzer C# files are
// compiled unchanged by the .NET test runner.
public enum ResourceType { Troop, Food, Strategy, Risk }
namespace UnityEngine
{
    public class TextAsset
    {
        public string text;
        public TextAsset(string textValue) => text = textValue;
    }
    public static class Resources
    {
        public static T Load<T>(string name) where T : class
        {
            string path = Path.Combine("Assets", "Resources", name + ".json");
            return File.Exists(path) ? new TextAsset(File.ReadAllText(path)) as T : null;
        }
    }
    public static class JsonUtility
    {
        public static T FromJson<T>(string text)
            => JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions { IncludeFields = true });
    }
    public static class Debug
    {
        public static void LogWarning(string message) => Console.Error.WriteLine(message);
    }
}

internal static class Program
{
    private static int passed;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL " + message);
        Console.WriteLine("PASS " + message);
        passed++;
    }
    static DecisionRecordData Decision(int node, int index, string text,
        float troopBefore=50, float troopAfter=50, float foodBefore=50, float foodAfter=50)
    {
        return new DecisionRecordData
        {
            nodeId=node,optionIndex=index,optionText=text,chapterIndex=node/1000-1,
            before=new ResourceSnapshotData { troop=troopBefore,food=foodBefore,strategy=50,risk=50 },
            after=new ResourceSnapshotData { troop=troopAfter,food=foodAfter,strategy=50,risk=50 }
        };
    }
    static void Main()
    {
        RunHistoryData empty=new RunHistoryData();
        DecisionTendencyResult zero=BattleReportAnalyzer.Analyze(empty);
        Check(zero.label=="尚待观察","No choices cannot generate a stable war style");
        Check(zero.consultationLabel=="尚无军议记录","No counsel record is not called disobedience");

        var single=new RunHistoryData();
        single.decisions.Add(Decision(1001,1,"挖掘地道",50,40,50,45));
        var one=BattleReportAnalyzer.Analyze(single);
        Check(one.label=="尚待观察","Only one strategic decision is insufficient");
        Check(one.evidence.Count==1&&one.evidence[0].Contains("兵力-10")&&
            one.evidence[0].Contains("粮草-5"),"Actual before/after deltas become factual evidence");

        var diversified=new RunHistoryData();
        diversified.decisions.Add(Decision(1001,1,"老文案：挖掘地道"));
        diversified.decisions.Add(Decision(4001,0,"原文：曹操亲自领军"));
        diversified.decisions.Add(Decision(5001,2,"原文：直取袁绍"));
        var baseResult=BattleReportAnalyzer.Analyze(diversified);
        Check(baseResult.label=="奇谋","Two unconventional actions outweigh one direct attack");

        var rewritten=new RunHistoryData();
        rewritten.decisions.Add(Decision(1001,1,"文案完全不同，舍掉所有旧关键词"));
        rewritten.decisions.Add(Decision(4001,0,"文案自由润色"));
        rewritten.decisions.Add(Decision(5001,2,"另一种完全改写"));
        var changed=BattleReportAnalyzer.Analyze(rewritten);
        Check(changed.label==baseResult.label && changed.classifiedChoices==baseResult.classifiedChoices,
            "Copywriter edits cannot change behavior classification");
        Check(!changed.evidence.Any(x=>x.Contains("文案完全不同")),
            "Behavioral evidence is mapped to actual actions, not text matching");

        var tie=new RunHistoryData();
        tie.decisions.Add(Decision(1001,2,"诱敌"));
        tie.decisions.Add(Decision(2001,1,"撤兵"));
        var equal=BattleReportAnalyzer.Analyze(tie);
        Check(equal.label=="兼采并用","Equal two styles get neutral hybrid rather than priority tie-break");

        var unknown=new RunHistoryData();
        unknown.decisions.Add(Decision(888888,0,"出现一个奇袭关键词也不能触发画像"));
        unknown.decisions.Add(Decision(999999,4,"果决"));
        var unknownResult=BattleReportAnalyzer.Analyze(unknown);
        Check(unknownResult.label=="尚待观察","Unknown option IDs fail closed instead of keyword fallback");

        var npc=new RunHistoryData();
        npc.decisions.Add(Decision(1001,1,"挖地道"));
        npc.decisions.Add(Decision(2001,0,"听取荀彧"));
        var npcResult=BattleReportAnalyzer.Analyze(npc);
        Check(npcResult.consultationLabel=="纳谏践行",
            "Actual scripted acceptance may establish NPC counsel behavior");

        var declined=new RunHistoryData();
        declined.decisions.Add(Decision(1001,1,"奇袭"));
        declined.decisions.Add(Decision(4001,0,"亲率"));
        declined.advisorEchoes.Add(new AdvisorEchoData {
            nodeId=4001,optionIndex=1,guestLabel="友人",accepted=false,
            optionText="让部将出征",reason="守住大营"
        });
        declined.completed=true;
        declined.ending=new EndingAnalysisData { endingName="以少胜多" };
        var free=BattleReportAnalyzer.Analyze(declined);
        Check(free.consultationLabel=="审议自决",
            "Declining a guest suggestion and winning is neutral independent deliberation");
        Check(!free.consultationSummary.Contains("固执")&&!free.consultationSummary.Contains("错误"),
            "No negative character judgement after rejecting advice");
        Check(free.outcomeEvidence.Any(x=>x.Contains("以少胜多")),
            "Actual ending may be displayed separately from counsel label");

        var accepted=new RunHistoryData();
        accepted.decisions.Add(Decision(4001,1,"遣将"));
        accepted.advisorEchoes.Add(new AdvisorEchoData {
            nodeId=4001,optionIndex=1,guestLabel="好友",accepted=true,
            optionText="遣将"
        });
        var followed=BattleReportAnalyzer.Analyze(accepted);
        Check(followed.consultationLabel=="纳谏践行",
            "Approved guest advice + exact node and selected index proves implementation");

        var diverged=new RunHistoryData();
        diverged.decisions.Add(Decision(4001,0,"亲率"));
        diverged.advisorEchoes.Add(new AdvisorEchoData {
            nodeId=4001,optionIndex=1,guestLabel="好友",accepted=true
        });
        Check(BattleReportAnalyzer.Analyze(diverged).consultationLabel=="审议自决",
            "Approving a proposal without following it cannot count as executed advice");

        var pending=new RunHistoryData();
        pending.advisorEchoes.Add(new AdvisorEchoData {
            nodeId=5001,optionIndex=0,guestLabel="军师",accepted=true
        });
        Check(BattleReportAnalyzer.Analyze(pending).consultationLabel=="议而未决",
            "Reviewed counsel before a decision cannot be treated as executed");

        var gameRun=new RunHistoryData();
        gameRun.decisions.Add(Decision(1001,0,"发石车"));
        gameRun.decisions.Add(Decision(5001,0,"强攻"));
        gameRun.miniGames.Add(new MiniGameTraceData {
            gameType="Grid",completed=true,success=false
        });
        var game=BattleReportAnalyzer.Analyze(gameRun);
        Check(game.outcomeEvidence.Any(x=>x.Contains("失败")),
            "Actual failed minigame is displayed as outcome evidence, not scored as personality");
        Check(game.consultationLabel=="尚无军议记录",
            "A minigame cannot fabricate consultation behavior");

        Console.WriteLine("decision-profile-regressions: "+passed+"/"+passed);
    }
}
