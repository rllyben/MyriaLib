using MyriaLib.Entities.Characters;
using MyriaLib.Services.Builder;
using Xunit;

namespace MyriaLib.Tests;

/// <summary>
/// Exercises GameService.InitializeGame() against the real Data/common/*.json content
/// (linked from MyriaLib\Data — see MyriaLib.Tests.csproj) via the shared GameDataFixture,
/// proving the fixture/collection setup actually works end-to-end, not just in isolation.
/// </summary>
[Collection("GameData")]
public class GameServiceIntegrationTests
{
    public GameServiceIntegrationTests(GameDataFixture _) { }

    [Fact]
    public void InitializeGame_LoadsRealRaceData()
    {
        Assert.True(RaceProfile.All.ContainsKey("Myralu"));
    }

    [Fact]
    public void InitializeGame_LoadsRealItemData()
    {
        var item = ItemFactory.CreateItem("t1_healing_potion");
        Assert.NotNull(item);
        Assert.Equal("t1_healing_potion", item!.Id);
    }
}
