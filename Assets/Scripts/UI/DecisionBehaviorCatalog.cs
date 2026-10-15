using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Versioned semantic metadata for real key decisions. No text matching:
/// changing a dialogue option's copy cannot change its strategic classification.
/// The chosen (nodeId, optionIndex) must also exist in RunHistoryData.decisions.
/// </summary>
public static class DecisionBehaviorCatalog
{
    [Serializable]
    public sealed class Entry
    {
        public int nodeId;
        public int optionIndex;
        public int nextNodeId;
        public string strategy;
        public string action;
        public string counsel;
    }

    [Serializable]
    public sealed class Library
    {
        public int version;
        public Entry[] entries;
    }

    private static Dictionary<long, Entry> cache;
    private static long Key(int nodeId, int optionIndex)
    {
        return ((long)nodeId << 32) | (uint)optionIndex;
    }

    public static bool TryFind(int nodeId, int optionIndex, out Entry item)
    {
        if (cache == null) Load();
        return cache.TryGetValue(Key(nodeId, optionIndex), out item);
    }

    private static void Load()
    {
        cache = new Dictionary<long, Entry>();
        TextAsset json = Resources.Load<TextAsset>("BattleReport/DecisionBehaviors");
        if (json == null)
        {
            Debug.LogWarning("[DecisionProfile] Missing decision behavior catalog. No keyword fallback.");
            return;
        }
        try
        {
            Library library = JsonUtility.FromJson<Library>(json.text);
            if (library == null || library.version != 1 || library.entries == null)
                throw new Exception("Unsupported catalog version");
            foreach (Entry e in library.entries)
            {
                if (e == null || e.nodeId <= 0 || e.optionIndex < 0 || e.nextNodeId <= 0 ||
                    string.IsNullOrWhiteSpace(e.action) ||
                    (e.strategy != "" && e.strategy != "稳慎" &&
                     e.strategy != "奇谋" && e.strategy != "果决"))
                {
                    Debug.LogWarning("[DecisionProfile] Invalid catalog row ignored.");
                    continue;
                }
                long key = Key(e.nodeId, e.optionIndex);
                if (!cache.ContainsKey(key)) cache.Add(key, e);
                else Debug.LogWarning("[DecisionProfile] Duplicate decision mapping ignored.");
            }
        }
        catch (Exception error)
        {
            cache.Clear();
            Debug.LogWarning("[DecisionProfile] Failed to parse behavior catalog: " + error.Message);
        }
    }
}
