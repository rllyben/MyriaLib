namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Built-in item rarity identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/> — so mod-added rarity tiers
    /// (defined in Data/common/item_rarities.json) work the same way as these built-ins without
    /// requiring any code changes. Previously a closed enum; ItemRarityRegistry already only ever
    /// used it for display-name/order lookup by string ID, so nothing behavioral depended on it
    /// being a real enum.
    /// </summary>
    public static class ItemRarity
    {
        public const string Common    = "Common";
        public const string Uncommon  = "Uncommon";
        public const string Rare      = "Rare";
        public const string Epic      = "Epic";
        public const string Unique    = "Unique";
        public const string Legendary = "Legendary";
        public const string Godly     = "Godly";

        /// <summary>
        /// All built-in rarity IDs, in their original enum declaration order — used as the
        /// default sort order fallback (see ItemRarityRegistry.GetOrder) when no
        /// item_rarities.json entry overrides it.
        /// </summary>
        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            Common, Uncommon, Rare, Epic, Unique, Legendary, Godly
        };
    }
}
