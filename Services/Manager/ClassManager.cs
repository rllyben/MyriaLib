using MyriaLib.Entities.Characters;
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

        public static IEnumerable<CharacterClass> GetAllowedClasses(CharacterRace race)
        {
            var forbidden = RaceProfile.All.TryGetValue(race, out var profile)
                ? profile.ForbiddenClasses
                : new HashSet<CharacterClass>();

            return Enum.GetValues<CharacterClass>().Where(c => !forbidden.Contains(c));
        }

        public static ClassGroup GetClassGroup(CharacterClass cls) => cls switch
        {
            CharacterClass.Fighter or CharacterClass.Knight   or CharacterClass.Barbarian                  => ClassGroup.Physical,
            CharacterClass.Archer  or CharacterClass.Hunter   or CharacterClass.Rogue                      => ClassGroup.RangerRogue,
            CharacterClass.ElementalMage or CharacterClass.ArcanMage or CharacterClass.RunicMage           => ClassGroup.Mage,
            CharacterClass.Cleric  or CharacterClass.SoulsKnight or CharacterClass.Druid                   => ClassGroup.DivineHybrid,
            _                                                                                      => ClassGroup.Physical
        };

        public static bool IsClassAllowed(CharacterRace race, CharacterClass cls)
        {
            if (!RaceProfile.All.TryGetValue(race, out var profile)) return true;
            return !profile.ForbiddenClasses.Contains(cls);
        }

        public static long GetClassXp(Character character, CharacterClass cls)
            => character.ClassXp.TryGetValue(cls, out var xp) ? xp : 0L;

        public static int GetClassLevel(Character character, CharacterClass cls)
            => ClassXpService.GetLevel(GetClassXp(character, cls));

        public static void GrantClassXp(Character character, long amount)
            => GrantClassXp(character, character.Class, amount);

        public static void GrantClassXp(Character character, CharacterClass cls, long amount)
        {
            if (amount <= 0) return;
            character.ClassXp[cls] = GetClassXp(character, cls) + amount;
        }

        /// <summary>
        /// Subtracts <see cref="PenaltyPerDay"/> XP from every class except the character's active
        /// one. Safe to call multiple times per day — skips if already applied today.
        /// </summary>
        public static void ApplyDailyPenalty(Character character)
        {
            var today = DateTime.UtcNow.Date;
            if (character.LastClassPenaltyApplied.Date >= today) return;
            character.LastClassPenaltyApplied = today;

            foreach (CharacterClass cls in Enum.GetValues<CharacterClass>())
            {
                if (cls == character.Class) continue;
                long current = GetClassXp(character, cls);
                if (current <= 0) continue;
                character.ClassXp[cls] = Math.Max(0, current - PenaltyPerDay);
            }
        }

        /// <summary>Returns true when the character is allowed to switch to a new class.</summary>
        public static bool CanChangeClass(Character character)
            => GetClassChangeCooldownRemaining(character) <= TimeSpan.Zero;

        /// <summary>How long until the character may switch classes again; zero or negative means ready.</summary>
        public static TimeSpan GetClassChangeCooldownRemaining(Character character)
            => character.LastClassChanged == DateTime.MinValue
                ? TimeSpan.Zero
                : character.LastClassChanged + ClassChangeCooldown - DateTime.UtcNow;

        /// <summary>
        /// Changes the character's active class.
        /// Returns false if the class is forbidden for their race or the 7-day cooldown has not elapsed.
        /// Switching to the already-active class is a no-op (returns true, no cooldown consumed).
        /// </summary>
        public static bool SetClass(Character character, CharacterClass cls)
        {
            if (!IsClassAllowed(character.Race, cls)) return false;
            if (cls == character.Class) return true;
            if (!CanChangeClass(character)) return false;

            var oldClass = character.Class;

            // Remove base skills that belong to the outgoing class
            var removedBaseIds = new HashSet<string>(
                character.Skills.Where(s => s.Class == oldClass).Select(s => s.Id));
            character.Skills.RemoveAll(s => s.Class == oldClass);

            // Collect combined/composite IDs before stashing (for slot cleanup)
            var oldCombinedIds   = new HashSet<string>(character.CombinedSkills.Select(c => c.Id));
            var oldCompositeIds  = new HashSet<string>(character.CompositeSkills.Select(c => c.Id));

            // Stash combined + composite skills under the old class
            character.StashedCombinedSkills[oldClass]  = character.CombinedSkills.ToList();
            character.StashedCompositeSkills[oldClass] = character.CompositeSkills.ToList();
            character.CombinedSkills.Clear();
            character.CompositeSkills.Clear();
            character.ActiveCompositeSkillIds.Clear();

            // Drop all skill-bar slots that referenced old-class skills
            character.SkillSlots.RemoveAll(slot =>
                (slot.Source == SlottedSkillSource.Regular         && removedBaseIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.Combined        && oldCombinedIds.Contains(slot.SkillId))    ||
                (slot.Source == SlottedSkillSource.CompositeFusion && oldCompositeIds.Contains(slot.SkillId)));

            character.Class = cls;
            character.LastClassChanged = DateTime.UtcNow;

            // In-group XP transfer: same group → new class gains 50 % of old class XP
            if (GetClassGroup(oldClass) == GetClassGroup(cls))
            {
                long transfer = GetClassXp(character, oldClass) / 2;
                if (transfer > 0)
                    character.ClassXp[cls] = GetClassXp(character, cls) + transfer;
            }

            // Restore combined + composite skills previously stashed for the new class
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

            // Grant base skills for the new class
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
