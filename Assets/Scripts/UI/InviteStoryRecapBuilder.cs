using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Evidence-only recap for a public "军议邀约".
/// Cards unlock after their trigger node has already been shown AND left.
/// This is deliberately separate from the richer, sometimes forward-looking
/// five-act summaries in CampaignMapUI's story gallery.
/// </summary>
public static class InviteStoryRecapBuilder
{
    [Serializable]
    public sealed class RecapEvent
    {
        public string id;
        public int chapter;
        public int triggerNodeId;
        public string text;
    }

    [Serializable]
    public sealed class RecapEventLibrary
    {
        public RecapEvent[] events;
    }

    private const string ResourcePath = "StoryRecap/InviteEvents";
    private static readonly string[] ChapterNames =
    {
        "第一幕", "第二幕", "第三幕", "第四幕", "第五幕"
    };

    private static bool loaded;
    private static Dictionary<int, List<RecapEvent>> eventsByTrigger;

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        eventsByTrigger = new Dictionary<int, List<RecapEvent>>();

        TextAsset file = Resources.Load<TextAsset>(ResourcePath);
        if (file == null)
        {
            Debug.LogWarning("[InviteStoryRecap] Missing " + ResourcePath +
                             ".json; returning no facts rather than an unverified chapter summary.");
            return;
        }

        RecapEventLibrary library;
        try
        {
            library = JsonUtility.FromJson<RecapEventLibrary>(file.text);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[InviteStoryRecap] Invalid event JSON: " + exception.Message);
            return;
        }

        if (library == null || library.events == null) return;

        HashSet<string> seenIds = new HashSet<string>();
        for (int i = 0; i < library.events.Length; i++)
        {
            RecapEvent item = library.events[i];
            if (item == null ||
                string.IsNullOrWhiteSpace(item.id) ||
                !seenIds.Add(item.id) ||
                item.chapter < 1 || item.chapter > ChapterNames.Length ||
                item.triggerNodeId <= 0 ||
                string.IsNullOrWhiteSpace(item.text))
            {
                Debug.LogWarning("[InviteStoryRecap] Invalid or duplicate entry at index " + i);
                continue;
            }
            // A recap card must be a short, self-contained fact. It must never
            // contain an option's predicted payoff or an unseen IF ending.
            if (!eventsByTrigger.TryGetValue(item.triggerNodeId, out List<RecapEvent> events))
            {
                events = new List<RecapEvent>();
                eventsByTrigger.Add(item.triggerNodeId, events);
            }
            events.Add(item);
        }
    }

    /// <summary>
    /// Use actual shown-node order, not chapter reached/maximum node id.
    /// For the strictest boundary we exclude the currently displayed node;
    /// "正在考虑" already supplies its text separately in the invitation.
    /// </summary>
    public static string Build(IList<int> shownNodeOrder, int currentNodeId)
    {
        EnsureLoaded();
        if (shownNodeOrder == null || shownNodeOrder.Count == 0 ||
            eventsByTrigger == null || eventsByTrigger.Count == 0)
            return "暂无已走剧情";

        List<RecapEvent>[] byChapter = new List<RecapEvent>[ChapterNames.Length];
        HashSet<string> emitted = new HashSet<string>();
        for (int i = 0; i < shownNodeOrder.Count; i++)
        {
            int nodeId = shownNodeOrder[i];
            // Fail closed: current text might still be typing onto screen.
            if (nodeId <= 0 || nodeId == currentNodeId) continue;
            if (!eventsByTrigger.TryGetValue(nodeId, out List<RecapEvent> candidates)) continue;
            for (int j = 0; j < candidates.Count; j++)
            {
                RecapEvent item = candidates[j];
                if (!emitted.Add(item.id)) continue;
                int chapterIndex = item.chapter - 1;
                if (byChapter[chapterIndex] == null)
                    byChapter[chapterIndex] = new List<RecapEvent>();
                byChapter[chapterIndex].Add(item);
            }
        }

        StringBuilder result = new StringBuilder(512);
        for (int i = 0; i < byChapter.Length; i++)
        {
            List<RecapEvent> cards = byChapter[i];
            if (cards == null || cards.Count == 0) continue;
            if (result.Length > 0) result.Append('\n');
            result.Append(ChapterNames[i]).Append("：");

            // Keep the earliest grounded chapter context, plus up to two
            // latest events in that chapter. No long 30-card timeline in
            // the invited advisor's small mobile scroll area.
            result.Append(cards[0].text.Trim());
            int firstRecent = Math.Max(1, cards.Count - 2);
            for (int j = firstRecent; j < cards.Count; j++)
                result.Append(' ').Append(cards[j].text.Trim());
        }

        return result.Length == 0 ? "暂无已走剧情" : result.ToString();
    }
}
