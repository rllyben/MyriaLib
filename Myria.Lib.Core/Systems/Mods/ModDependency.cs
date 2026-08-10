namespace Myria.Lib.Core.Systems.Mods
{
    public class ModDependency
    {
        /// <summary>Id of the required mod.</summary>
        public string Id { get; set; } = "";

        /// <summary>Minimum version of the required mod. Null/empty means any version is accepted.</summary>
        public string? MinVersion { get; set; }

        /// <summary>
        /// When true this is a soft dependency: the mod loads fine without it but may have
        /// reduced functionality. A missing or outdated optional dependency produces a
        /// warning rather than blocking the mod from loading.
        /// </summary>
        public bool Optional { get; set; }
    }
}
