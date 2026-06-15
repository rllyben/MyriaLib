namespace MyriaLib.Systems.Mods
{
    /// <summary>A mod that has been scanned, validated, and registered by <see cref="ModLoader"/>.</summary>
    public class LoadedMod
    {
        public ModManifest           Manifest        { get; init; } = new();
        public string                Directory       { get; init; } = "";
        public bool                  IsEnabled       { get; init; } = true;
        /// <summary>True when every file in this mod lives under a visual-only path (locales, icons, images).</summary>
        public bool                  IsVisualOnly    { get; init; }
        public IReadOnlyList<string> OverriddenFiles { get; init; } = [];
        /// <summary>SHA-256 fingerprint of all gameplay-affecting files; empty string for visual-only mods.</summary>
        public string                Fingerprint     { get; init; } = "";
    }
}
