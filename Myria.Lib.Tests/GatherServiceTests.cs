using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// GatherService.Gather used to have zero callers anywhere in the codebase — both the WPF
/// single-player client and the multiplayer server had their own independent, hand-duplicated
/// copies of this exact logic (which happened to agree with each other, but not with this
/// method). This rewrite matches what those two real implementations actually did, and both
/// now call this method instead of their own copies. Needs [Collection("GameData")] because
/// ItemFactory.TryCreateItem requires items.json to be loaded.
/// </summary>
[Collection("GameData")]
public class GatherServiceTests
{
    public GatherServiceTests(GameDataFixture _) { }

    private static Room MakeRoom(params GatheringSpot[] spots)
    {
        var room = new Room("Test Room", "")
        {
            GatheringSpots = spots.ToList(),
        };
        room.RollGatherLimit(); // gives GathersRemaining a positive value (1-5)
        return room;
    }

    [Fact]
    public void Gather_NoGatheringSpots_ReturnsNoSpots()
    {
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(); // no spots

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.NoSpots, outcome.Result);
    }

    [Fact]
    public void Gather_Depleted_WhenRoomHasNoChargesRemaining()
    {
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(new GatheringSpot { Id = "s1", Name = "Spot", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment" });
        room.GathersRemaining = 0;

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.Depleted, outcome.Result);
    }

    [Fact]
    public void Gather_SkipsSpotRequiringMissingTool_UsesNextWorkableSpot()
    {
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(
            new GatheringSpot { Id = "locked", Name = "Locked", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment", RequiredToolId = "pickaxe" },
            new GatheringSpot { Id = "open", Name = "Open", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment" });
        int chargesBefore = room.GathersRemaining;

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.Success, outcome.Result);
        Assert.Equal("stone_fragment", outcome.ItemId);
        Assert.Equal(chargesBefore - 1, room.GathersRemaining);
    }

    [Fact]
    public void Gather_NoToolForAnySpot_ReturnsNoTool_AndDoesNotConsumeACharge()
    {
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(
            new GatheringSpot { Id = "locked", Name = "Locked", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment", RequiredToolId = "pickaxe" });
        int chargesBefore = room.GathersRemaining;

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.NoTool, outcome.Result);
        Assert.Equal(chargesBefore, room.GathersRemaining); // no charge consumed on failure
    }

    [Fact]
    public void Gather_WithRequiredToolInInventory_Succeeds()
    {
        var character = TestHelpers.CreateCharacter();
        var pickaxe = new EquipmentItem { Id = "pickaxe", Name = "Pickaxe" };
        character.Inventory.Items.Add(pickaxe);
        var room = MakeRoom(
            new GatheringSpot { Id = "vein", Name = "Vein", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment", RequiredToolId = "pickaxe" });

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.Success, outcome.Result);
    }

    [Fact]
    public void Gather_Success_AddsItemToCharacterInventory()
    {
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(
            new GatheringSpot { Id = "s1", Name = "Spot", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment" });

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.Success, outcome.Result);
        Assert.Contains(character.Inventory.Items, i => i.Id == "stone_fragment");
    }

    [Fact]
    public void Gather_InventoryFull_ReturnsInventoryFull_AndDoesNotConsumeACharge()
    {
        int originalPageSize = Inventory.PageSize;
        try
        {
            Inventory.PageSize = 0; // Capacity = Pages * PageSize = 0 -> AddItem always fails
            var character = TestHelpers.CreateCharacter();
            var room = MakeRoom(
                new GatheringSpot { Id = "s1", Name = "Spot", Description = "", Type = GatheringType.Ore, GatheredItemId = "stone_fragment" });
            int chargesBefore = room.GathersRemaining;

            var outcome = GatherService.Gather(character, room);

            Assert.Equal(GatherResult.InventoryFull, outcome.Result);
            Assert.Equal(chargesBefore, room.GathersRemaining);
        }
        finally
        {
            Inventory.PageSize = originalPageSize;
        }
    }

    [Fact]
    public void Gather_NoJobForSpotType_StillSucceeds_WithNoXpGranted()
    {
        // GatheringType has no matching jobId in the switch (only Ore/Tree/Herb map to a job) —
        // spot.Type here uses a value with no mapped job to confirm the jobless path is safe.
        var character = TestHelpers.CreateCharacter();
        var room = MakeRoom(
            new GatheringSpot { Id = "s1", Name = "Spot", Description = "", Type = "Unknown", GatheredItemId = "stone_fragment" });

        var outcome = GatherService.Gather(character, room);

        Assert.Equal(GatherResult.Success, outcome.Result);
        Assert.True(string.IsNullOrEmpty(outcome.JobId)); // "" for an unmapped GatheringType, not null
        Assert.Equal(0, outcome.XpGranted);
    }
}
