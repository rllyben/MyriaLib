using MyriaLib.Systems.Enums;

namespace MyriaLib.Models
{
    /// <summary>
    /// A semantic category of runic words (e.g. Fire, Water, Poison).
    /// Defines the default <see cref="WordRelationship"/> this family has toward other families.
    /// Words in the same family default to <see cref="WordRelationship.Support"/>.
    /// </summary>
    public class WordFamily
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        /// <summary>
        /// Default relationship from this family to another family, keyed by the other family's Id.
        /// If a pair is not listed here, the relationship defaults to <see cref="WordRelationship.Neutral"/>.
        /// Explicit <see cref="WordPairRelation"/> entries override these family-level defaults.
        /// </summary>
        public Dictionary<string, WordRelationship> FamilyRelations { get; set; } = new();
    }
}
