using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Models.BaseModel;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    /// <summary>
    /// Fuses a list of base skill components into a composite <see cref="Skill"/>.
    /// Uses algorithmic rules to derive stats and targeting, with named recipe overrides
    /// for specific combinations that need a hand-crafted identity or rebalanced values.
    /// </summary>
    public static class SkillFusionSystem
    {
        // ── Tuneable constants ────────────────────────────────────────────────────

        /// <summary>ScalingFactor added per component beyond the first.</summary>
        public static float ScalingPerComponent { get; set; } = 0.3f;

        /// <summary>How many stacks of the same base skill are needed to promote targeting to AllEnemies.</summary>
        public static int AoeStackThreshold { get; set; } = 3;

        /// <summary>MP cost multiplier applied per component (multiplicative stacking).</summary>
        public static float ManaCostMultiplierPerComponent { get; set; } = 0.6f;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fuses the given base skill components into a composite skill.
        /// Returns <c>null</c> if <paramref name="components"/> is empty or all IDs are unknown.
        /// </summary>
        public static Skill? Fuse(IReadOnlyList<BaseSkillData> components)
        {
            if (components == null || components.Count == 0) return null;

            // Collect all component types from all inputs
            var allTypes = components
                .SelectMany(c => c.ComponentType
                    .Select(t => Enum.TryParse<SkillComponentType>(t, ignoreCase: true, out var parsed)
                        ? (SkillComponentType?)parsed : null)
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value))
                .ToList();

            // Derive stats algorithmically
            float scalingFactor  = components.Max(c => c.ScalingFactor)
                                 + (components.Count - 1) * ScalingPerComponent;
            string statToScale   = components.OrderByDescending(c => c.ScalingFactor).First().StatToScaleFrom;
            bool   isHealing     = allTypes.Contains(SkillComponentType.Heal);
            var    target        = DeriveTarget(components, allTypes);
            var    type          = allTypes.Contains(SkillComponentType.Magic)
                                     ? SkillType.Magical : SkillType.Physical;

            // Mana cost: base from highest-cost component, each additional raises it by a fraction
            int manaCost = components.Max(c => c.ManaCost);
            for (int i = 1; i < components.Count; i++)
                manaCost += (int)(components[i].ManaCost * ManaCostMultiplierPerComponent);

            string id   = "fusion_" + string.Join("_", components.Select(c => c.Id).OrderBy(x => x));
            string name = BuildFusionName(components);
            string desc = $"A fusion of {string.Join(", ", components.Select(c => c.Name))}.";

            var skill = new Skill
            {
                Id              = id,
                Name            = name,
                Description     = desc,
                ManaCost        = manaCost,
                ScalingFactor   = scalingFactor,
                StatToScaleFrom = statToScale,
                Target          = target,
                IsHealing       = isHealing,
                Type            = type,
                MinLevel        = components.Max(c => c.RequiredLevel),
            };

            // Apply named recipe override (identity + optional stat overrides)
            ApplyRecipeOverride(skill, components);

            return skill;
        }

        /// <summary>
        /// Creates a <see cref="CompositeSkill"/>, fuses it, and adds it to the player's collection
        /// if they have a free slot. Returns the new composite, or <c>null</c> if the slot cap is reached
        /// or fusion produced no result.
        /// </summary>
        public static CompositeSkill? TryCreateForPlayer(Player player, IReadOnlyList<BaseSkillData> components)
        {
            if (player.ActiveCompositeSkillIds.Count >= player.FusionSlotCount)
                return null;

            var resolved = Fuse(components);
            if (resolved == null) return null;

            var composite = new CompositeSkill
            {
                ComponentIds  = components.Select(c => c.Id).ToList(),
                ResolvedSkill = resolved
            };

            player.CompositeSkills.Add(composite);
            player.ActiveCompositeSkillIds.Add(composite.Id);
            return composite;
        }

        /// <summary>
        /// Re-resolves all <see cref="CompositeSkill"/> entries for a player after loading from save.
        /// Call once after deserializing a player — populates <see cref="CompositeSkill.ResolvedSkill"/>.
        /// </summary>
        public static void ResolveCompositeSkills(Player player)
        {
            foreach (var composite in player.CompositeSkills)
            {
                var components = composite.ComponentIds
                    .Select(id => BaseSkillLoader.Get(id))
                    .Where(c => c != null)
                    .Select(c => c!)
                    .ToList();

                if (components.Count == 0)
                {
                    GameLog.Error($"Player '{player.Name}': composite skill '{composite.Id}' has no resolvable components.");
                    continue;
                }

                composite.ResolvedSkill = Fuse(components);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static SkillTarget DeriveTarget(
            IReadOnlyList<BaseSkillData> components,
            List<SkillComponentType> allTypes)
        {
            // Explicit Self component → Self
            if (allTypes.Contains(SkillComponentType.Self))
                return SkillTarget.Self;

            // Area or MultiHit component → AllEnemies
            if (allTypes.Contains(SkillComponentType.Area)
                || allTypes.Contains(SkillComponentType.MultiHit))
                return SkillTarget.AllEnemies;

            // Stacking the same base skill enough times → AllEnemies
            bool hasAoeStack = components
                .GroupBy(c => c.Id)
                .Any(g => g.Count() >= AoeStackThreshold);
            if (hasAoeStack)
                return SkillTarget.AllEnemies;

            return SkillTarget.SingleEnemy;
        }

        private static string BuildFusionName(IReadOnlyList<BaseSkillData> components)
        {
            // If all components are the same skill, prefix with "Grand"
            if (components.Select(c => c.Id).Distinct().Count() == 1)
                return $"Grand {components[0].Name}";

            // Otherwise join names
            return string.Join(" ", components.Select(c => c.Name));
        }

        private static void ApplyRecipeOverride(Skill skill, IReadOnlyList<BaseSkillData> components)
        {
            var recipe = FusionRecipeService.FindRecipe(components.Select(c => c.Id).ToList());
            if (recipe == null) return;

            skill.Id          = recipe.ResultId;
            skill.Name        = recipe.ResultName;
            skill.Description = recipe.ResultDescription;

            if (recipe.ScalingFactorOverride.HasValue)
                skill.ScalingFactor = recipe.ScalingFactorOverride.Value;
            if (recipe.ManaCostOverride.HasValue)
                skill.ManaCost = recipe.ManaCostOverride.Value;
            if (!string.IsNullOrEmpty(recipe.TargetOverride)
                && Enum.TryParse<SkillTarget>(recipe.TargetOverride, ignoreCase: true, out var targetOverride))
                skill.Target = targetOverride;
        }
    }
}
