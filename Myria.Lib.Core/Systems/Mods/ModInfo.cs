namespace Myria.Lib.Core.Systems.Mods
{
    /// <summary>
    /// Snapshot of active mods sent to the server on connection for multiplayer validation.
    /// Serialises cleanly over SignalR / JSON.
    /// </summary>
    public class ModInfo
    {
        public IReadOnlyList<ModEntry> ActiveMods    { get; init; } = [];
        public bool                    HasGameplayMods => ActiveMods.Any(m => !m.IsVisualOnly);
    }

    public class ModEntry
    {
        public string Id           { get; init; } = "";
        public string Name         { get; init; } = "";
        public string Version      { get; init; } = "";
        public bool   IsVisualOnly { get; init; }
        /// <summary>SHA-256 fingerprint of this mod's gameplay-affecting files. Empty for visual-only mods.</summary>
        public string Fingerprint  { get; init; } = "";
    }
}
