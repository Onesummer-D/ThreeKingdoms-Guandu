using System;
using System.Collections.Generic;

[Serializable]
public sealed class ResourceSnapshotData
{
    public int sequence;
    public int nodeId;
    public string label;
    public float elapsedSeconds;
    public float troop;
    public float food;
    public float strategy;
    public float risk;
}

[Serializable]
public sealed class DecisionRecordData
{
    public int sequence;
    public int nodeId;
    public int chapterIndex;
    public int optionIndex;
    public string optionText;
    public int nextNodeId;
    public float troopEffect;
    public float foodEffect;
    public float strategyEffect;
    public float riskEffect;
    public ResourceSnapshotData before;
    public ResourceSnapshotData after;
}

[Serializable]
public sealed class AdvisorEchoData
{
    public int sequence;
    public int nodeId;
    public int chapterIndex;
    public int optionIndex;
    public string guestLabel;
    public string optionText;
    public string reason;
    public bool accepted;
    public string summary;
}

[Serializable]
public sealed class RunHistoryData
{
    public int schemaVersion = RunEventSchema.CurrentVersion;
    public string runId = string.Empty;
    public string seed = string.Empty;
    public string createdAtUtc = string.Empty;
    public float activeSeconds;
    public bool completed;
    public bool dataIntegrityValid = true;
    public string dataIntegrityNote = string.Empty;
    public List<RunParticipantData> participants = new List<RunParticipantData>();
    public List<RunEventData> events = new List<RunEventData>();
    public List<MiniGameTraceData> miniGames = new List<MiniGameTraceData>();
    public EndingAnalysisData ending = new EndingAnalysisData();
    public List<int> shownNodeOrder = new List<int>();
    public List<DecisionRecordData> decisions = new List<DecisionRecordData>();
    public List<ResourceSnapshotData> resourceTimeline = new List<ResourceSnapshotData>();
    public List<AdvisorEchoData> advisorEchoes = new List<AdvisorEchoData>();
}
