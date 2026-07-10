using UnityEngine;

// Rank derived from total XP. XP comes from battles (kills/wins) and unlocking gear.
public static class Rank
{
    // XP needed to advance from `level` to level+1 (gradually increasing).
    static int Step(int level) => 120 + (level - 1) * 90;

    public static int LevelOf(int xp)
    {
        int lvl = 1;
        while (xp >= Step(lvl)) { xp -= Step(lvl); lvl++; }
        return lvl;
    }

    public static int XpInto(int xp)
    {
        int lvl = 1;
        while (xp >= Step(lvl)) { xp -= Step(lvl); lvl++; }
        return xp;
    }

    public static int XpForLevel(int level) => Step(level);

    public static string Tier(int level)
    {
        if (level >= 25) return "LEGEND";
        if (level >= 20) return "ELITE";
        if (level >= 15) return "ACE";
        if (level >= 11) return "VETERAN";
        if (level >= 7) return "PILOT";
        if (level >= 4) return "CADET";
        return "RECRUIT";
    }

    public static Color TierColor(int level)
    {
        if (level >= 25) return new Color(1f, 0.3f, 0.3f);
        if (level >= 20) return new Color(1f, 0.5f, 1f);
        if (level >= 15) return new Color(1f, 0.8f, 0.3f);
        if (level >= 11) return new Color(0.5f, 0.9f, 1f);
        if (level >= 7) return new Color(0.5f, 1f, 0.6f);
        if (level >= 4) return new Color(0.7f, 0.82f, 0.95f);
        return new Color(0.72f, 0.72f, 0.78f);
    }
}
