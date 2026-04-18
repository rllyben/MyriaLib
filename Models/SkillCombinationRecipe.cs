namespace MyriaLib.Models
{
    /// <summary>
    /// An authored recipe that defines what a specific multiset of base skill IDs produces
    /// when combined. Matched order-independently via a sorted key.
    /// </summary>
    public class SkillCombinationRecipe
    {
        /// <summary>
        /// The input skill IDs (2–5, duplicates allowed). Matched as a sorted multiset —
        /// order does not matter during lookup.
        /// </summary>
        public List<string> InputSkillIds { get; set; } = new();

        /// <summary>ID to assign to the resulting <c>Skill</c> object.</summary>
        public string ResultId { get; set; } = "";

        public string ResultName { get; set; } = "";
        public string ResultDescription { get; set; } = "";

        public string? TypeOverride { get; set; }
        public string? TargetOverride { get; set; }
        public string? StatToScaleFromOverride { get; set; }
        public float? ScalingFactorOverride { get; set; }
        public int? ManaCostOverride { get; set; }
        public bool? IsHealingOverride { get; set; }
    }
}
