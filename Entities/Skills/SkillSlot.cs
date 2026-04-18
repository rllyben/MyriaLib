using System.Text.Json.Serialization;

namespace MyriaLib.Entities.Skills
{
    public enum SlottedSkillSource { Regular, Combined, CompositeFusion }

    /// <summary>
    /// One entry in the player's combat skill bar.
    /// Serialized as part of the player save. <see cref="ResolvedSkill"/> is populated
    /// at runtime by <c>SkillSlotService.ResolveSlots</c>.
    /// </summary>
    public class SkillSlot
    {
        /// <summary>Which pool the skill comes from.</summary>
        public SlottedSkillSource Source { get; set; }

        /// <summary>
        /// The ID of the skill within its pool:
        /// <list type="bullet">
        ///   <item>Regular — <c>Skill.Id</c> from <c>player.Skills</c></item>
        ///   <item>Combined — <c>CombinedSkill.Id</c> from <c>player.CombinedSkills</c></item>
        ///   <item>CompositeFusion — <c>CompositeSkill.Id</c> from <c>player.CompositeSkills</c></item>
        /// </list>
        /// </summary>
        public string SkillId { get; set; } = "";

        /// <summary>The resolved combat skill. Populated at runtime — not serialized.</summary>
        [JsonIgnore]
        public Skill? ResolvedSkill { get; set; }
    }
}
