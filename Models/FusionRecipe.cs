namespace MyriaLib.Models
{
    /// <summary>
    /// A named override for a specific base-skill combination.
    /// When the player fuses a set of base skills that matches <see cref="ComponentIds"/>,
    /// the resulting skill gets this name and description instead of the algorithmically generated ones.
    /// Stats and effects are still derived by the fusion rules — only the identity is overridden.
    /// </summary>
    public class FusionRecipe
    {
        public string ResultId { get; set; } = "";
        public string ResultName { get; set; } = "";
        public string ResultDescription { get; set; } = "";

        /// <summary>
        /// The base skill IDs that trigger this recipe.
        /// Stored and matched in sorted order so (A, B) and (B, A) resolve to the same recipe.
        /// </summary>
        public List<string> ComponentIds { get; set; } = new();

        /// <summary>
        /// Optional stat overrides — if set, these replace the algorithmically derived values.
        /// Use to balance combinations that the rules make too strong or too weak.
        /// Leave null to use fully algorithmic output.
        /// </summary>
        public float? ScalingFactorOverride { get; set; }
        public int? ManaCostOverride { get; set; }
        public string? TargetOverride { get; set; }
    }
}
