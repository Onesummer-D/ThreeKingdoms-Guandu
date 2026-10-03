using System;
using System.Collections.Generic;

/// <summary>
/// Stable event names shared by runtime recording, battle replay and
/// diagnostics. Keep the wire names backward-compatible once exported.
/// </summary>
public static class RunEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string NodeShown = "NodeShown";
    public const string DecisionMade = "DecisionMade";
    public const string ResourceChanged = "ResourceChanged";
    public const string MiniGameStarted = "MiniGameStarted";
    public const string MiniGameAction = "MiniGameAction";
    public const string MiniGameCompleted = "MiniGameCompleted";
    public const string AdvisorSuggested = "AdvisorSuggested";
    public const string SceneStateChanged = "SceneStateChanged";
    public const string EndingReached = "EndingReached";
    public const string RunCompleted = "RunCompleted";
}

public static class RunEventSchema
{
    public const int CurrentVersion = 1;
}

[Serializable]
public sealed class RunEventData
{
    public int schemaVersion = RunEventSchema.CurrentVersion;
    public string runId = string.Empty;
    public int sequence;
    public float elapsedSeconds;
    public string eventType = string.Empty;
    public int nodeId;
    public int chapterIndex = -1;
    public string label = string.Empty;
    public string payload = string.Empty;

    // Resource fields are populated for ResourceChanged events. Keeping them
    // flat makes Unity JsonUtility output readable and migration-friendly.
    public ResourceType resourceType;
    public float oldValue;
    public float newValue;
    public float changeAmount;
    public bool success;
}

[Serializable]
public sealed class RunParticipantData
{
    public string participantId = string.Empty;
    public string role = string.Empty;
    public string displayName = string.Empty;
    public string joinedAtUtc = string.Empty;
}

[Serializable]
public sealed class MiniGameTraceData
{
    public int sequence;
    public string gameType = string.Empty;
    public int nodeId;
    public int nextNodeId;
    public int actionCount;
    public int failedAttempts;
    public bool completed;
    public bool success;
    public float startedElapsedSeconds;
    public float completedElapsedSeconds;
    public string actionSummary = string.Empty;
}

[Serializable]
public sealed class EndingAnalysisData
{
    public int nodeId;
    public string endingId = string.Empty;
    public string endingName = string.Empty;
    public string reason = string.Empty;
    public string historicalAnchor = string.Empty;
    public string historicalDeviation = string.Empty;
    public List<int> keyDecisionSequences = new List<int>();
}
