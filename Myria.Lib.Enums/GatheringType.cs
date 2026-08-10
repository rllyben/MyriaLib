namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Built-in gathering type identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/>/<see cref="ItemRarity"/> — so
    /// mod-added gathering types (defined in Data/common/gathering_types.json) work the same way
    /// as these built-ins without requiring any code changes. Previously a closed enum already
    /// serialized as a JSON string (via JsonStringEnumConverter) and stored as a string end-to-end
    /// in items.json/rooms.json/SQL — unlike EquipmentType, no legacy-int migration was needed.
    /// </summary>
    public static class GatheringType
    {
        public const string Ore  = "Ore";
        public const string Tree = "Tree";
        public const string Herb = "Herb";

        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            Ore, Tree, Herb
        };
    }
}
