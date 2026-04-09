using System.Text.Json.Serialization;

namespace MyriaLib.Entities.Skills
{
    /// <summary>
    /// A fusion skill in the player's collection: a set of base skill components fused into one.
    /// Serialized as part of the player's save. <see cref="ResolvedSkill"/> is populated at runtime
    /// by <c>SkillFusionSystem.Fuse</c> and is never written to disk.
    /// </summary>
    public class CompositeSkill
    {
        /// <summary>Unique identifier for this fusion skill instance (generated on creation).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The base skill IDs that were fused to create this skill.
        /// The same base skill ID can appear more than once (stacking).
        /// </summary>
        public List<string> ComponentIds { get; set; } = new();

        /// <summary>
        /// The combat-ready skill produced by fusing the components.
        /// Populated by <c>SkillFusionSystem.Fuse</c> after load or after creation.
        /// Not serialized.
        /// </summary>
        [JsonIgnore]
        public Skill? ResolvedSkill { get; set; }

        /// <summary>Display name — uses the resolved skill's name if available, falls back to the component IDs joined.</summary>
        [JsonIgnore]
        public string DisplayName => ResolvedSkill?.Name ?? string.Join("+", ComponentIds);
    }
}
