using MyriaLib.Entities.Skills;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Models
{
    /// <summary>
    /// Definition of a base rune as loaded from JSON.
    /// A base rune is the starting point that players can expand by adding runic words.
    /// Class-specific — players receive all base runes for their class at character creation.
    /// </summary>
    public class BaseRuneData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        /// <summary>The root <see cref="RuneWord.Id"/> that defines this rune's core meaning.</summary>
        public string CoreWordId { get; set; } = "";

        /// <summary>The player class that has access to this base rune.</summary>
        public string Class { get; set; } = "";

        public int BaseManaCost { get; set; }
        public float BaseScalingFactor { get; set; } = 1.0f;

        /// <summary>"ATK", "MATK", "INT", "SPR", etc. — same keys as <see cref="Skill.StatToScaleFrom"/>.</summary>
        public string StatToScaleFrom { get; set; } = "MATK";

        public string Target { get; set; } = "SingleEnemy";  // serialized as string, parsed to SkillTarget
        public bool IsHealing { get; set; } = false;
    }
}
