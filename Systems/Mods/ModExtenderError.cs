namespace MyriaLib.Systems.Mods
{
    /// <summary>Failure reported by a registered <see cref="IModLoaderExtender"/>.</summary>
    public sealed class ModExtenderError
    {
        public string ExtenderName { get; init; } = "";
        public string Phase { get; init; } = "";
        public Exception Exception { get; init; } = new();
    }
}
