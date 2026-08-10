using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// EquipmentType was a closed enum; it's now a string-constant class (matching CharacterClass/
/// CharacterRace/ItemRarity). Unlike ItemRarity, items.json stores SlotType as a raw JSON number
/// (e.g. 2), not a string, and the SQL mirror (DbItem.SlotType) stores it as int? too — both
/// convert via EquipmentType.FromLegacyInt (same pattern as CharacterClass.FromLegacyInt) rather
/// than a plain type-for-type swap. Needs [Collection("GameData")] for the real items.json content.
/// </summary>
[Collection("GameData")]
public class EquipmentTypeTests
{
    public EquipmentTypeTests(GameDataFixture _) { }

    [Theory]
    [InlineData(0, EquipmentType.Weapon)]
    [InlineData(1, EquipmentType.Armor)]
    [InlineData(2, EquipmentType.Accessory)]
    public void FromLegacyInt_MapsOldEnumOrdinalsToTheCorrectStringId(int legacyOrdinal, string expected)
    {
        Assert.Equal(expected, EquipmentType.FromLegacyInt[legacyOrdinal]);
    }

    [Fact]
    public void ItemFactory_ConvertsRealItemsJsonIntSlotType_ToTheCorrectStringId()
    {
        // soulbound_phylactery has "SlotType": 2 in items.json (raw JSON int) -> Accessory.
        var item = ItemFactory.CreateItem("soulbound_phylactery");
        var equipment = Assert.IsType<EquipmentItem>(item);
        Assert.Equal(EquipmentType.Accessory, equipment.SlotType);
    }

    [Fact]
    public void GetDisplayName_NotInRegistry_ReturnsTheRawStringItself()
    {
        // equipment_slots.json doesn't exist in this repo (see README §21, Known Limitations),
        // so EquipmentTypeRegistry.All is always empty here — this always exercises the fallback.
        Assert.Equal(EquipmentType.Weapon, EquipmentTypeRegistry.GetDisplayName(EquipmentType.Weapon));
    }
}
