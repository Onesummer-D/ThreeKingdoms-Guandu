using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Runtime-only state for the offline advisor handoff. It intentionally has
/// no Unity, network, account or persistence dependency: the main route stays
/// authoritative and a suggestion can only become a reviewable echo.
/// </summary>
public enum InviteSessionStatus
{
    None,
    AwaitingGuest,
    GuestSubmitted,
    Accepted,
    Declined,
    Closed
}

[Serializable]
public sealed class InviteSuggestion
{
    public string guestLabel;
    public int optionIndex = -1;
    public string optionText;
    public string reason;
}

public sealed class InviteSessionState
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly List<string> optionTexts = new List<string>();

    public InviteSessionStatus Status { get; private set; }
    public string RunId { get; private set; }
    public string InviteCode { get; private set; }
    public int ChapterIndex { get; private set; }
    public int NodeId { get; private set; }
    public int StateVersion { get; private set; }
    public InviteSuggestion Suggestion { get; private set; }
    public string EchoSummary { get; private set; }
    public IReadOnlyList<string> OptionTexts => optionTexts;
    public bool IsActive => Status == InviteSessionStatus.AwaitingGuest ||
        Status == InviteSessionStatus.GuestSubmitted || Status == InviteSessionStatus.Accepted ||
        Status == InviteSessionStatus.Declined;
    public bool HasSuggestion => Suggestion != null && Status != InviteSessionStatus.None &&
        Status != InviteSessionStatus.Closed;

    public void Begin(InviteSnapshotData snapshot, IList<string> options)
    {
        Reset();
        if (snapshot == null) return;

        ChapterIndex = snapshot.ChapterIndex;
        NodeId = snapshot.CurrentNodeId;
        StateVersion = 1;
        RunId = "local-" + DateTime.UtcNow.Ticks.ToString("X");
        InviteCode = BuildInviteCode(snapshot.ChapterIndex, snapshot.CurrentNodeId,
            snapshot.SelectedRouteSummary, snapshot.Troop, snapshot.Food,
            snapshot.Strategy, snapshot.Risk);
        if (options != null)
            for (int i = 0; i < options.Count; i++)
                optionTexts.Add(options[i] ?? string.Empty);
        Status = InviteSessionStatus.AwaitingGuest;
    }

    public bool Matches(InviteSnapshotData snapshot)
    {
        return snapshot != null && IsActive && NodeId == snapshot.CurrentNodeId &&
            ChapterIndex == snapshot.ChapterIndex &&
            string.Equals(InviteCode, BuildInviteCode(snapshot.ChapterIndex, snapshot.CurrentNodeId,
                snapshot.SelectedRouteSummary, snapshot.Troop, snapshot.Food,
                snapshot.Strategy, snapshot.Risk), StringComparison.OrdinalIgnoreCase);
    }

    public bool ValidateCode(string code)
    {
        return IsActive && !string.IsNullOrWhiteSpace(InviteCode) &&
            string.Equals(Normalize(code), InviteCode, StringComparison.OrdinalIgnoreCase);
    }

    public bool SubmitSuggestion(string guestLabel, int optionIndex, string reason)
    {
        if (Status != InviteSessionStatus.AwaitingGuest || optionIndex < 0 ||
            optionIndex >= optionTexts.Count || string.IsNullOrWhiteSpace(guestLabel)) return false;
        reason = reason ?? string.Empty;

        Suggestion = new InviteSuggestion
        {
            guestLabel = guestLabel.Trim(),
            optionIndex = optionIndex,
            optionText = optionTexts[optionIndex],
            reason = reason.Trim()
        };
        StateVersion++;
        Status = InviteSessionStatus.GuestSubmitted;
        EchoSummary = string.Empty;
        return true;
    }

    public bool AcceptSuggestion()
    {
        if (Status != InviteSessionStatus.GuestSubmitted || Suggestion == null) return false;
        StateVersion++;
        Status = InviteSessionStatus.Accepted;
        EchoSummary = "参谋建议“" + Suggestion.optionText + "”" +
            (string.IsNullOrWhiteSpace(Suggestion.reason) ? "" : "，理由：“" + Suggestion.reason + "”") +
            "；主将已采纳，实际决策仍由主将亲自确认。";
        return true;
    }

    public bool DeclineSuggestion()
    {
        if (Status != InviteSessionStatus.GuestSubmitted || Suggestion == null) return false;
        StateVersion++;
        Status = InviteSessionStatus.Declined;
        EchoSummary = "参谋建议“" + Suggestion.optionText + "”未被采纳；主线仍等待主将决策。";
        return true;
    }

    public void Close()
    {
        if (Status != InviteSessionStatus.None) Status = InviteSessionStatus.Closed;
    }

    public void Reset()
    {
        Status = InviteSessionStatus.None;
        RunId = string.Empty;
        InviteCode = string.Empty;
        ChapterIndex = -1;
        NodeId = 0;
        StateVersion = 0;
        Suggestion = null;
        EchoSummary = string.Empty;
        optionTexts.Clear();
    }

    /// <summary>
    /// A deterministic local code for the current snapshot. It is not a
    /// credential and is intentionally labelled as a same-instance handoff
    /// in the UI rather than advertised as an online room code.
    /// </summary>
    public static string BuildInviteCode(int chapterIndex, int nodeId, string route,
        int troop, int food, int strategy, int risk)
    {
        string payload = chapterIndex + "|" + nodeId + "|" + (route ?? string.Empty) + "|" +
            troop + "|" + food + "|" + strategy + "|" + risk;
        uint hash = 2166136261u;
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        for (int i = 0; i < bytes.Length; i++)
        {
            hash ^= bytes[i];
            hash *= 16777619u;
        }

        char[] result = new char[6];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Alphabet[(int)(hash % (uint)Alphabet.Length)];
            hash = (hash >> 5) ^ (hash * 0x45d9f3bu);
        }
        return new string(result);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value.Trim().Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
    }
}
