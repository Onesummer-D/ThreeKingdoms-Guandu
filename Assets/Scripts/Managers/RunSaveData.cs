using System;
using System.Collections.Generic;

[Serializable]
public sealed class SavedChoiceData
{
    public int anchorNodeId;
    public int optionIndex;

    public SavedChoiceData() { }

    public SavedChoiceData(int anchorNodeId, int optionIndex)
    {
        this.anchorNodeId = anchorNodeId;
        this.optionIndex = optionIndex;
    }
}

[Serializable]
public sealed class RunSaveData
{
    public const int CurrentFormatVersion = 1;
    public const string CurrentDialogueVersion = "guandu-2026-09";

    public int formatVersion = CurrentFormatVersion;
    public string dialogueVersion = CurrentDialogueVersion;
    public string slotId;
    public string runId;
    public string savedAtUtc;
    public string displayName;
    public int currentNodeId;
    public List<int> visitedNodeOrder = new List<int>();
    public List<SavedChoiceData> selectedChoices = new List<SavedChoiceData>();
    public float troop;
    public float food;
    public float strategy;
    public float risk;
    public List<string> unlockedIfLines = new List<string>();
    public RunHistoryData history = new RunHistoryData();
    public bool isCompleted;
    public string endingId;
    public string endingName;
}
