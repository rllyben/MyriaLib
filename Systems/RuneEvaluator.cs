using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Services;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    /// <summary>
    /// Evaluates a <see cref="CompositeRune"/> (base rune + added words) into a combat-ready <see cref="Skill"/>.
    /// Also detects Transform combinations that unlock new runes for the player.
    /// </summary>
    public static class RuneEvaluator
    {
        // ── Tuneable constants ────────────────────────────────────────────────────

        /// <summary>ScalingFactor added per Support word pair.</summary>
        public static float SupportBonus { get; set; } = 0.25f;

        /// <summary>Flat damage bonus added per Contradiction word pair (applied as a ScalingFactor offset).</summary>
        public static float ContradictionBonus { get; set; } = 0.20f;

        /// <summary>MP cost added per Neutral word (for each word that contributed at least one Neutral pair).</summary>
        public static int NeutralMpPenalty { get; set; } = 15;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a rune instance into a Skill.
        /// Pass the <see cref="BaseRuneData"/> definition and the resolved <see cref="RuneWord"/> objects.
        /// Returns <c>null</c> only if <paramref name="baseRune"/> is null.
        /// </summary>
        public static Skill? Evaluate(BaseRuneData baseRune, IReadOnlyList<RuneWord> addedWords)
        {
            if (baseRune == null) return null;

            // Build the full word set: core word + added words
            var coreWord   = RuneWordService.GetWord(baseRune.CoreWordId);
            var allWordIds = new List<string>();
            if (coreWord != null) allWordIds.Add(coreWord.Id);
            allWordIds.AddRange(addedWords.Select(w => w.Id));

            // Evaluate all unique pairs
            var pairs = GetAllPairs(allWordIds);

            float   scalingFactor = baseRune.BaseScalingFactor;
            int     manaCost      = baseRune.BaseManaCost;
            var     target        = Enum.Parse<SkillTarget>(baseRune.Target, ignoreCase: true);
            bool    isAoe         = false;

            int supportCount       = 0;
            int contradictionCount = 0;
            // Words that have at least one Neutral pair (to apply per-word MP penalty once)
            var neutralWords = new HashSet<string>();

            foreach (var (idA, idB) in pairs)
            {
                var rel = RuneWordService.GetRelationship(idA, idB);
                switch (rel)
                {
                    case WordRelationship.Support:
                        supportCount++;
                        break;

                    case WordRelationship.Contradiction:
                        contradictionCount++;
                        break;

                    case WordRelationship.Neutral:
                        neutralWords.Add(idA);
                        neutralWords.Add(idB);
                        break;

                    // Transform is handled separately below — skip here
                }
            }

            // Apply modifiers
            scalingFactor += supportCount       * SupportBonus;
            scalingFactor += contradictionCount * ContradictionBonus;
            manaCost      += neutralWords.Count * NeutralMpPenalty;

            // Check for AoE via specific word tags (words whose family implies area)
            isAoe = addedWords.Any(w => IsAreaWord(w));
            if (isAoe) target = SkillTarget.AllEnemies;

            // Build the result skill
            return new Skill
            {
                Id              = $"rune_{baseRune.Id}_{string.Join("_", addedWords.Select(w => w.Id))}",
                Name            = BuildName(baseRune, addedWords),
                Description     = baseRune.Description,
                ManaCost        = manaCost,
                ScalingFactor   = scalingFactor,
                StatToScaleFrom = baseRune.StatToScaleFrom,
                Target          = target,
                IsHealing       = baseRune.IsHealing,
                Type            = SkillType.Magical,
            };
        }

        /// <summary>
        /// Checks all word pairs for Transform combinations.
        /// Returns the IDs of new base runes to unlock, or an empty list if none.
        /// The caller is responsible for adding those runes to the player's collection.
        /// </summary>
        public static List<string> FindTransforms(IReadOnlyList<string> allWordIds)
        {
            var results = new List<string>();
            foreach (var (idA, idB) in GetAllPairs(allWordIds.ToList()))
            {
                var transformId = RuneWordService.GetTransformResult(idA, idB);
                if (transformId != null && !results.Contains(transformId))
                    results.Add(transformId);
            }
            return results;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static IEnumerable<(string, string)> GetAllPairs(List<string> ids)
        {
            for (int i = 0; i < ids.Count; i++)
            for (int j = i + 1; j < ids.Count; j++)
                yield return (ids[i], ids[j]);
        }

        /// <summary>
        /// Words in the "area" family (ocean, storm, etc.) promote AoE targeting.
        /// The family ID "area" is a convention — the JSON data must use this id.
        /// </summary>
        private static bool IsAreaWord(RuneWord word) =>
            word.FamilyId.Equals("area", StringComparison.OrdinalIgnoreCase);

        private static string BuildName(BaseRuneData baseRune, IReadOnlyList<RuneWord> addedWords)
        {
            if (addedWords.Count == 0)
                return baseRune.Name;

            // e.g. "Fire Rune: ocean, intensify"
            return $"{baseRune.Name}: {string.Join(", ", addedWords.Select(w => w.EnglishName))}";
        }
    }
}
