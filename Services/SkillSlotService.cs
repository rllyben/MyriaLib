using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Services.Builder;

namespace MyriaLib.Services
{
    public static class SkillSlotService
    {
        /// <summary>
        /// Re-populates <see cref="SkillSlot.ResolvedSkill"/> for all slots after a save load.
        /// Must be called after <c>SkillCombinationService.ResolveCombinedSkills</c>.
        /// </summary>
        public static void ResolveSlots(Player player)
        {
            foreach (var slot in player.SkillSlots)
                slot.ResolvedSkill = Resolve(player, slot);
        }

        /// <summary>
        /// If the player has no skill slots yet (new system, existing save), auto-fills up to
        /// <see cref="Player.SkillSlotCount"/> with the player's first learned regular skills.
        /// This is a no-op if <c>SkillSlots</c> is already populated.
        /// </summary>
        public static void MigrateIfEmpty(Player player)
        {
            if (player.SkillSlots.Count > 0) return;

            foreach (var skill in player.Skills.Take(player.SkillSlotCount))
            {
                player.SkillSlots.Add(new SkillSlot
                {
                    Source = SlottedSkillSource.Regular,
                    SkillId = skill.Id,
                    ResolvedSkill = skill
                });
            }
        }

        /// <summary>
        /// Adds a skill to the player's combat slots. Returns false if the cap is reached,
        /// the skill is already slotted, or the referenced skill cannot be found.
        /// </summary>
        public static bool TryAddSlot(Player player, SlottedSkillSource source, string skillId)
        {
            if (player.SkillSlots.Count >= player.SkillSlotCount) return false;
            if (player.SkillSlots.Any(s => s.Source == source && s.SkillId == skillId)) return false;

            var resolved = ResolveById(player, source, skillId);
            if (resolved == null) return false;

            player.SkillSlots.Add(new SkillSlot
            {
                Source = source,
                SkillId = skillId,
                ResolvedSkill = resolved
            });
            return true;
        }

        /// <summary>Removes the matching slot. Returns false if not found.</summary>
        public static bool RemoveSlot(Player player, SlottedSkillSource source, string skillId)
        {
            var slot = player.SkillSlots.FirstOrDefault(s => s.Source == source && s.SkillId == skillId);
            if (slot == null) return false;
            player.SkillSlots.Remove(slot);
            return true;
        }

        /// <summary>Moves the slot at <paramref name="fromIndex"/> to <paramref name="toIndex"/>.</summary>
        public static void ReorderSlots(Player player, int fromIndex, int toIndex)
        {
            var slots = player.SkillSlots;
            if (fromIndex < 0 || fromIndex >= slots.Count) return;
            if (toIndex < 0 || toIndex >= slots.Count) return;
            if (fromIndex == toIndex) return;

            var item = slots[fromIndex];
            slots.RemoveAt(fromIndex);
            slots.Insert(toIndex, item);
        }

        /// <summary>
        /// Returns the resolved skills for all active slots in order.
        /// Slots whose skill could not be resolved are skipped.
        /// </summary>
        public static IEnumerable<(Skill Skill, SlottedSkillSource Source)> GetCombatSkills(Player player)
        {
            foreach (var slot in player.SkillSlots)
            {
                var skill = slot.ResolvedSkill ?? Resolve(player, slot);
                if (skill != null)
                    yield return (skill, slot.Source);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static Skill? Resolve(Player player, SkillSlot slot) =>
            ResolveById(player, slot.Source, slot.SkillId);

        private static Skill? ResolveById(Player player, SlottedSkillSource source, string skillId) =>
            source switch
            {
                SlottedSkillSource.Regular =>
                    player.Skills.FirstOrDefault(s => s.Id == skillId),
                SlottedSkillSource.Combined =>
                    player.CombinedSkills.FirstOrDefault(c => c.Id == skillId)?.ResolvedSkill,
                SlottedSkillSource.CompositeFusion =>
                    player.CompositeSkills.FirstOrDefault(c => c.Id == skillId)?.ResolvedSkill,
                _ => null
            };
    }
}
