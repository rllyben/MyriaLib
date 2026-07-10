namespace MyriaLib.Entities.Effects
{
    public class ActiveEffect
    {
        public string DefinitionId  { get; set; } = "";
        public string Name          { get; set; } = "";
        public EffectType Type      { get; set; }
        public int TurnsRemaining   { get; set; }
        public float Magnitude      { get; set; }
        public string StatName      { get; set; } = "";  // StatBuff only
        public string SourceSkillId       { get; set; } = "";  // for log readability
        public string SourceCharacterName { get; set; } = "";  // for aggro credit on DoT ticks
    }
}
