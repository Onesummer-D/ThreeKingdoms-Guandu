using System;

public static class BattleReportAnalyzerChecks
{
    public static int Main()
    {
        RunHistoryData history = new RunHistoryData();
        history.decisions.Add(new DecisionRecordData
        {
            nodeId = 1001,
            chapterIndex = 0,
            optionIndex = 1,
            optionText = "令将士挖掘地道，奇袭袁营"
        });
        DecisionTendencyResult result = BattleReportAnalyzer.Analyze(history);
        Assert(result.label == "奇谋", "Expected the tunnel choice to produce the 奇谋 tendency.");
        Assert(result.evidence.Count == 1, "Tendency evidence must include the real choice.");
        Assert(result.evidence[0].Contains("挖掘地道"), "Evidence text must preserve the selected option.");

        RunHistoryData empty = new RunHistoryData();
        DecisionTendencyResult emptyResult = BattleReportAnalyzer.Analyze(empty);
        Assert(emptyResult.label == "尚待观察", "Empty history must not fabricate a tendency.");
        Console.WriteLine("PASS: Battle report analyzer checks.");
        return 0;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
