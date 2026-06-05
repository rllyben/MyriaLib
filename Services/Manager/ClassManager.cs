using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public static class ClassManager
    {
        public static long     PenaltyPerDay        { get; set; } = 500L;
        public static TimeSpan ClassChangeCooldown  { get; set; } = TimeSpan.FromDays(7);

        public static IEnumerable<PlayerClass> GetAllowedClasses(PlayerRace race)
        {
            var forbidden = RaceProfile.All.TryGetValue(race, out var profile)
                ? profile.ForbiddenClasses
                : new HashSet<PlayerClass>();

            return Enum.GetValues<PlayerClass>().Where(c => !forbidden.Contains(c));
        }

        public static ClassGroup GetClassGroup(PlayerClass cls) => cls switch
        {
            PlayerClass.Fighter or PlayerClass.Knight   or PlayerClass.Barbarian                  => ClassGroup.Physical,
            PlayerClass.Archer  or PlayerClass.Hunter   or PlayerClass.Rogue                      => ClassGroup.RangerRogue,
            PlayerClass.ElementalMage or PlayerClass.ArcanMage or PlayerClass.RunicMage           => ClassGroup.Mage,
            PlayerClass.Cleric  or PlayerClass.SoulsKnight or PlayerClass.Druid                   => ClassGroup.DivineHybrid,
            _                                                                                      => ClassGroup.Physical
        };

        public static bool IsClassAllowed(PlayerRace race, PlayerClass cls)
        {
            if (!RaceProfile.All.TryGetValue(race, out var profile)) return true;
            return !profile.ForbiddenClasses.Contains(cls);
        }

        public static long GetClassXp(Player player, PlayerClass cls)
            => player.ClassXp.TryGetValue(cls, out var xp) ? xp : 0L;

        public static int GetClassLevel(Player player, PlayerClass cls)
            => ClassXpService.GetLevel(GetClassXp(player, cls));

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

        /// <summary>Returns true when the player is allowed to switch to a new class.</summary>
        public static bool CanChangeClass(Player player)
            => GetClassChangeCooldownRemaining(player) <= TimeSpan.Zero;

        /// <summary>How long until the player may switch classes again; zero or negative means ready.</summary>
        public static TimeSpan GetClassChangeCooldownRemaining(Player player)
            => player.LastClassChanged == DateTime.MinValue
                ? TimeSpan.Zero
                : player.LastClassChanged + ClassChangeCooldown - DateTime.UtcNow;

        /// <summary>
        /// Changes the player's active class.
        /// Returns false if the class is forbidden for their race or the 7-day cooldown has not elapsed.
        /// Switching to the already-active class is a no-op (returns true, no cooldown consumed).
        /// </summary>
        public static bool SetClass(Player player, PlayerClass cls)
        {
            if (!IsClassAllowed(player.Race, cls)) return false;
            if (cls == player.Class) return true;
            if (!CanChangeClass(player)) return false;

            var oldClass = player.Class;

            // Remove base skills that belong to the outgoing class
            var removedBaseIds = new HashSet<string>(
                player.Skills.Where(s => s.Class == oldClass).Select(s => s.Id));
            player.Skills.RemoveAll(s => s.Class == oldClass);

            // Collect combined/composite IDs before stashing (for slot cleanup)
            var oldCombinedIds   = new HashSet<string>(player.CombinedSkills.Select(c => c.Id));
            var oldCompositeIds  = new HashSet<string>(player.CompositeSkills.Select(c => c.Id));

            // Stash combined + composite skills under the old class
            player.StashedCombinedSkills[oldClass]  = player.CombinedSkills.ToList();
            player.StashedCompositeSkills[oldClass] = player.CompositeSkills.ToList();
            player.CombinedSkills.Clear();
            player.CompositeSkills.Clear();
            player.ActiveCompositeSkillIds.Clear();

            // Drop all skill-bar slots that referenced old-class skills
            player.SkillSlots.RemoveAll(slot =>
                (slot.Source == SlottedSkillSource.Regular         && removedBaseIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.Combined        && oldCombinedIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.CompositeFusion && oldCompositeIds.Contains(slot.SkillId)));

            player.Class = cls;
            player.LastClassChanged = DateTime.UtcNow;

            // In-group XP transfer: same group → new class gains 50 % of old class XP
            if (GetClassGroup(oldClass) == GetClassGroup(cls))
            {
                long transfer = GetClassXp(player, oldClass) / 2;
                if (transfer > 0)
                    player.ClassXp[cls] = GetClassXp(player, cls) + transfer;
            }

            // Restore combined + composite skills previously stashed for the new class
            if (player.StashedCombinedSkills.TryGetValue(cls, out var stashedCombined))
            {
                player.CombinedSkills.AddRange(stashedCombined);
                player.StashedCombinedSkills.Remove(cls);
            }
            if (player.StashedCompositeSkills.TryGetValue(cls, out var stashedComposite))
            {
                player.CompositeSkills.AddRange(stashedComposite);
                player.StashedCompositeSkills.Remove(cls);
            }

            // Grant base skills for the new class
            SkillFactory.UpdateSkills(player);

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
