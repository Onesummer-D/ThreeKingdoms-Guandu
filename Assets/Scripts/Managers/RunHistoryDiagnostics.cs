using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RunHistoryDiagnosticResult
{
    public bool valid;
    public string message = string.Empty;
    public int eventCount;
    public int decisionCount;
    public int miniGameCount;
    public bool hasEnding;
}

/// <summary>
/// Small, engine-side validation surface for exported run records. It keeps
/// the replay and server payload checks independent from the presentation UI.
/// </summary>
public static class RunHistoryDiagnostics
{
    public static string ExportJson(RunHistoryData history)
    {
        return JsonUtility.ToJson(history ?? new RunHistoryData(), true);
    }

    public static bool TryValidate(RunHistoryData history, out string message)
    {
        RunHistoryDiagnosticResult result = Validate(history);
        message = result.message;
        return result.valid;
    }

    public static RunHistoryDiagnosticResult Validate(RunHistoryData history)
    {
        RunHistoryDiagnosticResult result = new RunHistoryDiagnosticResult
        {
            valid = true,
            message = "事件记录有效"
        };
        if (history == null)
            return Invalid(result, "记录为空");

        result.eventCount = history.events != null ? history.events.Count : 0;
        result.decisionCount = history.decisions != null ? history.decisions.Count : 0;
        result.miniGameCount = history.miniGames != null ? history.miniGames.Count : 0;
        result.hasEnding = history.ending != null && history.ending.nodeId > 0;

        if (history.schemaVersion <= 0)
            return Invalid(result, "缺少记录版本");
        if (string.IsNullOrEmpty(history.runId))
            return Invalid(result, "缺少 runId");
        if (string.IsNullOrEmpty(history.seed))
            return Invalid(result, "缺少 seed");
        if (history.events == null || history.events.Count == 0)
            return Invalid(result, "事件流为空");

        for (int i = 0; i < history.events.Count; i++)
        {
            RunEventData item = history.events[i];
            if (item == null)
                return Invalid(result, "事件为空：index=" + i);
            if (item.sequence != i + 1)
                return Invalid(result, "事件序号不连续：index=" + i);
            if (string.IsNullOrEmpty(item.eventType))
                return Invalid(result, "事件类型为空：sequence=" + item.sequence);
            if (!string.Equals(item.runId, history.runId, StringComparison.Ordinal))
                return Invalid(result, "事件 runId 不匹配：sequence=" + item.sequence);
            if (item.schemaVersion <= 0 || item.schemaVersion > RunEventSchema.CurrentVersion)
                return Invalid(result, "事件版本不支持：sequence=" + item.sequence);
        }

        return result;
    }

    public static string BuildSummary(RunHistoryData history)
    {
        RunHistoryDiagnosticResult result = Validate(history);
        return (result.valid ? "PASS" : "FAIL") +
            " events=" + result.eventCount +
            " decisions=" + result.decisionCount +
            " miniGames=" + result.miniGameCount +
            " ending=" + result.hasEnding +
            " message=" + result.message;
    }

    private static RunHistoryDiagnosticResult Invalid(RunHistoryDiagnosticResult result, string message)
    {
        result.valid = false;
        result.message = message;
        return result;
    }
}
