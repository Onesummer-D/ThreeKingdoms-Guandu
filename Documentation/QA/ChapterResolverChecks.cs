using System;

public static class ChapterResolverChecks
{
    public static int Main()
    {
        // Reproduction: opening the recap immediately at the fourth anchor
        // must already yield act IV, without waiting for 400201 to be entered.
        int[][] reachedRoutes = {
            new[] {100001, 100005, 1001, 1004, 100101, 100201, 100301},
            new[] {200001, 2001, 200005, 200309, 200314, 200401},
            new[] {3001, 300101, 300410, 300416},
            new[] {4001, 400101, 400201, 400301},
            new[] {5001, 500101, 500211, 500217, 500313, 500320, 500411, 500418}
        };
        int checks = 0;
        for (int expected = 0; expected < reachedRoutes.Length; expected++)
            foreach (int id in reachedRoutes[expected])
            {
                if (CampaignChapterResolver.Resolve(id) != expected)
                    throw new Exception("Wrong chapter for " + id);
                checks++;
            }
        foreach (int invalid in new[] {-1, 0, 1, 2000, 6001, 99999, 600000})
        {
            if (CampaignChapterResolver.Resolve(invalid) != -1)
                throw new Exception("Unexpected chapter for " + invalid);
            checks++;
        }
        Console.WriteLine("PASS: " + checks + " chapter checks (production resolver).");
        return 0;
    }
}
