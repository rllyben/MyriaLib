namespace Myria.Lib.Core.Systems.Mods
{
    /// <summary>Immutable snapshot passed to <see cref="IModLoaderExtender"/> implementations.</summary>
    public sealed class ModLoadContext
    {
        public string ModsDirectory { get; }
        public IReadOnlyList<LoadedMod> ActiveMods { get; }
        public IReadOnlyList<LoadedMod> VisualMods { get; }
        public IReadOnlyList<LoadedMod> GameplayMods { get; }
        public bool MultiplayerMode { get; }

        internal ModLoadContext(string modsDirectory, IReadOnlyList<LoadedMod> activeMods, bool multiplayerMode)
        {
            ModsDirectory = modsDirectory;
            ActiveMods = activeMods;
            VisualMods = activeMods.Where(m => m.IsVisualOnly).ToList();
            GameplayMods = activeMods.Where(m => !m.IsVisualOnly).ToList();
            MultiplayerMode = multiplayerMode;
        }
    }
}
