using MyriaLib.Entities;
using MyriaLib.Entities.Characters;
using MyriaLib.Services.Manager;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

[Collection("GameData")]
public class StartingEquipmentServiceTests
{
    [Fact]
    public void GrantStartingEquipment_Fighter_AddsRealItemsToInventory()
    {
        var character = new Character("test", new Stats()) { Class = CharacterClass.Fighter };

        StartingEquipmentService.GrantStartingEquipment(character);

        Assert.NotEmpty(character.Inventory.Items);
    }
}
