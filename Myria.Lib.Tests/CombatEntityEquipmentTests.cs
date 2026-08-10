using System.Text.Json;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// CombatEntity's WeaponSlot/ArmorSlot/AccessorySlot used to be three independent auto-properties,
/// with GetBonusFromGear manually null-checking each by name. They're now pass-through properties
/// over a generic Equipped dictionary, keyed by the same slot-ID strings as EquipmentType — a mod
/// could set character.Equipped["Ring"] right now and have it correctly included in combat math,
/// though it won't survive save/load or appear in any UI without matching DB/ItemFactory/UI work.
/// Equipped itself is [JsonIgnore]'d specifically so the save-file JSON shape doesn't change at
/// all — WeaponSlot/ArmorSlot/AccessorySlot remain the only serialized surface, exactly as before.
/// </summary>
public class CombatEntityEquipmentTests
{
    private static EquipmentItem MakeGear(string slotType, int atk = 0, int def = 0) => new()
    {
        Id = $"test_{slotType.ToLowerInvariant()}",
        Name = $"Test {slotType}",
        SlotType = slotType,
        Bonuses = new EquipmentBonuses { ATK = atk, DEF = def },
    };

    [Fact]
    public void NamedSlotProperties_RoundTripThroughEquippedDictionary()
    {
        var character = TestHelpers.CreateCharacter();
        var weapon = MakeGear(EquipmentType.Weapon);

        character.WeaponSlot = weapon;

        Assert.Same(weapon, character.Equipped[EquipmentType.Weapon]);
        Assert.Same(weapon, character.WeaponSlot);
        Assert.Null(character.ArmorSlot);
    }

    [Fact]
    public void GetBonusFromGear_SumsAcrossAllEquippedSlots()
    {
        var character = TestHelpers.CreateCharacter();
        character.WeaponSlot = MakeGear(EquipmentType.Weapon, atk: 10);
        character.ArmorSlot = MakeGear(EquipmentType.Armor, def: 5);
        character.AccessorySlot = MakeGear(EquipmentType.Accessory, atk: 2);

        Assert.Equal(12, character.GetBonusFromGear(g => g.Bonuses.ATK));
        Assert.Equal(5, character.GetBonusFromGear(g => g.Bonuses.DEF));
    }

    [Fact]
    public void GetBonusFromGear_ArbitraryModAddedSlot_IsIncludedToo()
    {
        // Not persisted/UI-visible yet, but the combat-math seam already supports it.
        var character = TestHelpers.CreateCharacter();
        character.Equipped["Ring"] = MakeGear("Ring", atk: 7);

        Assert.Equal(7, character.GetBonusFromGear(g => g.Bonuses.ATK));
    }

    [Fact]
    public void Equip_ReturnsPreviouslyEquippedItem_ToInventory()
    {
        var character = TestHelpers.CreateCharacter();
        var oldWeapon = MakeGear(EquipmentType.Weapon);
        var newWeapon = MakeGear(EquipmentType.Weapon);
        character.WeaponSlot = oldWeapon;

        character.Equip(newWeapon);

        Assert.Same(newWeapon, character.WeaponSlot);
        Assert.Contains(oldWeapon, character.Inventory.Items);
    }

    [Fact]
    public void JsonRoundTrip_SerializesOnlyNamedProperties_NotTheEquippedDictionary()
    {
        var character = TestHelpers.CreateCharacter();
        character.WeaponSlot = MakeGear(EquipmentType.Weapon, atk: 10);

        var options = new JsonSerializerOptions { Converters = { new ItemConverter() } };
        string json = JsonSerializer.Serialize(character, options);

        Assert.Contains("\"WeaponSlot\"", json);
        Assert.DoesNotContain("\"Equipped\"", json);

        var reloaded = JsonSerializer.Deserialize<Character>(json, options);
        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.WeaponSlot);
        Assert.Equal("test_weapon", reloaded.WeaponSlot!.Id);
        Assert.Same(reloaded.WeaponSlot, reloaded.Equipped[EquipmentType.Weapon]);
    }

    [Fact]
    public void JsonRoundTrip_OldSaveFileShapeWithNoEquippedKey_StillLoadsCorrectly()
    {
        // Matches the real shape of existing MyriaRPG/MyriaServer save files (checked directly):
        // top-level "WeaponSlot"/"ArmorSlot"/"AccessorySlot" keys, never an "Equipped" key.
        var character = TestHelpers.CreateCharacter();
        character.ArmorSlot = MakeGear(EquipmentType.Armor, def: 3);
        var options = new JsonSerializerOptions { Converters = { new ItemConverter() } };
        string oldFormatJson = JsonSerializer.Serialize(character, options);

        var reloaded = JsonSerializer.Deserialize<Character>(oldFormatJson, options);

        Assert.NotNull(reloaded);
        Assert.Null(reloaded!.WeaponSlot);
        Assert.NotNull(reloaded.ArmorSlot);
        Assert.Equal("test_armor", reloaded.ArmorSlot!.Id);
        Assert.Same(reloaded.ArmorSlot, reloaded.Equipped[EquipmentType.Armor]);
    }
}
