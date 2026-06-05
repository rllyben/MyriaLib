namespace MyriaLib.Systems.Mods
{
    public class ModManifest
    {
        public string Id          { get; set; } = "";
        public string Name        { get; set; } = "";
        public string Version     { get; set; } = "1.0.0";
        public string Author      { get; set; } = "";
        public string Description { get; set; } = "";
        /// <summary>Lower values load first. Default 100. Higher-order mods override earlier ones.</summary>
        public int    LoadOrder   { get; set; } = 100;
    }
}
