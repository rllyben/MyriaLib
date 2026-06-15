namespace MyriaLib.Systems.Mods
{
    using MyriaLib.Models.BaseModel;

    public class ModManifest
    {
        public string Id          { get; set; } = "";
        public string Name        { get; set; } = "";
        public string Version     { get; set; } = "1.0.0";
        public string Author      { get; set; } = "";
        public string Description { get; set; } = "";
        public bool   Enabled     { get; set; } = true;
        /// <summary>Lower values load first. Default 100. Higher-order mods override earlier ones.</summary>
        public int    LoadOrder   { get; set; } = 100;
        public List<ModSettingDefinition> Settings { get; set; } = [];
        public Dictionary<string, string> SettingValues { get; set; } = new();
        public List<ModVisualEffectDefinition> VisualEffects { get; set; } = [];
        public List<GameItem> ItemAdditions { get; set; } = [];
        public List<ModItemUseEffectDefinition> ItemUseEffects { get; set; } = [];
        public List<ModNpcItemAdditionDefinition> NpcItemAdditions { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> LocaleAdditions { get; set; } = new();
    }
}
