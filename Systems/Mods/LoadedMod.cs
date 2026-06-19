namespace MyriaLib.Systems.Mods
{
    /// <summary>
    /// A mod that has been scanned, validated, and registered by <see cref="ModLoader"/>.
    /// In addition to <see cref="Manifest"/> (from <c>mod.json</c>), any recognised sidecar files
    /// in the mod folder are loaded here:
    /// <list type="bullet">
    ///   <item><c>settings.json</c>        → <see cref="Settings"/></item>
    ///   <item><c>setting_values.json</c>  → <see cref="SettingValues"/></item>
    ///   <item><c>visual_effects.json</c>  → <see cref="VisualEffects"/></item>
    ///   <item><c>item_effects.json</c>    → <see cref="ItemEffects"/></item>
    /// </list>
    /// </summary>
    public class LoadedMod
    {
        public ModManifest           Manifest        { get; init; } = new();
        public string                Directory       { get; init; } = "";
        public bool                  IsEnabled       { get; set; } = true;
        /// <summary>True when every file in this mod lives under a visual-only path (locales, icons, images).</summary>
        public bool                  IsVisualOnly    { get; init; }
        public IReadOnlyList<string> OverriddenFiles { get; init; } = [];
        /// <summary>SHA-256 fingerprint of all gameplay-affecting files; empty for visual-only mods.</summary>
        public string                Fingerprint     { get; init; } = "";

        /// <summary>True when the mod shipped a <c>plugin.dll</c> that was loaded successfully.</summary>
        public bool                  HasPlugin       { get; init; }

        // ── Sidecar data ──────────────────────────────────────────────────────

        /// <summary>Setting definitions from <c>settings.json</c>.</summary>
        public List<ModSettingDefinition>       Settings      { get; init; } = [];

        /// <summary>User-chosen setting values from <c>setting_values.json</c>. Mutable so values can be persisted.</summary>
        public Dictionary<string, string>       SettingValues { get; init; } = new();

        /// <summary>Visual effect definitions from <c>visual_effects.json</c>.</summary>
        public List<ModVisualEffectDefinition>  VisualEffects { get; init; } = [];

        /// <summary>Item use-effect definitions from <c>item_effects.json</c>.</summary>
        public List<ModItemUseEffectDefinition> ItemEffects   { get; init; } = [];

        /// <summary>
        /// Populated during load validation. Each entry is a human-readable problem description.
        /// A non-empty list means the mod was not activated even though <see cref="IsEnabled"/> is true.
        /// </summary>
        public List<string> Warnings { get; } = [];
    }
}
