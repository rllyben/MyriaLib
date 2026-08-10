using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;

namespace Myria.Lib.Core.Services.Manager
{
    public static class ClassManager
    {
        public static long     PenaltyPerDay        { get; set; } = 500L;
        public static TimeSpan ClassChangeCooldown  { get; set; } = TimeSpan.FromDays(7);

        /// <summary>Returns all class IDs that are not forbidden for the given race.</summary>
        public static IEnumerable<string> GetAllowedClasses(string race)
        {
            var forbidden = RaceProfile.All.TryGetValue(race, out var profile)
                ? profile.ForbiddenClasses
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return ClassProfile.All.Keys.Where(c => !forbidden.Contains(c));
        }

        /// <summary>
        /// Returns the group name for a class, read from its ClassProfile.
        /// Defaults to "Physical" if the class has no registered profile.
        /// </summary>
        public static string GetClassGroup(string cls)
            => ClassProfile.All.TryGetValue(cls, out var p) ? p.Group : "Physical";

        public static bool IsClassAllowed(string race, string cls)
        {
            if (!RaceProfile.All.TryGetValue(race, out var profile)) return true;
            return !profile.ForbiddenClasses.Contains(cls);
        }

        public static long GetClassXp(Character character, string cls)
            => character.ClassXp.TryGetValue(cls, out var xp) ? xp : 0L;

        public static int GetClassLevel(Character character, string cls)
            => ClassXpService.GetLevel(GetClassXp(character, cls));

        public static void GrantClassXp(Character character, long amount)
            => GrantClassXp(character, character.Class, amount);

        public static void GrantClassXp(Character character, string cls, long amount)
        {
            if (amount <= 0) return;
            character.ClassXp[cls] = GetClassXp(character, cls) + amount;
        }

        public static void ApplyDailyPenalty(Character character)
        {
            var today = DateTime.UtcNow.Date;
            if (character.LastClassPenaltyApplied.Date >= today) return;
            character.LastClassPenaltyApplied = today;

            foreach (var cls in ClassProfile.All.Keys)
            {
                if (cls.Equals(character.Class, StringComparison.OrdinalIgnoreCase)) continue;
                long current = GetClassXp(character, cls);
                if (current <= 0) continue;
                character.ClassXp[cls] = Math.Max(0, current - PenaltyPerDay);
            }
        }

        public static bool CanChangeClass(Character character)
            => GetClassChangeCooldownRemaining(character) <= TimeSpan.Zero;

        public static TimeSpan GetClassChangeCooldownRemaining(Character character)
            => character.LastClassChanged == DateTime.MinValue
                ? TimeSpan.Zero
                : character.LastClassChanged + ClassChangeCooldown - DateTime.UtcNow;

        public static bool SetClass(Character character, string cls)
        {
            if (!IsClassAllowed(character.Race, cls)) return false;
            if (cls.Equals(character.Class, StringComparison.OrdinalIgnoreCase)) return true;
            if (!CanChangeClass(character)) return false;

            var oldClass = character.Class;

            var removedBaseIds = new HashSet<string>(
                character.Skills.Where(s => s.Class.Equals(oldClass, StringComparison.OrdinalIgnoreCase))
                                 .Select(s => s.Id));
            character.Skills.RemoveAll(s => s.Class.Equals(oldClass, StringComparison.OrdinalIgnoreCase));

            var oldCombinedIds  = new HashSet<string>(character.CombinedSkills.Select(c => c.Id));
            var oldCompositeIds = new HashSet<string>(character.CompositeSkills.Select(c => c.Id));

            character.StashedCombinedSkills[oldClass]  = character.CombinedSkills.ToList();
            character.StashedCompositeSkills[oldClass] = character.CompositeSkills.ToList();
            character.CombinedSkills.Clear();
            character.CompositeSkills.Clear();
            character.ActiveCompositeSkillIds.Clear();

            character.SkillSlots.RemoveAll(slot =>
                (slot.Source == SlottedSkillSource.Regular         && removedBaseIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.Combined        && oldCombinedIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.CompositeFusion && oldCompositeIds.Contains(slot.SkillId)));

            character.Class = cls;
            character.LastClassChanged = DateTime.UtcNow;
            Myria.Lib.Core.Systems.GameEvents.FireClassChanged(character, oldClass, cls);

            // In-group XP transfer: same group → new class gains 50% of old class XP.
            if (GetClassGroup(oldClass).Equals(GetClassGroup(cls), StringComparison.OrdinalIgnoreCase))
            {
                long transfer = GetClassXp(character, oldClass) / 2;
                if (transfer > 0)
                    character.ClassXp[cls] = GetClassXp(character, cls) + transfer;
            }

            if (character.StashedCombinedSkills.TryGetValue(cls, out var stashedCombined))
            {
                character.CombinedSkills.AddRange(stashedCombined);
                character.StashedCombinedSkills.Remove(cls);
            }
            if (character.StashedCompositeSkills.TryGetValue(cls, out var stashedComposite))
            {
                character.CompositeSkills.AddRange(stashedComposite);
                character.StashedCompositeSkills.Remove(cls);
            }

            SkillFactory.UpdateSkills(character);
            return true;
        }

        public static int GetClassBonusForStat(Character character, string stat)
        {
            if (!ClassProfile.All.TryGetValue(character.Class, out var profile)) return 0;
            int level = GetClassLevel(character, character.Class);
            return profile.StatGrowth.TryGetValue(stat, out var growth) ? growth * level : 0;
        }

        public static int GetClassHpBonus(Character character)
        {
            if (!ClassProfile.All.TryGetValue(character.Class, out var profile)) return 0;
            return profile.HpPerLevel * GetClassLevel(character, character.Class);
        }

        public static int GetClassManaBonus(Character character)
        {
            if (!ClassProfile.All.TryGetValue(character.Class, out var profile)) return 0;
            return profile.ManaPerLevel * GetClassLevel(character, character.Class);
        }
    }
}
