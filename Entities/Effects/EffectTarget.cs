namespace MyriaLib.Entities.Effects
{
    public enum EffectTarget
    {
        Target,    // applied to whoever the skill hits
        Caster,    // applied to the skill user
        AllAllies, // applied to all living allies (in solo: just the character)
    }
}
