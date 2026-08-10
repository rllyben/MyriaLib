namespace Myria.Lib.Core.Systems.Mods
{
    /// <summary>
    /// Project-specific extension point for reacting to mod reloads.
    /// UI projects can implement this to apply visual assets without making Myria.Lib.Core depend on a UI stack.
    /// </summary>
    public interface IModLoaderExtender
    {
        /// <summary>Called immediately before the active mod list is cleared and rebuilt.</summary>
        void BeforeModsReload(ModLoadContext context);

        /// <summary>Called after the active mod list has been rebuilt and sorted.</summary>
        void AfterModsLoaded(ModLoadContext context);
    }
}
