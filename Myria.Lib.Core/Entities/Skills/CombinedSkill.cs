using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Entities.Skills
{
    /// <summary>
    /// A skill the player created by combining 2–5 of their learned base skills.
    /// Serialized as part of the player save. <see cref="ResolvedSkill"/> is populated
    /// at runtime by <c>SkillCombinationService.ResolveCombinedSkills</c>.
    /// </summary>
    public class CombinedSkill
    {
        /// <summary>Unique identifier for this combined skill instance (generated on creation).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The IDs of the base skills that were combined (2–5 entries, duplicates allowed).
        /// Stored in sorted order so recipe lookup is order-independent.
        /// </summary>
        public List<string> SkillIds { get; set; } = new();

        /// <summary>
        /// The combat-ready skill produced by the combination.
        /// Populated at runtime — not serialized.
        /// </summary>
        [JsonIgnore]
        public Skill? ResolvedSkill { get; set; }

        /// <summary>Display name — uses the resolved skill's name if available, falls back to joined IDs.</summary>
        [JsonIgnore]
        public string DisplayName => ResolvedSkill?.Name ?? string.Join("+", SkillIds);
    }
}
