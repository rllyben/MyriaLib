namespace Myria.Lib.Core.Entities.Effects
{
    public class EffectDefinition
    {
        public string Id          { get; set; } = "";
        public string Name        { get; set; } = "";
        public string Description { get; set; } = "";
        public EffectType Type    { get; set; }
        public int Duration       { get; set; } = 1;   // turns the effect lasts
        public float Magnitude    { get; set; } = 0f;  // damage/heal per tick, flat stat bonus, or revive fraction (0–1)
        public string StatName    { get; set; } = "";  // StatBuff only: "STR", "DEX", "END", "INT", "SPR"
    }
}
