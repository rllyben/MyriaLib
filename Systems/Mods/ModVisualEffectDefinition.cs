namespace MyriaLib.Systems.Mods
{
    public class ModVisualEffectDefinition
    {
        public string Key { get; set; } = "";
        public string Target { get; set; } = "";
        public string Effect { get; set; } = "";
        public string SourceSetting { get; set; } = "";
        public string BrightnessSetting { get; set; } = "";
        public string EnabledSetting { get; set; } = "";
        public double Speed { get; set; } = 1;
    }
}
