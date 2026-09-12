using System;
using System.Collections.Generic;

// Minimal snapshot contract used to exercise the runtime-only state machine
// without loading Unity or any scene asset.
public sealed class InviteSnapshotData
{
    public readonly int ChapterIndex;
    public readonly int CurrentNodeId;
    public readonly string SelectedRouteSummary;
    public readonly int Troop;
    public readonly int Food;
    public readonly int Strategy;
    public readonly int Risk;

    public InviteSnapshotData(int chapterIndex, int nodeId, string route,
        int troop, int food, int strategy, int risk)
    {
        ChapterIndex = chapterIndex;
        CurrentNodeId = nodeId;
        SelectedRouteSummary = route;
        Troop = troop;
        Food = food;
        Strategy = strategy;
        Risk = risk;
    }
}

public static class AdvisorStateChecks
{
    public static int Main()
    {
        InviteSnapshotData snapshot = new InviteSnapshotData(2, 3001, "第3幕：试探", 40, 35, 50, 50);
        InviteSessionState state = new InviteSessionState();
        state.Begin(snapshot, new List<string> { "坦诚文若之谋", "试探其意", "暂缓决断" });
        Assert(state.Status == InviteSessionStatus.AwaitingGuest, "begin status");
        Assert(state.ValidateCode(state.InviteCode), "valid code");
        Assert(!state.ValidateCode("BAD999"), "invalid code");
        Assert(state.SubmitSuggestion("文若", 1, "先试探再决定"), "submit suggestion");
        Assert(!state.SubmitSuggestion("重复", 0, "不应重复"), "duplicate submit blocked");
        Assert(state.AcceptSuggestion(), "accept suggestion");
        Assert(state.Status == InviteSessionStatus.Accepted && !string.IsNullOrWhiteSpace(state.EchoSummary), "accepted echo");
        state.Reset();
        state.Begin(snapshot, new List<string> { "坦诚文若之谋", "试探其意", "暂缓决断" });
        Assert(state.SubmitSuggestion("无理由", 0, null), "null reason allowed");
        Assert(string.IsNullOrEmpty(state.Suggestion.reason), "null reason normalized");
        Assert(state.AcceptSuggestion(), "accept without reason");
        Assert(state.EchoSummary.IndexOf("理由", StringComparison.Ordinal) < 0, "empty reason omitted from echo");
        state.Reset();
        Assert(state.Status == InviteSessionStatus.None && string.IsNullOrEmpty(state.InviteCode), "reset isolation");

        // Reuse the same runtime state machine at every real decision anchor.
        // The production anchors intentionally expose 3/2/3/2/3 choices; a
        // blank reason must remain valid for each shape, not just the fixture
        // above.
        int[] anchorIds = { 1001, 2001, 3001, 4001, 5001 };
        string[][] anchorOptions =
        {
            new[] { "挖掘地道", "发石车", "诈败" },
            new[] { "坚守官渡", "回许都" },
            new[] { "坦诚相告", "试探其意", "斩首许攸" },
            new[] { "亲自出征", "遣将迎敌" },
            new[] { "奇袭乌巢", "回守官渡", "直取袁绍" }
        };
        for (int i = 0; i < anchorIds.Length; i++)
        {
            state.Begin(new InviteSnapshotData(i + 1, anchorIds[i], "第" + (i + 1) + "幕", 40, 35, 50, 50),
                new List<string>(anchorOptions[i]));
            Assert(state.ValidateCode(state.InviteCode), "anchor " + anchorIds[i] + " code");
            Assert(state.SubmitSuggestion("参谋", 0, string.Empty), "anchor " + anchorIds[i] + " empty reason");
            Assert(state.AcceptSuggestion(), "anchor " + anchorIds[i] + " accept");
            state.Reset();
        }
        Console.WriteLine("PASS: advisor state lifecycle checks.");
        return 0;
    }

    private static void Assert(bool condition, string label)
    {
        if (!condition) throw new Exception("Advisor state check failed: " + label);
    }
}
