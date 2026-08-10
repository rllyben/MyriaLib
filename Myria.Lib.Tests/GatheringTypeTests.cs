using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// GatheringType was a closed enum; it's now a string-constant class (matching CharacterClass/
/// CharacterRace/ItemRarity/EquipmentType). Unlike EquipmentType, it was already serialized as a
/// JSON string end-to-end (items.json/rooms.json/SQL all store it as a string, and the enum
/// already carried [JsonConverter(typeof(JsonStringEnumConverter))]) — a plain type-for-type swap,
/// no FromLegacyInt migration table needed. Needs [Collection("GameData")] for real items.json content.
/// </summary>
[Collection("GameData")]
public class GatheringTypeTests
{
    public GatheringTypeTests(GameDataFixture _) { }

    [Fact]
    public void ItemFactory_LoadsRealItemsJsonToolType_AsTheCorrectStringId()
    {
        // pickaxe has "ToolType": "Ore" in items.json.
        var item = ItemFactory.CreateItem("pickaxe");
        var equipment = Assert.IsType<EquipmentItem>(item);
        Assert.Equal(GatheringType.Ore, equipment.ToolType);
        Assert.True(equipment.IsTool);
    }

    [Fact]
    public void HasToolFor_Herb_AlwaysTrue_RegardlessOfInventory()
    {
        var character = TestHelpers.CreateCharacter();
        Assert.True(character.HasToolFor(GatheringType.Herb));
    }

    [Fact]
    public void HasToolFor_Ore_FalseWithoutMatchingTool_TrueOnceEquipped()
    {
        var character = TestHelpers.CreateCharacter();
        Assert.False(character.HasToolFor(GatheringType.Ore));

        var pickaxe = Assert.IsType<EquipmentItem>(ItemFactory.CreateItem("pickaxe"));
        character.Equip(pickaxe);

        Assert.True(character.HasToolFor(GatheringType.Ore));
    }

    [Fact]
    public void GetDisplayName_NotInRegistry_ReturnsTheRawStringItself()
    {
        // gathering_types.json doesn't exist in this repo, so GatheringTypeRegistry.All is
        // always empty here — this always exercises the fallback.
        Assert.Equal(GatheringType.Ore, GatheringTypeRegistry.GetDisplayName(GatheringType.Ore));
    }
}
