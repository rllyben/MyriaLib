using Myria.Lib.Core.Entities;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

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
