namespace Myria.Lib.Core.Entities.Effects
{
    public class SkillEffectEntry
    {
        public string EffectId { get; set; } = "";
        public EffectTarget ApplyTo { get; set; } = EffectTarget.Target;
    }
}
