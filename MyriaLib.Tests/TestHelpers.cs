using MyriaLib.Entities;
using MyriaLib.Entities.Characters;

namespace MyriaLib.Tests;

internal static class TestHelpers
{
    /// <summary>Builds a minimal, valid Character for tests that don't care about race/class balancing.</summary>
    public static Character CreateCharacter(string name = "TestChar", int level = 1)
    {
        var stats = new Stats();
        var character = new Character(name, stats) { Level = level };
        return character;
    }
}
