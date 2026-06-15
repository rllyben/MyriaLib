namespace MyriaLib.Systems.Mods
{
    public sealed class ModItemUseEffectDefinition
    {
        public string Key { get; set; } = "";
        public string ItemId { get; set; } = "";
        public bool ApplyBaseEffect { get; set; } = true;
        public bool HealToFull { get; set; }
        public bool RestoreManaToFull { get; set; }
        public int KillOnDailyUseNumber { get; set; }
        public string DailyCounterKey { get; set; } = "";
        public string DeathSource { get; set; } = "";
    }
}
