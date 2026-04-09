using System.Text.Json.Serialization;

namespace MyriaLib.Entities.Skills
{
    /// <summary>
    /// A rune in the player's collection: a base rune with zero or more runic words added to it.
    /// Serialized as part of the player's save. <see cref="ResolvedSkill"/> is populated at runtime
    /// by <c>RuneEvaluator</c> and is never written to disk.
    /// </summary>
    public class CompositeRune
    {
        /// <summary>Unique identifier for this rune instance (generated on creation).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>References <c>BaseRuneData.Id</c> — the root rune this was built from.</summary>
        public string BaseRuneId { get; set; } = "";

        /// <summary>
        /// The runic word IDs the player has added to this rune, in any order.
        /// Word order has no effect on evaluation.
        /// </summary>
        public List<string> AddedWordIds { get; set; } = new();

        /// <summary>
        /// The combat-ready skill produced by evaluating this rune's words.
        /// Populated by <c>RuneEvaluator.Resolve</c> after load or after the player modifies the rune.
        /// Not serialized.
        /// </summary>
        [JsonIgnore]
        public Skill? ResolvedSkill { get; set; }

        /// <summary>Display name — uses the resolved skill's name if available, falls back to the base rune id.</summary>
        [JsonIgnore]
        public string DisplayName => ResolvedSkill?.Name ?? BaseRuneId;
    }
}
