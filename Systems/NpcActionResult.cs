namespace MyriaLib.Systems
{
    public class NpcActionResult
    {
        public bool Success { get; init; }
        public string MessageKey { get; init; } = "";   // for localization
        public object[] MessageArgs { get; init; } = Array.Empty<object>();

        public int HpRestored { get; init; }
        public int MpRestored { get; init; }
        public long GoldChange { get; init; }

        // Later: items bought, quest accepted, etc.
    }

}
