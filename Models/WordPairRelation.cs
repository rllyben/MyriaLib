using MyriaLib.Systems.Enums;

namespace MyriaLib.Models
{
    /// <summary>
    /// An explicit relationship override between two specific runic words.
    /// Takes precedence over the family-level default when both words are present on a rune.
    /// Word order does not matter — (A, B) and (B, A) are the same pair.
    /// </summary>
    public class WordPairRelation
    {
        public string WordIdA { get; set; } = "";
        public string WordIdB { get; set; } = "";
        public WordRelationship Relationship { get; set; }

        /// <summary>
        /// Only set when <see cref="Relationship"/> is <see cref="WordRelationship.Transform"/>.
        /// The Id of the new <see cref="BaseRuneData"/> that becomes available to the player.
        /// </summary>
        public string? TransformResultRuneId { get; set; }
    }
}
