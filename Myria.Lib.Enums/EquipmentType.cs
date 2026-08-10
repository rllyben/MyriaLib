namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Built-in equipment slot identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/>/<see cref="ItemRarity"/> — instead
    /// of a closed enum. Note this alone does not let a mod add a 4th equipment slot: CombatEntity
    /// still has exactly 3 named slot properties (WeaponSlot/ArmorSlot/AccessorySlot), not a slot
    /// dictionary — that's separate, larger structural work. This conversion only makes the
    /// existing 3 slot identifiers consistent with the rest of the string-based type system.
    /// </summary>
    public static class EquipmentType
    {
        public const string Weapon    = "Weapon";
        public const string Armor     = "Armor";
        public const string Accessory = "Accessory";

        public static readonly IReadOnlyList<string> AllBuiltIn = new[] { Weapon, Armor, Accessory };

        /// <summary>
        /// Maps the old enum's integer values to string IDs. items.json stores SlotType as a raw
        /// JSON number (e.g. 2), and the SQL mirror (DbItem.SlotType) stores it as int? — both
        /// need this to convert into the new string form.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string> FromLegacyInt =
            new Dictionary<int, string>
            {
                [0] = Weapon,
                [1] = Armor,
                [2] = Accessory,
            };
    }
}
