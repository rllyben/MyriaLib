using MyriaLib.Entities.Players;
using MyriaLib.Services;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public static class ClassManager
    {
        private const long PenaltyPerDay = 500L;

        public static IEnumerable<PlayerClass> GetAllowedClasses(PlayerRace race)
        {
            var forbidden = RaceProfile.All.TryGetValue(race, out var profile)
                ? profile.ForbiddenClasses
                : new HashSet<PlayerClass>();

            return Enum.GetValues<PlayerClass>().Where(c => !forbidden.Contains(c));
        }

        public static bool IsClassAllowed(PlayerRace race, PlayerClass cls)
        {
            if (!RaceProfile.All.TryGetValue(race, out var profile)) return true;
            return !profile.ForbiddenClasses.Contains(cls);
        }

        public static long GetClassXp(Player player, PlayerClass cls)
            => player.ClassXp.TryGetValue(cls, out var xp) ? xp : 0L;

        public static int GetClassLevel(Player player, PlayerClass cls)
            => JobXpService.GetLevel(GetClassXp(player, cls));

        public static void GrantClassXp(Player player, long amount)
            => GrantClassXp(player, player.Class, amount);

        public static void GrantClassXp(Player player, PlayerClass cls, long amount)
        {
            if (amount <= 0) return;
            player.ClassXp[cls] = GetClassXp(player, cls) + amount;
        }

        /// <summary>
        /// Subtracts <see cref="PenaltyPerDay"/> XP from every class except the player's active
        /// one. Safe to call multiple times per day — skips if already applied today.
        /// </summary>
        public static void ApplyDailyPenalty(Player player)
        {
            var today = DateTime.UtcNow.Date;
            if (player.LastClassPenaltyApplied.Date >= today) return;
            player.LastClassPenaltyApplied = today;

            foreach (PlayerClass cls in Enum.GetValues<PlayerClass>())
            {
                if (cls == player.Class) continue;
                long current = GetClassXp(player, cls);
                if (current <= 0) continue;
                player.ClassXp[cls] = Math.Max(0, current - PenaltyPerDay);
            }
        }

        /// <summary>
        /// Changes the player's active class. Returns false if the class is forbidden for their race.
        /// </summary>
        public static bool SetClass(Player player, PlayerClass cls)
        {
            if (!IsClassAllowed(player.Race, cls)) return false;
            player.Class = cls;
            return true;
        }

        public static int GetClassBonusForStat(Player player, string stat)
        {
            if (!ClassProfile.All.TryGetValue(player.Class, out var profile)) return 0;
            int level = GetClassLevel(player, player.Class);
            return profile.StatGrowth.TryGetValue(stat, out var growth) ? growth * level : 0;
        }

        public static int GetClassHpBonus(Player player)
        {
            if (!ClassProfile.All.TryGetValue(player.Class, out var profile)) return 0;
            return profile.HpPerLevel * GetClassLevel(player, player.Class);
        }

        public static int GetClassManaBonus(Player player)
        {
            if (!ClassProfile.All.TryGetValue(player.Class, out var profile)) return 0;
            return profile.ManaPerLevel * GetClassLevel(player, player.Class);
        }
    }
}
