using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Services
{
    public static class SkillCombinationService
    {
        private static Dictionary<string, SkillCombinationRecipe> _recipes = new();

        public static void Load(string path = "Data/common/skill_combinations.json")
        {
            if (!File.Exists(path))
            {
                GameLog.Error($"skill_combinations.json not found at '{path}' — no recipes loaded.");
                _recipes = new();
                return;
            }

            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<SkillCombinationRecipe>>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            if (list == null)
            {
                GameLog.Error("Failed to deserialize skill_combinations.json.");
                _recipes = new();
                return;
            }

            Load(list);
        }

        /// <summary>Loads skill combination recipes from already-parsed data (e.g. read from a database).</summary>
        public static void Load(List<SkillCombinationRecipe> list)
        {
            _recipes = list.ToDictionary(r => MakeKey(r.InputSkillIds));
        }

        /// <summary>
        /// Produces a sorted, joined key from a multiset of skill IDs so that
        /// [a, b, a] and [a, a, b] both resolve to the same key.
        /// </summary>
        public static string MakeKey(IEnumerable<string> ids) =>
            string.Join("|", ids.OrderBy(x => x));

        /// <summary>
        /// Combines 2–5 skill IDs into a new <see cref="Skill"/>.
        /// Uses an authored recipe if one exists, otherwise falls back to algorithmic merging.
        /// Returns null if fewer than 2 IDs are provided.
        /// </summary>
        public static Skill? Combine(List<string> skillIds)
        {
            if (skillIds == null || skillIds.Count < 2) return null;

            var skills = skillIds
                .Select(id => Builder.SkillFactory.GetSkill(id))
                .Where(s => s != null)
                .Select(s => s!)
                .ToList();

            if (skills.Count < 2) return null;

            // Authored recipe takes priority
            var key = MakeKey(skillIds);
            if (_recipes.TryGetValue(key, out var recipe))
                return BuildFromRecipe(recipe, skills);

            return BuildAlgorithmic(skillIds, skills);
        }

        private static Skill BuildFromRecipe(SkillCombinationRecipe recipe, List<Skill> inputs)
        {
            // Use the first input skill as the base to inherit class/unoverridden fields
            var dominant = inputs.OrderByDescending(s => s.ScalingFactor).First();

            return new Skill
            {
                Id              = recipe.ResultId,
                Name            = recipe.ResultName,
                Description     = recipe.ResultDescription,
                Class           = dominant.Class,
                ManaCost        = recipe.ManaCostOverride ?? (int)(inputs.Sum(s => s.ManaCost) * 0.8f),
                Type            = recipe.TypeOverride != null
                                    ? Enum.Parse<SkillType>(recipe.TypeOverride)
                                    : dominant.Type,
                Target          = recipe.TargetOverride ?? dominant.Target,
                ScalingFactor   = recipe.ScalingFactorOverride ?? inputs.Average(s => s.ScalingFactor) * 1.1f,
                StatToScaleFrom = recipe.StatToScaleFromOverride ?? dominant.StatToScaleFrom,
                IsHealing       = recipe.IsHealingOverride ?? inputs.Any(s => s.IsHealing),
                MinLevel        = 1,
                Effects         = recipe.Effects ?? new(),
                AggroModifier   = recipe.AggroModifier,
            };
        }

        private static Skill BuildAlgorithmic(List<string> skillIds, List<Skill> inputs)
        {
            var dominant = inputs.OrderByDescending(s => s.ScalingFactor).First();

            // If any input is AoE the result is AoE, otherwise keep dominant target
            var target = inputs.Any(s => s.Target == SkillTarget.AllEnemies)
                ? SkillTarget.AllEnemies
                : dominant.Target;

            // AoE combinations get a scaling penalty
            float scaling = target == SkillTarget.AllEnemies
                ? inputs.Average(s => s.ScalingFactor) * 0.9f
                : inputs.Average(s => s.ScalingFactor) * 1.1f;

            // Mana cost = sum * 0.8 (discount for combining)
            int mana = (int)(inputs.Sum(s => s.ManaCost) * 0.8f);

            string name = inputs.Count == 2
                ? $"{inputs[0].Name.Split(' ')[0]} {inputs[1].Name.Split(' ')[0]}"
                : $"Combined ({inputs.Count})";

            return new Skill
            {
                Id              = $"combined_{MakeKey(skillIds).Replace("|", "_")}",
                Name            = name,
                Description     = $"A combination of {string.Join(", ", inputs.Select(s => s.Name))}.",
                Class           = dominant.Class,
                ManaCost        = mana,
                Type            = dominant.Type,
                Target          = target,
                ScalingFactor   = scaling,
                StatToScaleFrom = dominant.StatToScaleFrom,
                IsHealing       = inputs.Any(s => s.IsHealing),
                MinLevel        = 1
            };
        }

        /// <summary>
        /// Creates a combined skill for the character from the given skill IDs (2–5, duplicates allowed).
        /// Returns the new <see cref="CombinedSkill"/> on success, or null on failure.
        /// Does NOT prevent using the same skill ID more than once in the input list.
        /// </summary>
        public static CombinedSkill? TryCreateForCharacter(Character character, List<string> skillIds)
        {
            if (skillIds == null || skillIds.Count < 2 || skillIds.Count > 5)
                return null;

            // Verify all referenced skills exist in the character's learned skills
            foreach (var id in skillIds)
            {
                if (!character.Skills.Any(s => s.Id == id))
                    return null;
            }

            // Block exact duplicate combinations (same multiset already created)
            var key = MakeKey(skillIds);
            if (character.CombinedSkills.Any(c => MakeKey(c.SkillIds) == key))
                return null;

            var resolved = Combine(skillIds);
            if (resolved == null) return null;

            var combined = new CombinedSkill
            {
                SkillIds = skillIds.OrderBy(x => x).ToList(),
                ResolvedSkill = resolved
            };

            character.CombinedSkills.Add(combined);
            return combined;
        }

        /// <summary>
        /// Re-populates <see cref="CombinedSkill.ResolvedSkill"/> for all entries after a save load.
        /// Must be called before <c>SkillSlotService.ResolveSlots</c>.
        /// </summary>
        public static void ResolveCombinedSkills(Character character)
        {
            foreach (var combined in character.CombinedSkills)
                combined.ResolvedSkill = Combine(combined.SkillIds);
        }
    }
}
