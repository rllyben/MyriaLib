namespace MyriaLib.Services
{
    /// <summary>
    /// XP and level calculations for job aspects (Skill, Knowledge, Fame).
    /// Levelling cost: XpForLevel(n) = n * 1000  (linear, max level 100).
    /// Cumulative XP to reach level L = 1000 * (L-1) * L / 2.
    /// </summary>
    public static class JobXpService
    {
        public const int MaxLevel = 100;

        /// <summary>XP required to advance from level <paramref name="level"/> to the next.</summary>
        public static long XpForLevel(int level)
            => level >= MaxLevel ? long.MaxValue : level * 1000L;

        /// <summary>Cumulative XP required to reach exactly level <paramref name="level"/>.</summary>
        public static long TotalXpToReach(int level)
        {
            if (level <= 1) return 0;
            long n = level - 1;
            return 1000L * n * (n + 1) / 2;
        }

        /// <summary>Current level (1–100) derived from total accumulated XP.</summary>
        public static int GetLevel(long totalXp)
        {
            int level = 1;
            while (level < MaxLevel && totalXp >= TotalXpToReach(level + 1))
                level++;
            return level;
        }

        /// <summary>XP earned within the current level.</summary>
        public static long XpInCurrentLevel(long totalXp)
            => totalXp - TotalXpToReach(GetLevel(totalXp));

        /// <summary>XP required to complete the current level.</summary>
        public static long XpForCurrentLevel(long totalXp)
        {
            int level = GetLevel(totalXp);
            return level >= MaxLevel ? 1 : XpForLevel(level);
        }

        /// <summary>Progress fraction (0.0–1.0) within the current level.</summary>
        public static double GetProgressFraction(long totalXp)
        {
            if (GetLevel(totalXp) >= MaxLevel) return 1.0;
            return (double)XpInCurrentLevel(totalXp) / XpForCurrentLevel(totalXp);
        }

        /// <summary>
        /// Sell-value multiplier from Fame level.
        /// Scales linearly from x1.0 (level 1, +0%) to x2.5 (level 100, +150%).
        /// </summary>
        public static double GetFameMultiplier(int level)
        {
            int clamped = Math.Clamp(level, 1, MaxLevel);
            return 1.0 + (clamped - 1) * (1.5 / (MaxLevel - 1));
        }

        /// <summary>Fame sell multiplier derived from accumulated Fame XP.</summary>
        public static double GetFameMultiplierFromXp(long fameXp)
            => GetFameMultiplier(GetLevel(fameXp));

        /// <summary>"1,200 / 3,400 XP" progress string, or "MAX" at level 100.</summary>
        public static string FormatProgress(long totalXp)
        {
            if (GetLevel(totalXp) >= MaxLevel) return "MAX";
            return $"{XpInCurrentLevel(totalXp):N0} / {XpForCurrentLevel(totalXp):N0} XP";
        }

        /// <summary>
        /// Multiplier applied to gathered amounts (Gathering jobs) or crafted item stats (Crafting jobs).
        /// Scales linearly from x1.0 at level 1 to x3.0 at level 100.
        /// </summary>
        public static double GetSkillMultiplier(int level)
        {
            int clamped = Math.Clamp(level, 1, MaxLevel);
            return 1.0 + (clamped - 1) * (2.0 / (MaxLevel - 1));
        }

        /// <summary>Skill multiplier derived from accumulated XP rather than a precomputed level.</summary>
        public static double GetSkillMultiplierFromXp(long totalXp)
            => GetSkillMultiplier(GetLevel(totalXp));
    }
}
