using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RunHistoryTracker : MonoBehaviour
{
    public static RunHistoryTracker Instance { get; private set; }

    private RunHistoryData current = new RunHistoryData();
    private readonly List<ResourceChangeEvent> pendingChanges = new List<ResourceChangeEvent>();
    private Coroutine pendingFlush;
    private bool initialized;
    private bool runActive;

    private static readonly HashSet<int> TerminalNodes = new HashSet<int>
    {
        200314, 300416, 500217, 500313, 500320, 500411, 500418
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown += HandleNodeShown;
            DialogueSystem.Instance.OnOptionSelected += HandleOptionSelected;
        }
        ResourceManager.OnResourceChanged += HandleResourceChanged;
    }

    private void OnDestroy()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.OnDialogueNodeShown -= HandleNodeShown;
            DialogueSystem.Instance.OnOptionSelected -= HandleOptionSelected;
        }
        ResourceManager.OnResourceChanged -= HandleResourceChanged;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (runActive) current.activeSeconds += Time.unscaledDeltaTime;
    }

    public void BeginNewRun()
    {
        ClearPendingChanges();
        current = new RunHistoryData();
        runActive = true;
    }

    public void ResetRunState()
    {
        runActive = false;
        ClearPendingChanges();
        current = new RunHistoryData();
    }

    public RunHistoryData ExportHistory()
    {
        return CloneHistory(current);
    }

    public void RestoreHistory(RunHistoryData saved, bool shouldContinue)
    {
        ClearPendingChanges();
        current = saved != null ? CloneHistory(saved) : new RunHistoryData();
        runActive = shouldContinue && !current.completed;
    }

    /// <summary>
    /// Records one completed advisor attempt without touching the authoritative
    /// dialogue/resource path. Every accepted or declined attempt is kept,
    /// including two advisors who happen to choose the same option; the UI
    /// disables the completed attempt's buttons so a redraw cannot duplicate it.
    /// </summary>
    public void RecordAdvisorEcho(int nodeId, int chapterIndex, int optionIndex,
        string guestLabel, string optionText, string reason, bool accepted, string summary)
    {
        if (current.advisorEchoes == null) current.advisorEchoes = new List<AdvisorEchoData>();
        current.advisorEchoes.Add(new AdvisorEchoData
        {
            sequence = current.advisorEchoes.Count + 1,
            nodeId = nodeId,
            chapterIndex = chapterIndex,
            optionIndex = optionIndex,
            guestLabel = guestLabel,
            optionText = optionText,
            reason = reason,
            accepted = accepted,
            summary = summary
        });
    }

    private void HandleNodeShown(int nodeId)
    {
        if (!runActive && !TerminalNodes.Contains(nodeId)) return;

        if (current.shownNodeOrder.Count == 0 ||
            current.shownNodeOrder[current.shownNodeOrder.Count - 1] != nodeId)
            current.shownNodeOrder.Add(nodeId);

        if (current.resourceTimeline.Count == 0)
            AppendSnapshot(nodeId, "出征");

        if (TerminalNodes.Contains(nodeId))
        {
            AppendSnapshotIfChanged(nodeId, "抵达结局");
            current.completed = true;
            runActive = false;
        }
    }

    private void HandleResourceChanged(ResourceChangeEvent change)
    {
        if (!runActive || change == null) return;
        pendingChanges.Add(new ResourceChangeEvent
        {
            resourceType = change.resourceType,
            oldValue = change.oldValue,
            newValue = change.newValue,
            changeAmount = change.changeAmount
        });
        if (pendingFlush == null) pendingFlush = StartCoroutine(FlushAtEndOfFrame());
    }

    private void HandleOptionSelected(int optionIndex, DialogueOption option)
    {
        if (!runActive || option == null || DialogueSystem.Instance?.CurrentNode == null) return;
        int nodeId = DialogueSystem.Instance.CurrentNode.nodeId;
        if (current.decisions.Count > 0)
        {
            DecisionRecordData last = current.decisions[current.decisions.Count - 1];
            if (last.nodeId == nodeId && last.optionIndex == optionIndex) return;
        }

        ResourceSnapshotData after = CaptureSnapshot(nodeId, Compact(option.optionText, 18));
        ResourceSnapshotData before = CloneSnapshot(after);
        before.label = "选择前";
        for (int i = 0; i < pendingChanges.Count; i++)
        {
            ResourceChangeEvent change = pendingChanges[i];
            switch (change.resourceType)
            {
                case ResourceType.Troop: before.troop = change.oldValue; break;
                case ResourceType.Food: before.food = change.oldValue; break;
                case ResourceType.Strategy: before.strategy = change.oldValue; break;
                case ResourceType.Risk: before.risk = change.oldValue; break;
            }
        }

        current.decisions.Add(new DecisionRecordData
        {
            sequence = current.decisions.Count + 1,
            nodeId = nodeId,
            chapterIndex = CampaignChapterResolver.Resolve(nodeId),
            optionIndex = optionIndex,
            optionText = option.optionText,
            nextNodeId = option.nextNodeId,
            troopEffect = option.troopEffect,
            foodEffect = option.foodEffect,
            strategyEffect = option.strategyEffect,
            riskEffect = option.riskEffect,
            before = before,
            after = CloneSnapshot(after)
        });
        // Keep one x-axis point for every actual decision even when the choice
        // changes no resource, so the report never drops a visited decision.
        AppendSnapshot(nodeId, "决策 · " + Compact(option.optionText, 12));
        ClearPendingChanges();
    }

    private IEnumerator FlushAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        pendingFlush = null;
        if (pendingChanges.Count == 0 || !runActive) yield break;
        int nodeId = DialogueSystem.Instance?.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId
            : 0;
        AppendSnapshotIfChanged(nodeId, "战局变化");
        pendingChanges.Clear();
    }

    private void AppendSnapshotIfChanged(int nodeId, string label)
    {
        ResourceSnapshotData next = CaptureSnapshot(nodeId, label);
        if (current.resourceTimeline.Count > 0)
        {
            ResourceSnapshotData last = current.resourceTimeline[current.resourceTimeline.Count - 1];
            if (Mathf.Approximately(last.troop, next.troop) &&
                Mathf.Approximately(last.food, next.food) &&
                Mathf.Approximately(last.strategy, next.strategy) &&
                Mathf.Approximately(last.risk, next.risk))
                return;
        }
        next.sequence = current.resourceTimeline.Count + 1;
        current.resourceTimeline.Add(next);
    }

    private void AppendSnapshot(int nodeId, string label)
    {
        ResourceSnapshotData snapshot = CaptureSnapshot(nodeId, label);
        snapshot.sequence = current.resourceTimeline.Count + 1;
        current.resourceTimeline.Add(snapshot);
    }

    private ResourceSnapshotData CaptureSnapshot(int nodeId, string label)
    {
        ResourceManager resources = ResourceManager.Instance;
        return new ResourceSnapshotData
        {
            nodeId = nodeId,
            label = label,
            elapsedSeconds = current.activeSeconds,
            troop = resources != null ? resources.GetTroop() : 0f,
            food = resources != null ? resources.GetFood() : 0f,
            strategy = resources != null ? resources.GetStrategy() : 0f,
            risk = resources != null ? resources.GetRisk() : 0f
        };
    }

    private void ClearPendingChanges()
    {
        pendingChanges.Clear();
        if (pendingFlush != null)
        {
            StopCoroutine(pendingFlush);
            pendingFlush = null;
        }
    }

    private static RunHistoryData CloneHistory(RunHistoryData source)
    {
        RunHistoryData result = new RunHistoryData
        {
            activeSeconds = source != null ? source.activeSeconds : 0f,
            completed = source != null && source.completed
        };
        if (source == null) return result;
        if (source.shownNodeOrder != null) result.shownNodeOrder.AddRange(source.shownNodeOrder);
        if (source.resourceTimeline != null)
            for (int i = 0; i < source.resourceTimeline.Count; i++)
                result.resourceTimeline.Add(CloneSnapshot(source.resourceTimeline[i]));
        if (source.decisions != null)
        {
            for (int i = 0; i < source.decisions.Count; i++)
            {
                DecisionRecordData item = source.decisions[i];
                if (item == null) continue;
                result.decisions.Add(new DecisionRecordData
                {
                    sequence = item.sequence,
                    nodeId = item.nodeId,
                    chapterIndex = item.chapterIndex,
                    optionIndex = item.optionIndex,
                    optionText = item.optionText,
                    nextNodeId = item.nextNodeId,
                    troopEffect = item.troopEffect,
                    foodEffect = item.foodEffect,
                    strategyEffect = item.strategyEffect,
                    riskEffect = item.riskEffect,
                    before = CloneSnapshot(item.before),
                    after = CloneSnapshot(item.after)
                });
            }
        }
        if (source.advisorEchoes != null)
        {
            for (int i = 0; i < source.advisorEchoes.Count; i++)
            {
                AdvisorEchoData item = source.advisorEchoes[i];
                if (item == null) continue;
                result.advisorEchoes.Add(new AdvisorEchoData
                {
                    sequence = item.sequence,
                    nodeId = item.nodeId,
                    chapterIndex = item.chapterIndex,
                    optionIndex = item.optionIndex,
                    guestLabel = item.guestLabel,
                    optionText = item.optionText,
                    reason = item.reason,
                    accepted = item.accepted,
                    summary = item.summary
                });
            }
        }
        return result;
    }

    private static ResourceSnapshotData CloneSnapshot(ResourceSnapshotData source)
    {
        if (source == null) return new ResourceSnapshotData();
        return new ResourceSnapshotData
        {
            sequence = source.sequence,
            nodeId = source.nodeId,
            label = source.label,
            elapsedSeconds = source.elapsedSeconds,
            troop = source.troop,
            food = source.food,
            strategy = source.strategy,
            risk = source.risk
        };
    }

    private static string Compact(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return "未命名选择";
        string singleLine = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return singleLine.Length <= maxCharacters
            ? singleLine
            : singleLine.Substring(0, maxCharacters - 1) + "…";
    }
}
