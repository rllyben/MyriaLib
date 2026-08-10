using Myria.Lib.Core.Services;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// Loads the real Data/common/*.json content once for the whole test run via
/// GameService.InitializeGame(). Myria.Lib.Core's loaders are static, process-wide state
/// (see README §21, Known Limitations), so this must run exactly once and be shared
/// — never re-initialize per test class/method.
/// </summary>
public sealed class GameDataFixture
{
    public GameDataFixture()
    {
        GameService.InitializeGame();
    }
}

/// <summary>
/// Apply <c>[Collection("GameData")]</c> to any test class whose tests rely on loaded
/// content (items, races, classes, skills, ...). Tests that construct everything by
/// hand (pure formulas, value types) don't need this collection.
/// </summary>
[CollectionDefinition("GameData")]
public sealed class GameDataCollection : ICollectionFixture<GameDataFixture>
{
}
