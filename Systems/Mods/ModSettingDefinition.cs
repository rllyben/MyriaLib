namespace MyriaLib.Systems.Mods
{
    public class ModSettingDefinition
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "text";
        public string DefaultValue { get; set; } = "";
        public double Min { get; set; } = 0;
        public double Max { get; set; } = 100;
        public double Step { get; set; } = 1;
    }
}
