using System.Text.Json;
using MyriaLib.Entities;
using MyriaLib.Entities.Characters;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

/// <summary>
/// Character.Class/Race were closed enums; they're now string-constant classes (matching
/// EquipmentType/ItemRarity/TimeSegment). Old character saves store them as a raw JSON int
/// (e.g. "Class": 0) or occasionally a numeric string (e.g. "Class": "0") - both are handled by
/// CharacterClassJsonConverter/CharacterRaceJsonConverter via CharacterClass/CharacterRace.FromLegacyInt,
/// mirroring TimeSegmentJsonConverter, so pre-conversion saves keep loading instead of throwing
/// a JsonException during deserialization (which happened before these converters existed,
/// since a bare property-level int->string mismatch fails before any post-load fixup can run).
/// </summary>
public class CharacterClassRaceMigrationTests
{
    [Theory]
    [InlineData(0, CharacterClass.Archer)]
    [InlineData(3, CharacterClass.Fighter)]
    [InlineData(11, CharacterClass.RunicMage)]
    public void CharacterClass_FromLegacyInt_MapsOldEnumOrdinalsToTheCorrectStringId(int legacyOrdinal, string expected)
    {
        Assert.Equal(expected, CharacterClass.FromLegacyInt[legacyOrdinal]);
    }

    [Theory]
    [InlineData(0, CharacterRace.Rotuka)]
    [InlineData(2, CharacterRace.Myralu)]
    [InlineData(6, CharacterRace.Amato)]
    public void CharacterRace_FromLegacyInt_MapsOldEnumOrdinalsToTheCorrectStringId(int legacyOrdinal, string expected)
    {
        Assert.Equal(expected, CharacterRace.FromLegacyInt[legacyOrdinal]);
    }

    [Fact]
    public void Character_DeserializesLegacyRawIntClassAndRace_ToTheCorrectStringIds()
    {
        // Real shape of a pre-conversion character save: Class/Race as raw JSON numbers.
        var json = """{ "Name": "sss", "Stats": {}, "Class": 0, "Race": 2 }""";

        var character = JsonSerializer.Deserialize<Character>(json);

        Assert.NotNull(character);
        Assert.Equal(CharacterClass.Archer, character!.Class);
        Assert.Equal(CharacterRace.Myralu, character.Race);
    }

    [Fact]
    public void Character_DeserializesLegacyNumericStringClassAndRace_ToTheCorrectStringIds()
    {
        // Some older saves stored the legacy ordinal as a quoted string instead of a raw number.
        var json = """{ "Name": "sss", "Stats": {}, "Class": "3", "Race": "6" }""";

        var character = JsonSerializer.Deserialize<Character>(json);

        Assert.NotNull(character);
        Assert.Equal(CharacterClass.Fighter, character!.Class);
        Assert.Equal(CharacterRace.Amato, character.Race);
    }

    [Fact]
    public void Character_RoundTripsCurrentStringClassAndRace()
    {
        var character = new Character("test", new Stats()) { Class = CharacterClass.Druid, Race = CharacterRace.Gavon };

        var json = JsonSerializer.Serialize(character);
        var roundTripped = JsonSerializer.Deserialize<Character>(json);

        Assert.Contains("\"Druid\"", json);
        Assert.Contains("\"Gavon\"", json);
        Assert.Equal(CharacterClass.Druid, roundTripped!.Class);
        Assert.Equal(CharacterRace.Gavon, roundTripped.Race);
    }
}
