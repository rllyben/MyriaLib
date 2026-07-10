using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Effects;
using MyriaLib.Systems.Interfaces;
using System.Text.Json.Serialization;

namespace MyriaLib.Entities.Skills
{
    public enum SkillType { Physical, Magical }
    public enum SkillTarget { SingleEnemy, AllEnemies, Self, SingleAlly, AllAllies }

    public class Skill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CastTime { get; set; } = 0;     // turns before it activates
        public int RecoveryTime { get; set; } = 0; // turns before you can act again
        public string Class { get; set; } = "";
        public bool IsHealing { get; set; } = false;
        public int ManaCost { get; set; }
        public SkillType Type { get; set; }
        public SkillTarget Target { get; set; }

        public float ScalingFactor { get; set; }
        public string StatToScaleFrom { get; set; } = "ATK";

        public int MinLevel { get; set; } = 1;

        // Extra aggro generated when this skill is cast, on top of the base +1.
        // Default 0 means the skill generates the standard 1.0 aggro.
        public float AggroModifier { get; set; } = 0f;

        // Data-driven effects applied when the skill fires.
        // Each entry references an EffectDefinition by ID and specifies who receives it.
        public List<SkillEffectEntry> Effects { get; set; } = new();

        // Optional code-defined effect hook for special cases not covered by EffectDefinition.
        [JsonIgnore]
        public Action<Character, ICombatant>? Effect { get; set; }
    }

}
