namespace MyriaLib.Services
{
    /// <summary>
    /// XP and level calculations for class levels.
    /// Levelling cost: XpForLevel(n) = n × 5000 (5× steeper than job aspects, max level 50).
    /// Cumulative XP to reach level L = 5000 × (L-1) × L / 2.
    /// </summary>
    public static class ClassXpService
    {
        public const int MaxLevel = 50;

        /// <summary>XP required to advance from <paramref name="level"/> to the next.</summary>
        public static long XpForLevel(int level)
            => level >= MaxLevel ? long.MaxValue : level * 5000L;

        /// <summary>Cumulative XP required to reach exactly level <paramref name="level"/>.</summary>
        public static long TotalXpToReach(int level)
        {
            if (level <= 1) return 0;
            long n = level - 1;
            return 5000L * n * (n + 1) / 2;
        }

        /// <summary>Current class level (1–50) derived from total accumulated XP.</summary>
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

        /// <summary>"1,200 / 5,000 XP" progress string, or "MAX" at level 50.</summary>
        public static string FormatProgress(long totalXp)
        {
            if (GetLevel(totalXp) >= MaxLevel) return "MAX";
            return $"{XpInCurrentLevel(totalXp):N0} / {XpForCurrentLevel(totalXp):N0} XP";
        }
    }
}
