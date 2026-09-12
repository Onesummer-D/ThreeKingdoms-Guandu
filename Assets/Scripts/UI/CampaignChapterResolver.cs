/// <summary>Maps the two existing node-ID formats without overlapping ranges.</summary>
public static class CampaignChapterResolver
{
    public static int Resolve(int nodeId)
    {
        // Four-digit decision/transition nodes present in GuanduDialogueData.
        switch (nodeId)
        {
            case 1001:
            case 1004: return 0;
            case 2001: return 1;
            case 3001: return 2;
            case 4001: return 3;
            case 5001: return 4;
        }
        // All other story nodes use the six-digit chapter prefix.
        if (nodeId >= 100000 && nodeId < 600000) return nodeId / 100000 - 1;
        return -1;
    }
}
