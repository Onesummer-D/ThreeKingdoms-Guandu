using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class LocalSaveManager : MonoBehaviour
{
    public const int ManualSlotCount = 3;
    public const string AutoSlotId = "auto";

    public static LocalSaveManager Instance { get; private set; }

    private CampaignMapUI campaignMapUI;
    private string currentRunId;

    private static readonly Dictionary<int, EndingDescriptor> Endings =
        new Dictionary<int, EndingDescriptor>
        {
            { 200314, new EndingDescriptor("IF1", "半途而废") },
            { 300416, new EndingDescriptor("IF2", "错失良机") },
            { 500217, new EndingDescriptor("HISTORICAL", "史实胜利") },
            { 500313, new EndingDescriptor("IF3", "元气大伤") },
            { 500320, new EndingDescriptor("IF4", "以退为进") },
            { 500411, new EndingDescriptor("IF5", "完美结局") },
            { 500418, new EndingDescriptor("IF6", "惨败结局") }
        };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    public void Initialize(CampaignMapUI mapUI)
    {
        campaignMapUI = mapUI;
    }

    public void BeginNewRun()
    {
        currentRunId = Guid.NewGuid().ToString("N");
    }

    public bool SaveManualSlot(int slotIndex, out string message)
    {
        if (slotIndex < 1 || slotIndex > ManualSlotCount)
        {
            message = "存档槽编号无效。";
            return false;
        }

        RunSaveData data;
        if (!TryCapture("slot" + slotIndex, out data, out message)) return false;
        data.displayName = BuildManualDisplayName(slotIndex, data);
        return TryWrite(data, out message);
    }

    public bool SaveAutoCheckpoint(int nodeId, out string message)
    {
        if (!IsSafeCheckpointNode(nodeId))
        {
            message = "当前节点不是安全检查点。";
            return false;
        }

        RunSaveData data;
        if (!TryCapture(AutoSlotId, out data, out message)) return false;
        EndingDescriptor ending;
        if (Endings.TryGetValue(nodeId, out ending))
        {
            data.isCompleted = true;
            data.endingId = ending.id;
            data.endingName = ending.name;
            data.displayName = "通关结局 " + GetEndingOrdinal(ending.id) + "：" + ending.name;
        }
        else
        {
            data.displayName = "自动存档 · " + GetChapterLabel(nodeId);
        }
        return TryWrite(data, out message);
    }

    public bool TryLoadSlot(string slotId, out RunSaveData data, out string message)
    {
        data = null;
        if (!IsKnownSlot(slotId))
        {
            message = "存档槽不存在。";
            return false;
        }

        string path = GetSlotPath(slotId);
        if (TryReadFile(path, out data, out message)) return true;

        string primaryError = message;
        string backupPath = path + ".bak";
        if (File.Exists(backupPath) && TryReadFile(backupPath, out data, out message))
        {
            message = "主存档损坏，已读取上一次备份。";
            return true;
        }

        data = null;
        message = primaryError;
        return false;
    }

    public bool TryRestore(RunSaveData data, out string message)
    {
        if (!Validate(data, out message)) return false;
        if (DialogueSystem.Instance == null || ResourceManager.Instance == null || campaignMapUI == null)
        {
            message = "游戏数据尚未准备完成，请稍后再试。";
            return false;
        }
        if (!DialogueSystem.Instance.HasLoadedNode(data.currentNodeId))
        {
            message = "存档中的剧情节点已不存在，原文件已保留。";
            return false;
        }

        // Restore passive state first. Dialogue is restored last because its
        // single presentation event refreshes the complete gameplay UI.
        ResourceManager.Instance.RestoreSnapshot(data.troop, data.food, data.strategy, data.risk);
        if (RunHistoryTracker.Instance != null)
            RunHistoryTracker.Instance.RestoreHistory(data.history, !data.isCompleted);
        campaignMapUI.RestoreSelectedChoices(data.selectedChoices);
        if (GameData.Instance != null)
            GameData.Instance.RestoreRunState(data.currentNodeId, !data.isCompleted, data.unlockedIfLines);
        if (IfLineManager.Instance != null)
            IfLineManager.Instance.RestoreUnlockedIfLines(data.unlockedIfLines);
        if (EndingManager.Instance != null)
            EndingManager.Instance.RestoreEndingState(data.endingId);

        if (!DialogueSystem.Instance.RestoreRunState(data.currentNodeId, data.visitedNodeOrder))
        {
            message = "存档中的剧情节点已不存在，无法恢复。";
            return false;
        }

        currentRunId = string.IsNullOrEmpty(data.runId) ? Guid.NewGuid().ToString("N") : data.runId;
        message = "已读取：" + data.displayName;
        return true;
    }

    public bool SlotExists(string slotId)
    {
        return IsKnownSlot(slotId) && File.Exists(GetSlotPath(slotId));
    }

    public string GetSlotPathForDiagnostics(string slotId)
    {
        return IsKnownSlot(slotId) ? GetSlotPath(slotId) : string.Empty;
    }

    private bool TryCapture(string slotId, out RunSaveData data, out string message)
    {
        data = null;
        DialogueSystem dialogue = DialogueSystem.Instance;
        ResourceManager resources = ResourceManager.Instance;
        if (dialogue == null || resources == null || dialogue.CurrentNode == null || campaignMapUI == null)
        {
            message = "当前没有可保存的剧情进度。";
            return false;
        }

        int nodeId = dialogue.CurrentNode.nodeId;
        if (!IsSafeCheckpointNode(nodeId))
        {
            message = "本段正在结算，将在下一个安全节点保存。";
            return false;
        }

        if (string.IsNullOrEmpty(currentRunId)) currentRunId = Guid.NewGuid().ToString("N");
        data = new RunSaveData
        {
            slotId = slotId,
            runId = currentRunId,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            currentNodeId = nodeId,
            visitedNodeOrder = dialogue.ExportVisitedNodeOrder(),
            selectedChoices = campaignMapUI.ExportSelectedChoices(),
            troop = resources.GetTroop(),
            food = resources.GetFood(),
            strategy = resources.GetStrategy(),
            risk = resources.GetRisk(),
            history = RunHistoryTracker.Instance != null
                ? RunHistoryTracker.Instance.ExportHistory()
                : new RunHistoryData(),
            unlockedIfLines = GameData.Instance != null
                ? new List<string>(GameData.Instance.unlockedIfLines)
                : new List<string>()
        };

        EndingDescriptor ending;
        if (Endings.TryGetValue(nodeId, out ending))
        {
            data.isCompleted = true;
            data.endingId = ending.id;
            data.endingName = ending.name;
        }
        message = "存档快照已生成。";
        return true;
    }

    private static bool TryWrite(RunSaveData data, out string message)
    {
        try
        {
            Directory.CreateDirectory(GetSaveDirectory());
            string path = GetSlotPath(data.slotId);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, backupPath);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Copy(path, backupPath, true);
                    File.Copy(tempPath, path, true);
                    File.Delete(tempPath);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
            message = "保存成功：" + data.displayName;
            return true;
        }
        catch (Exception exception)
        {
            message = "保存失败：" + exception.Message;
            Debug.LogError(message);
            return false;
        }
    }

    private static bool TryReadFile(string path, out RunSaveData data, out string message)
    {
        data = null;
        if (!File.Exists(path))
        {
            message = "该存档槽为空。";
            return false;
        }
        try
        {
            data = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(path));
            if (!Validate(data, out message))
            {
                data = null;
                return false;
            }
            message = "存档读取成功。";
            return true;
        }
        catch (Exception exception)
        {
            message = "存档文件损坏或无法读取：" + exception.Message;
            return false;
        }
    }

    private static bool Validate(RunSaveData data, out string message)
    {
        if (data == null)
        {
            message = "存档内容为空。";
            return false;
        }
        if (data.formatVersion != RunSaveData.CurrentFormatVersion)
        {
            message = "存档版本不兼容，原文件已保留。";
            return false;
        }
        if (data.dialogueVersion != RunSaveData.CurrentDialogueVersion)
        {
            message = "剧情数据版本不兼容，原文件已保留。";
            return false;
        }
        if (data.currentNodeId <= 0 || data.visitedNodeOrder == null || data.visitedNodeOrder.Count == 0)
        {
            message = "存档缺少剧情进度。";
            return false;
        }
        message = string.Empty;
        return true;
    }

    private static bool IsSafeCheckpointNode(int nodeId)
    {
        return nodeId > 0 && nodeId != 500308 && nodeId != 500406;
    }

    private static bool IsKnownSlot(string slotId)
    {
        return slotId == AutoSlotId || slotId == "slot1" || slotId == "slot2" || slotId == "slot3";
    }

    private static string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "GuanduSaves");
    }

    private static string GetSlotPath(string slotId)
    {
        return Path.Combine(GetSaveDirectory(), slotId + ".json");
    }

    private static string BuildManualDisplayName(int slotIndex, RunSaveData data)
    {
        if (data.isCompleted)
            return "存档 " + slotIndex + " · 通关结局 " + GetEndingOrdinal(data.endingId) + "：" + data.endingName;
        return "存档 " + slotIndex + " · " + GetChapterLabel(data.currentNodeId);
    }

    private static string GetChapterLabel(int nodeId)
    {
        int chapter = CampaignChapterResolver.Resolve(nodeId);
        return chapter >= 0 ? "第" + (chapter + 1) + "幕" : "剧情进行中";
    }

    private static string GetEndingOrdinal(string endingId)
    {
        switch (endingId)
        {
            case "HISTORICAL": return "1";
            case "IF1": return "2";
            case "IF2": return "3";
            case "IF3": return "4";
            case "IF4": return "5";
            case "IF5": return "6";
            case "IF6": return "7";
            default: return "?";
        }
    }

    private struct EndingDescriptor
    {
        public readonly string id;
        public readonly string name;

        public EndingDescriptor(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
