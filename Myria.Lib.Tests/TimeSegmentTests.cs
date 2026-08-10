using System.Text.Json;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// TimeSegment was a closed enum; it's now a string-constant class (matching CharacterClass/
/// CharacterRace/ItemRarity/GatheringType/EquipmentType). Unlike GatheringType, existing
/// gameStatus.json save files store TimeOfDay as a raw JSON int (no JsonStringEnumConverter was
/// ever on the enum) — handled via TimeSegment.FromLegacyInt + TimeSegmentJsonConverter so old
/// saves keep loading, mirroring EquipmentType's FromLegacyInt pattern but as a converter since
/// GameStatus is live runtime state, not a build-time DTO like GameItem.
/// </summary>
public class TimeSegmentTests
{
    [Theory]
    [InlineData(0, TimeSegment.Morning)]
    [InlineData(1, TimeSegment.Midday)]
    [InlineData(2, TimeSegment.Evening)]
    [InlineData(3, TimeSegment.Night)]
    public void FromLegacyInt_MapsOldEnumOrdinalsToTheCorrectStringId(int legacyOrdinal, string expected)
    {
        Assert.Equal(expected, TimeSegment.FromLegacyInt[legacyOrdinal]);
    }

    [Fact]
    public void GameStatus_DeserializesLegacyIntTimeOfDay_ToTheCorrectStringId()
    {
        // Real shape of a pre-conversion gameStatus.json save file.
        var json = """{ "GameDay": 2, "TimeOfDay": 2, "Ticks": 42, "LastSavedAt": "2026-07-23T14:24:57Z" }""";

        var status = JsonSerializer.Deserialize<GameStatus>(json);

        Assert.NotNull(status);
        Assert.Equal(TimeSegment.Evening, status!.TimeOfDay);
    }

    [Fact]
    public void GameStatus_RoundTripsStringTimeOfDay()
    {
        var status = new GameStatus { TimeOfDay = TimeSegment.Night };

        var json = JsonSerializer.Serialize(status);
        var roundTripped = JsonSerializer.Deserialize<GameStatus>(json);

        Assert.Contains("\"Night\"", json);
        Assert.Equal(TimeSegment.Night, roundTripped!.TimeOfDay);
    }

    [Fact]
    public void DayCycleManager_AdvancesThroughSegmentsInOrder_AndWrapsToNewDay()
    {
        var game = GameService.Game;
        string origSegment = game.TimeOfDay;
        int origTicks = game.Ticks;
        int origDay = game.GameDay;
        int origTicksPerSegment = DayCycleManager.TicksPerSegment;
        try
        {
            DayCycleManager.TicksPerSegment = 10;
            game.TimeOfDay = TimeSegment.Morning;
            game.Ticks = 0;
            game.GameDay = 1;

            string? fired = null;
            void Handler(string s) => fired = s;
            DayCycleManager.SegmentChanged += Handler;
            try
            {
                DayCycleManager.AddTicks(10);
            }
            finally
            {
                DayCycleManager.SegmentChanged -= Handler;
            }

            Assert.Equal(TimeSegment.Midday, game.TimeOfDay);
            Assert.Equal(TimeSegment.Midday, fired);
            Assert.Equal(1, game.GameDay); // no wrap yet
        }
        finally
        {
            game.TimeOfDay = origSegment;
            game.Ticks = origTicks;
            game.GameDay = origDay;
            DayCycleManager.TicksPerSegment = origTicksPerSegment;
        }
    }

    [Fact]
    public void DayCycleManager_NightToMorning_AdvancesGameDay()
    {
        var game = GameService.Game;
        string origSegment = game.TimeOfDay;
        int origTicks = game.Ticks;
        int origDay = game.GameDay;
        int origTicksPerSegment = DayCycleManager.TicksPerSegment;
        try
        {
            DayCycleManager.TicksPerSegment = 10;
            game.TimeOfDay = TimeSegment.Night;
            game.Ticks = 0;
            game.GameDay = 1;

            DayCycleManager.AddTicks(10);

            Assert.Equal(TimeSegment.Morning, game.TimeOfDay);
            Assert.Equal(2, game.GameDay);
        }
        finally
        {
            game.TimeOfDay = origSegment;
            game.Ticks = origTicks;
            game.GameDay = origDay;
            DayCycleManager.TicksPerSegment = origTicksPerSegment;
        }
    }

    [Fact]
    public void GetDisplayName_NotInRegistry_ReturnsTheRawStringItself()
    {
        // time_segments.json doesn't exist in this repo, so TimeSegmentRegistry.All is always
        // empty here — this always exercises the fallback.
        Assert.Equal(TimeSegment.Morning, TimeSegmentRegistry.GetDisplayName(TimeSegment.Morning));
    }
}
