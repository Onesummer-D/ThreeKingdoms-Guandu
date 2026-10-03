using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RunHistoryTracker : MonoBehaviour
{
    public static RunHistoryTracker Instance { get; private set; }

    public string CurrentRunId => current != null ? current.runId : string.Empty;
    public string CurrentSeed => current != null ? current.seed : string.Empty;

    private RunHistoryData current = new RunHistoryData();
    private readonly List<ResourceChangeEvent> pendingChanges = new List<ResourceChangeEvent>();
    private Coroutine pendingFlush;
    private bool initialized;
    private bool runActive;
    private bool suppressNextRestoredNode;
    private MiniGameTraceData activeMiniGame;

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
        if (runActive && current != null) current.activeSeconds += Time.unscaledDeltaTime;
    }

    public void BeginNewRun()
    {
        ClearPendingChanges();
        activeMiniGame = null;
        suppressNextRestoredNode = false;

        string runId = LocalSaveManager.Instance != null
            ? LocalSaveManager.Instance.CurrentRunId
            : string.Empty;
        if (string.IsNullOrEmpty(runId)) runId = Guid.NewGuid().ToString("N");

        current = new RunHistoryData
        {
            schemaVersion = RunEventSchema.CurrentVersion,
            runId = runId,
            seed = BuildSeed(runId),
            createdAtUtc = DateTime.UtcNow.ToString("O"),
            dataIntegrityValid = true
        };
        current.participants.Add(new RunParticipantData
        {
            participantId = "player",
            role = "player",
            displayName = "曹操 · 曹军主将",
            joinedAtUtc = current.createdAtUtc
        });
        runActive = true;
        AppendEvent(RunEventTypes.RunStarted, 0, "出征", "seed=" + current.seed);
    }

    public void ResetRunState()
    {
        runActive = false;
        activeMiniGame = null;
        suppressNextRestoredNode = false;
        ClearPendingChanges();
        current = new RunHistoryData();
    }

    public RunHistoryData ExportHistory()
    {
        RunHistoryData result = CloneHistory(current);
        NormalizeHistory(result);
        return result;
    }

    public string ExportHistoryJson()
    {
        return RunHistoryDiagnostics.ExportJson(ExportHistory());
    }

    public bool TryValidateHistory(out string message)
    {
        return RunHistoryDiagnostics.TryValidate(ExportHistory(), out message);
    }

    public void RestoreHistory(RunHistoryData saved, bool shouldContinue)
    {
        ClearPendingChanges();
        activeMiniGame = null;
        current = saved != null ? CloneHistory(saved) : new RunHistoryData();
        NormalizeHistory(current);
        runActive = shouldContinue && !current.completed;
        suppressNextRestoredNode = true;
    }

    /// <summary>
    /// Records one completed advisor attempt without touching the authoritative
    /// dialogue/resource path. Every accepted or declined attempt is kept.
    /// </summary>
    public void RecordAdvisorEcho(int nodeId, int chapterIndex, int optionIndex,
        string guestLabel, string optionText, string reason, bool accepted, string summary)
    {
        EnsureCurrentCollections();
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
        AddParticipantIfMissing(guestLabel, "advisor");
        AppendEvent(RunEventTypes.AdvisorSuggested, nodeId, optionText,
            "guest=" + (guestLabel ?? string.Empty) + ";accepted=" + accepted +
            ";reason=" + Compact(reason, 80));
    }

    public void RecordSceneState(string state, string reason)
    {
        if (!runActive) return;
        AppendEvent(RunEventTypes.SceneStateChanged,
            DialogueSystem.Instance != null && DialogueSystem.Instance.CurrentNode != null
                ? DialogueSystem.Instance.CurrentNode.nodeId : 0,
            state, Compact(reason, 120));
    }

    public void RecordMiniGameStarted(string gameType, int nodeId, int nextNodeId)
    {
        if (!runActive) return;
        if (activeMiniGame != null && !activeMiniGame.completed)
            RecordMiniGameCompleted(activeMiniGame.gameType, activeMiniGame.nodeId,
                activeMiniGame.nextNodeId, false, "小游戏被新的小游戏替代");

        activeMiniGame = new MiniGameTraceData
        {
            sequence = current.miniGames.Count + 1,
            gameType = gameType ?? string.Empty,
            nodeId = nodeId,
            nextNodeId = nextNodeId,
            startedElapsedSeconds = current.activeSeconds
        };
        AppendEvent(RunEventTypes.MiniGameStarted, nodeId, gameType,
            "nextNodeId=" + nextNodeId);
    }

    public void RecordMiniGameAction(string actionType, string summary, bool failed)
    {
        if (!runActive || activeMiniGame == null) return;
        activeMiniGame.actionCount++;
        if (failed) activeMiniGame.failedAttempts++;
        activeMiniGame.actionSummary = AppendSummary(activeMiniGame.actionSummary,
            actionType + ":" + Compact(summary, 60));
        RunEventData actionEvent = AppendEvent(RunEventTypes.MiniGameAction, activeMiniGame.nodeId, actionType,
            "failed=" + failed + ";summary=" + Compact(summary, 120));
        actionEvent.success = !failed;
    }

    public void RecordMiniGameCompleted(string gameType, int nodeId, int nextNodeId,
        bool success, string summary)
    {
        if (!runActive) return;
        if (activeMiniGame == null)
        {
            activeMiniGame = new MiniGameTraceData
            {
                sequence = current.miniGames.Count + 1,
                gameType = gameType ?? string.Empty,
                nodeId = nodeId,
                nextNodeId = nextNodeId,
                startedElapsedSeconds = current.activeSeconds
            };
        }

        activeMiniGame.gameType = string.IsNullOrEmpty(activeMiniGame.gameType)
            ? (gameType ?? string.Empty) : activeMiniGame.gameType;
        activeMiniGame.nodeId = activeMiniGame.nodeId == 0 ? nodeId : activeMiniGame.nodeId;
        activeMiniGame.nextNodeId = activeMiniGame.nextNodeId == 0
            ? nextNodeId : activeMiniGame.nextNodeId;
        activeMiniGame.completed = true;
        activeMiniGame.success = success;
        activeMiniGame.completedElapsedSeconds = current.activeSeconds;
        activeMiniGame.actionSummary = AppendSummary(activeMiniGame.actionSummary,
            Compact(summary, 100));
        current.miniGames.Add(CloneMiniGame(activeMiniGame));

        RunEventData completionEvent = AppendEvent(RunEventTypes.MiniGameCompleted, activeMiniGame.nodeId,
            activeMiniGame.gameType,
            "success=" + success + ";actions=" + activeMiniGame.actionCount +
            ";failedAttempts=" + activeMiniGame.failedAttempts +
            ";summary=" + Compact(summary, 120));
        completionEvent.success = success;
        activeMiniGame = null;
    }

    private void HandleNodeShown(int nodeId)
    {
        if ((!runActive && !TerminalNodes.Contains(nodeId)) || nodeId <= 0) return;
        EnsureCurrentCollections();

        if (suppressNextRestoredNode)
        {
            suppressNextRestoredNode = false;
            return;
        }

        if (current.completed && current.ending != null && current.ending.nodeId == nodeId)
            return;

        if (current.shownNodeOrder.Count == 0 ||
            current.shownNodeOrder[current.shownNodeOrder.Count - 1] != nodeId)
            current.shownNodeOrder.Add(nodeId);
        AppendEvent(RunEventTypes.NodeShown, nodeId, "节点显示", string.Empty);

        if (current.resourceTimeline.Count == 0)
            AppendSnapshot(nodeId, "出征");

        if (TerminalNodes.Contains(nodeId))
        {
            AppendSnapshotIfChanged(nodeId, "抵达结局");
            current.ending = BuildEnding(nodeId);
            current.completed = true;
            runActive = false;
            AppendEvent(RunEventTypes.EndingReached, nodeId, current.ending.endingName,
                "endingId=" + current.ending.endingId +
                ";historicalDeviation=" + current.ending.historicalDeviation);
            AppendEvent(RunEventTypes.RunCompleted, nodeId, "战局完成", current.ending.endingId);
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
        int nodeId = DialogueSystem.Instance != null && DialogueSystem.Instance.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId : 0;
        AppendResourceEvent(nodeId, change);
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
        AppendEvent(RunEventTypes.DecisionMade, nodeId, option.optionText,
            "optionIndex=" + optionIndex + ";nextNodeId=" + option.nextNodeId +
            ";effects=" + option.troopEffect + "," + option.foodEffect + "," +
            option.strategyEffect + "," + option.riskEffect);
        AppendSnapshot(nodeId, "决策 · " + Compact(option.optionText, 12));
        ClearPendingChanges();
    }

    private IEnumerator FlushAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        pendingFlush = null;
        if (pendingChanges.Count == 0 || !runActive) yield break;
        int nodeId = DialogueSystem.Instance?.CurrentNode != null
            ? DialogueSystem.Instance.CurrentNode.nodeId : 0;
        AppendSnapshotIfChanged(nodeId, "战局变化");
        pendingChanges.Clear();
    }

    private void AppendResourceEvent(int nodeId, ResourceChangeEvent change)
    {
        RunEventData item = AppendEvent(RunEventTypes.ResourceChanged, nodeId,
            change.resourceType.ToString(), string.Empty);
        item.resourceType = change.resourceType;
        item.oldValue = change.oldValue;
        item.newValue = change.newValue;
        item.changeAmount = change.changeAmount;
    }

    private RunEventData AppendEvent(string eventType, int nodeId, string label, string payload)
    {
        EnsureCurrentCollections();
        RunEventData item = new RunEventData
        {
            schemaVersion = RunEventSchema.CurrentVersion,
            runId = current.runId ?? string.Empty,
            sequence = current.events.Count + 1,
            elapsedSeconds = current.activeSeconds,
            eventType = eventType ?? string.Empty,
            nodeId = nodeId,
            chapterIndex = nodeId > 0 ? CampaignChapterResolver.Resolve(nodeId) : -1,
            label = label ?? string.Empty,
            payload = payload ?? string.Empty
        };
        current.events.Add(item);
        return item;
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

    private static RunHistoryData CloneHistory(RunHistoryData source)
    {
        RunHistoryData result = new RunHistoryData
        {
            schemaVersion = source != null ? source.schemaVersion : RunEventSchema.CurrentVersion,
            runId = source != null ? source.runId : string.Empty,
            seed = source != null ? source.seed : string.Empty,
            createdAtUtc = source != null ? source.createdAtUtc : string.Empty,
            activeSeconds = source != null ? source.activeSeconds : 0f,
            completed = source != null && source.completed,
            dataIntegrityValid = source == null || source.dataIntegrityValid,
            dataIntegrityNote = source != null ? source.dataIntegrityNote : string.Empty
        };
        if (source == null) return result;

        if (source.participants != null)
            for (int i = 0; i < source.participants.Count; i++)
                if (source.participants[i] != null) result.participants.Add(CloneParticipant(source.participants[i]));
        if (source.events != null)
            for (int i = 0; i < source.events.Count; i++)
                if (source.events[i] != null) result.events.Add(CloneEvent(source.events[i]));
        if (source.miniGames != null)
            for (int i = 0; i < source.miniGames.Count; i++)
                if (source.miniGames[i] != null) result.miniGames.Add(CloneMiniGame(source.miniGames[i]));
        result.ending = CloneEnding(source.ending);
        if (source.shownNodeOrder != null) result.shownNodeOrder.AddRange(source.shownNodeOrder);
        if (source.resourceTimeline != null)
            for (int i = 0; i < source.resourceTimeline.Count; i++)
                if (source.resourceTimeline[i] != null) result.resourceTimeline.Add(CloneSnapshot(source.resourceTimeline[i]));
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

    private static void NormalizeHistory(RunHistoryData history)
    {
        if (history == null) return;
        EnsureCollections(history);
        bool legacy = history.schemaVersion <= 0;
        if (string.IsNullOrEmpty(history.runId)) history.runId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrEmpty(history.seed)) history.seed = BuildSeed(history.runId);
        if (string.IsNullOrEmpty(history.createdAtUtc)) history.createdAtUtc = DateTime.UtcNow.ToString("O");
        if (history.participants.Count == 0)
        {
            history.participants.Add(new RunParticipantData
            {
                participantId = "player",
                role = "player",
                displayName = "曹操 · 曹军主将",
                joinedAtUtc = history.createdAtUtc
            });
        }
        if (history.ending == null) history.ending = new EndingAnalysisData();
        if (legacy || history.events.Count == 0)
        {
            RebuildLegacyEvents(history);
            history.schemaVersion = RunEventSchema.CurrentVersion;
            history.dataIntegrityNote = AppendSummary(history.dataIntegrityNote, "legacy history migrated");
        }
        ValidateIntegrity(history);
    }

    private static void EnsureCollections(RunHistoryData history)
    {
        if (history.participants == null) history.participants = new List<RunParticipantData>();
        if (history.events == null) history.events = new List<RunEventData>();
        if (history.miniGames == null) history.miniGames = new List<MiniGameTraceData>();
        if (history.shownNodeOrder == null) history.shownNodeOrder = new List<int>();
        if (history.decisions == null) history.decisions = new List<DecisionRecordData>();
        if (history.resourceTimeline == null) history.resourceTimeline = new List<ResourceSnapshotData>();
        if (history.advisorEchoes == null) history.advisorEchoes = new List<AdvisorEchoData>();
    }

    private void EnsureCurrentCollections()
    {
        if (current == null) current = new RunHistoryData();
        EnsureCollections(current);
        if (string.IsNullOrEmpty(current.runId)) current.runId = Guid.NewGuid().ToString("N");
        if (string.IsNullOrEmpty(current.seed)) current.seed = BuildSeed(current.runId);
        if (current.ending == null) current.ending = new EndingAnalysisData();
    }

    private static void RebuildLegacyEvents(RunHistoryData history)
    {
        history.events.Clear();
        int sequence = 0;
        AddLegacyEvent(history, ref sequence, RunEventTypes.RunStarted, 0, "出征", "legacy=true");
        for (int i = 0; i < history.shownNodeOrder.Count; i++)
            AddLegacyEvent(history, ref sequence, RunEventTypes.NodeShown,
                history.shownNodeOrder[i], "节点显示", "legacy=true");
        for (int i = 0; i < history.decisions.Count; i++)
        {
            DecisionRecordData decision = history.decisions[i];
            if (decision == null) continue;
            AddLegacyEvent(history, ref sequence, RunEventTypes.DecisionMade,
                decision.nodeId, decision.optionText,
                "optionIndex=" + decision.optionIndex + ";nextNodeId=" + decision.nextNodeId);
        }
        for (int i = 0; i < history.advisorEchoes.Count; i++)
        {
            AdvisorEchoData echo = history.advisorEchoes[i];
            if (echo == null) continue;
            AddLegacyEvent(history, ref sequence, RunEventTypes.AdvisorSuggested,
                echo.nodeId, echo.optionText, "legacy=true;accepted=" + echo.accepted);
        }
        if (history.completed && history.ending != null && history.ending.nodeId > 0)
        {
            AddLegacyEvent(history, ref sequence, RunEventTypes.EndingReached,
                history.ending.nodeId, history.ending.endingName, "legacy=true");
            AddLegacyEvent(history, ref sequence, RunEventTypes.RunCompleted,
                history.ending.nodeId, "战局完成", "legacy=true");
        }
    }

    private static void AddLegacyEvent(RunHistoryData history, ref int sequence,
        string type, int nodeId, string label, string payload)
    {
        sequence++;
        history.events.Add(new RunEventData
        {
            schemaVersion = RunEventSchema.CurrentVersion,
            runId = history.runId,
            sequence = sequence,
            elapsedSeconds = 0f,
            eventType = type,
            nodeId = nodeId,
            chapterIndex = nodeId > 0 ? CampaignChapterResolver.Resolve(nodeId) : -1,
            label = label ?? string.Empty,
            payload = payload ?? string.Empty
        });
    }

    private static void ValidateIntegrity(RunHistoryData history)
    {
        bool valid = !string.IsNullOrEmpty(history.runId) && !string.IsNullOrEmpty(history.seed);
        string note = history.dataIntegrityNote ?? string.Empty;
        for (int i = 0; i < history.events.Count; i++)
        {
            RunEventData item = history.events[i];
            if (item == null || item.sequence != i + 1 || string.IsNullOrEmpty(item.eventType) ||
                string.IsNullOrEmpty(item.runId))
            {
                valid = false;
                note = AppendSummary(note, "event sequence or identity invalid at index " + i);
                break;
            }
        }
        history.dataIntegrityValid = valid;
        history.dataIntegrityNote = note;
    }

    private EndingAnalysisData BuildEnding(int nodeId)
    {
        EndingAnalysisData ending = new EndingAnalysisData
        {
            nodeId = nodeId,
            endingId = "UNKNOWN",
            endingName = "未知结局",
            historicalAnchor = "官渡之战",
            historicalDeviation = "架空推演分支"
        };
        switch (nodeId)
        {
            case 200314: ending.endingId = "IF1"; ending.endingName = "半途而废"; ending.reason = "过早退出官渡主线"; break;
            case 300416: ending.endingId = "IF2"; ending.endingName = "错失良机"; ending.reason = "关键节点未能把握战机"; break;
            case 500217:
                ending.endingId = "HISTORICAL";
                ending.endingName = "史实胜利";
                ending.reason = "粮道与决战判断共同形成优势";
                ending.historicalDeviation = "与史实叙事锚点一致的游戏化演绎";
                break;
            case 500313: ending.endingId = "IF3"; ending.endingName = "元气大伤"; ending.reason = "战局代价过高"; break;
            case 500320: ending.endingId = "IF4"; ending.endingName = "以退为进"; ending.reason = "选择撤退保存实力"; break;
            case 500411: ending.endingId = "IF5"; ending.endingName = "完美结局"; ending.reason = "资源与风险保持在有利区间"; break;
            case 500418: ending.endingId = "IF6"; ending.endingName = "惨败结局"; ending.reason = "风险累积导致战局失控"; break;
        }
        for (int i = Mathf.Max(0, current.decisions.Count - 3); i < current.decisions.Count; i++)
            ending.keyDecisionSequences.Add(current.decisions[i].sequence);
        return ending;
    }

    private void AddParticipantIfMissing(string displayName, string role)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return;
        for (int i = 0; i < current.participants.Count; i++)
            if (string.Equals(current.participants[i].displayName, displayName, StringComparison.OrdinalIgnoreCase)) return;
        current.participants.Add(new RunParticipantData
        {
            participantId = role + "-" + current.participants.Count,
            role = role,
            displayName = displayName.Trim(),
            joinedAtUtc = DateTime.UtcNow.ToString("O")
        });
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

    private static RunParticipantData CloneParticipant(RunParticipantData source)
    {
        if (source == null) return new RunParticipantData();
        return new RunParticipantData
        {
            participantId = source.participantId,
            role = source.role,
            displayName = source.displayName,
            joinedAtUtc = source.joinedAtUtc
        };
    }

    private static RunEventData CloneEvent(RunEventData source)
    {
        if (source == null) return new RunEventData();
        return new RunEventData
        {
            schemaVersion = source.schemaVersion,
            runId = source.runId,
            sequence = source.sequence,
            elapsedSeconds = source.elapsedSeconds,
            eventType = source.eventType,
            nodeId = source.nodeId,
            chapterIndex = source.chapterIndex,
            label = source.label,
            payload = source.payload,
            resourceType = source.resourceType,
            oldValue = source.oldValue,
            newValue = source.newValue,
            changeAmount = source.changeAmount,
            success = source.success
        };
    }

    private static MiniGameTraceData CloneMiniGame(MiniGameTraceData source)
    {
        if (source == null) return new MiniGameTraceData();
        return new MiniGameTraceData
        {
            sequence = source.sequence,
            gameType = source.gameType,
            nodeId = source.nodeId,
            nextNodeId = source.nextNodeId,
            actionCount = source.actionCount,
            failedAttempts = source.failedAttempts,
            completed = source.completed,
            success = source.success,
            startedElapsedSeconds = source.startedElapsedSeconds,
            completedElapsedSeconds = source.completedElapsedSeconds,
            actionSummary = source.actionSummary
        };
    }

    private static EndingAnalysisData CloneEnding(EndingAnalysisData source)
    {
        EndingAnalysisData result = new EndingAnalysisData();
        if (source == null) return result;
        result.nodeId = source.nodeId;
        result.endingId = source.endingId;
        result.endingName = source.endingName;
        result.reason = source.reason;
        result.historicalAnchor = source.historicalAnchor;
        result.historicalDeviation = source.historicalDeviation;
        if (source.keyDecisionSequences != null)
            result.keyDecisionSequences.AddRange(source.keyDecisionSequences);
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

    private static string BuildSeed(string runId)
    {
        if (string.IsNullOrEmpty(runId)) return Guid.NewGuid().ToString("N");
        return runId.Substring(0, Mathf.Min(12, runId.Length));
    }

    private static string AppendSummary(string existing, string addition)
    {
        if (string.IsNullOrEmpty(addition)) return existing ?? string.Empty;
        if (string.IsNullOrEmpty(existing)) return addition;
        return existing + " | " + addition;
    }

    private static string Compact(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value)) return "未命名事件";
        string singleLine = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return singleLine.Length <= maxCharacters
            ? singleLine
            : singleLine.Substring(0, Mathf.Max(1, maxCharacters - 1)) + "…";
    }
}
