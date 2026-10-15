using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;

// Tiny Unity stubs make the production InviteStoryRecapBuilder execute
// in CI without requiring Unity editor licenses or proprietary assemblies.
namespace UnityEngine
{
    public sealed class TextAsset
    {
        public string text;
        public TextAsset(string value) { text = value; }
    }

    public static class Resources
    {
        public static T Load<T>(string location) where T : class
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "Assets", "Resources", location + ".json");
            return File.Exists(path)
                ? new TextAsset(File.ReadAllText(path)) as T
                : null;
        }
    }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { IncludeFields = true });
        }
    }

    public static class Debug
    {
        public static void LogWarning(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}

internal static class Program
{
    private static int passed;

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL " + label);
        Console.WriteLine("PASS " + label);
        passed++;
    }

    private static string Recap(int current, params int[] shown)
        => InviteStoryRecapBuilder.Build(shown, current);

    private static void Contains(string text, string fragment, string label)
        => Check(text.Contains(fragment, StringComparison.Ordinal), label);

    private static void Excludes(string text, string fragment, string label)
        => Check(!text.Contains(fragment, StringComparison.Ordinal), label);

    private static void Main()
    {
        string path = "Assets/Resources/StoryRecap/InviteEvents.json";
        var data = JsonSerializer.Deserialize<InviteStoryRecapBuilder.RecapEventLibrary>(
            File.ReadAllText(path),
            new JsonSerializerOptions { IncludeFields = true });
        var entries = data.events;
        Check(entries.Length == 29, "Seed library has 29 short grounded events");
        Check(entries.Select(e => e.id).Distinct().Count() == entries.Length, "Event IDs unique");
        Check(entries.All(e => e.chapter >= 1 && e.chapter <= 5 &&
            e.triggerNodeId > 0 && e.text.Length <= 65), "Card schema and length valid");

        string serialized = File.ReadAllText("Assets/Scripts/Data/GuanduDialogueData.asset");
        foreach (var entry in entries)
            Check(Regex.IsMatch(serialized,
                    @"(?m)^  - nodeId: " + entry.triggerNodeId + @"\s*$"),
                "Trigger exists in real story graph: " + entry.id);

        Check(Recap(100101, 100101) == "暂无已走剧情",
            "The currently displayed first node cannot leak itself");
        string first = Recap(100104, 100101, 100104);
        Contains(first, "曹袁两军", "Earlier event unlocked after leaving node");
        Excludes(first, "放箭", "Current arrow scene is not prematurely recapped");
        Excludes(first, "发石", "An unseen choice branch does not leak");

        string beforeConstruction = Recap(100203, 100101, 100104, 100203);
        Contains(beforeConstruction, "弓弩", "Previously witnessed arrow scene recapped");
        Excludes(beforeConstruction, "赶制", "Current construction node withheld");

        string revealBefore = Recap(300309, 100101, 100104, 200001, 300105, 300202, 300309);
        Excludes(revealBefore, "透露袁军乌巢粮仓", "Intelligence not shown before leaving reveal node");
        string revealAfter = Recap(400101, 100101, 100104, 200001, 300105,
            300202, 300309, 400101);
        Contains(revealAfter, "透露袁军乌巢粮仓", "Intelligence appears after actual reveal");

        string executionBefore = Recap(300408, 100101, 200001, 300105, 300401, 300408);
        Excludes(executionBefore, "遭到处决", "Current execution scene withheld until left");
        string executionAfter = Recap(300411, 100101, 200001,
            300105, 300401, 300408, 300411);
        Contains(executionAfter, "许攸遭到处决", "Actual execution may be recalled once past");
        Excludes(executionAfter, "透露袁军乌巢粮仓", "Execution route does not inherit intelligence from a different branch");

        string retreat = Recap(200314, 100101, 100104, 200001, 200005, 200310, 200314);
        Contains(retreat, "撤退途中遭袁军", "Retreat-only events included when visited");
        Excludes(retreat, "采纳劝守", "Retreat route does not include the hold-the-line choice");

        string fireBefore = Recap(500101, 100101, 200001, 300105,
            300309, 400101, 500101);
        Excludes(fireBefore, "第一把火", "Fire event cannot be added while being shown");
        string fireAfter = Recap(500201, 100101, 200001, 300105,
            300309, 400101, 500101, 500201);
        Contains(fireAfter, "第一把火", "Fire event appears only after visiting and leaving the event node");
        Excludes(fireAfter, "集中兵力攻占", "Next battle strategy still undecided");

        string defend = Recap(500310, 100101, 200001,
            300105, 300309, 400101, 500101, 500306, 500310);
        Contains(defend, "回援官渡", "Divided-force route references real reinforcements");
        Excludes(defend, "攻势一度放缓", "The current battle outcome stays hidden");
        Excludes(defend, "集中兵力攻占", "No cross-branch assault orders");

        string ifRoute = Recap(500418, 100101, 200001, 300105,
            400101, 500101, 500408, 500415, 500417, 500418);
        Contains(ifRoute, "转攻袁绍本阵", "IF route real order shown");
        Contains(ifRoute, "倚天剑坠落", "IF route later factual outcome shown");
        Excludes(ifRoute, "集中兵力攻占", "IF route does not import another choice");

        string duplicate = Recap(300411, 100101, 200001,
            300105, 300105, 300401, 300408, 300411);
        Check(duplicate.Split("许攸深夜来到曹营").Length == 2,
            "Revisited nodes never cause duplicate recap cards");
        Check(duplicate.Split('\n').Length <= 5,
            "Recap keeps at most five chapter groups");
        string dense = Recap(500418, entries.Select(e => e.triggerNodeId).Concat(new[]{500418}).ToArray());
        Check(dense.Split('\n').Length == 5, "Dense route yields five compact chapter lines");
        Check(dense.Length < 700, "Dense route stays short enough for invitation UI");

        string anotherRun = Recap(300105, 100101, 200001, 300105);
        Excludes(anotherRun, "倚天剑", "New request does not retain prior run's events");
        Console.WriteLine("invite-recap-regressions: " + passed + "/" + passed);
    }
}
