namespace Myria.Lib.Core.Systems.Mods
{
    /// <summary>Convenience base class for extenders that only need one lifecycle hook.</summary>
    public abstract class ModLoaderExtender : IModLoaderExtender
    {
        public virtual void BeforeModsReload(ModLoadContext context) { }
        public virtual void AfterModsLoaded(ModLoadContext context) { }
    }
}
