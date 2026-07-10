namespace MyriaLib.Entities.Effects
{
    public enum EffectType
    {
        StatBuff,       // adds flat bonus to a specific stat for the duration
        Poison,         // deals fixed damage per turn
        Stun,           // entity skips its turn entirely (no attack, skill, or item)
        Silence,        // entity cannot use skills; auto-attacks and items still work
        HpRegen,        // restores fixed HP per turn
        ManaRegen,      // restores fixed mana per turn
        Resurrection,   // on death, revives at Magnitude% of max HP (only if allies remain alive)
        LifeSteal,      // instantaneous: heals caster for Magnitude fraction of damage dealt by the triggering skill
        ManaSiphon,     // instantaneous: restores caster mana for Magnitude fraction of damage dealt by the triggering skill
    }
}
