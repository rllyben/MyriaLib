namespace Myria.Lib.Core.Systems.Mods
{
    using Myria.Lib.Core.Models.BaseModel;

    /// <summary>
    /// Parsed from <c>mod.json</c>. Describes what the mod IS and gives the loader
    /// instructions on how to read it. Operational data (settings, effects, etc.)
    /// lives in separate sidecar files loaded alongside this manifest.
    /// </summary>
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

        // ── Compatibility ─────────────────────────────────────────────────────

        /// <summary>Minimum game version required, e.g. "1.2.0". Empty means no minimum.</summary>
        public string MinGameVersion { get; set; } = "";

        /// <summary>Maximum game version supported, e.g. "2.0.0". Empty means no maximum.</summary>
        public string MaxGameVersion { get; set; } = "";

        /// <summary>Other mods this mod depends on.</summary>
        public List<ModDependency> Dependencies { get; set; } = [];

        // ── Loading instructions ──────────────────────────────────────────────

        /// <summary>
        /// File paths (relative) that this mod replaces completely rather than merging.
        /// E.g. "Data/common/items.json" means the mod's version discards all previous content.
        /// </summary>
        public List<string> CompleteOverwriteFiles { get; set; } = [];

        /// <summary>New languages this mod provides. Each entry points to a locale file in the mod folder.</summary>
        public List<ModLanguageDefinition> LanguageDefinitions { get; set; } = [];
    }
}
