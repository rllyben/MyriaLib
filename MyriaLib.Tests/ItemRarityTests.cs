using MyriaLib.Entities.Items;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Regestries;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

/// <summary>
/// ItemRarity was a closed enum; it's now a string-constant class (matching CharacterClass/
/// CharacterRace) so mod-defined rarity tiers work without recompiling. These tests cover the
/// conversion's two real behavior changes: ItemFactory now accepts any non-empty rarity string
/// instead of silently rejecting anything Enum.TryParse didn't recognize, and
/// ItemRarityRegistry.GetOrder falls back to ItemRarity.AllBuiltIn's index instead of an enum
/// ordinal cast. Needs [Collection("GameData")] for the real items.json content.
/// </summary>
[Collection("GameData")]
public class ItemRarityTests
{
    public ItemRarityTests(GameDataFixture _) { }

    [Fact]
    public void ItemFactory_LoadsRealRarityFromContent()
    {
        var item = ItemFactory.CreateItem("soulbound_phylactery");
        Assert.Equal(ItemRarity.Legendary, item!.Rarity);
    }

    [Fact]
    public void Item_AcceptsAnyRarityString_NotJustTheSevenBuiltInOnes()
    {
        // Previously Enum.TryParse<ItemRarity> would silently reject anything not in the closed
        // 7-value enum, leaving the item at its default "Common". A mod defining a "Mythic" tier
        // now works without any code change here.
        var item = new MaterialItem { Id = "x", Name = "X", Rarity = "Mythic" };
        Assert.Equal("Mythic", item.Rarity);
    }

    [Theory]
    [InlineData(ItemRarity.Common, 0)]
    [InlineData(ItemRarity.Uncommon, 1)]
    [InlineData(ItemRarity.Godly, 6)]
    public void GetOrder_FallsBackToAllBuiltInIndex_ForBuiltInRaritiesNotInTheRegistry(string rarity, int expectedOrder)
    {
        // item_rarities.json doesn't exist in this repo (see README §21, Known Limitations), so
        // ItemRarityRegistry.All is always empty here — every call exercises the fallback path.
        Assert.Equal(expectedOrder, ItemRarityRegistry.GetOrder(rarity));
    }

    [Fact]
    public void GetOrder_UnrecognizedCustomRarity_FallsBackToZero()
    {
        Assert.Equal(0, ItemRarityRegistry.GetOrder("Mythic"));
    }

    [Fact]
    public void GetDisplayName_NotInRegistry_ReturnsTheRawStringItself()
    {
        Assert.Equal(ItemRarity.Rare, ItemRarityRegistry.GetDisplayName(ItemRarity.Rare));
    }
}
