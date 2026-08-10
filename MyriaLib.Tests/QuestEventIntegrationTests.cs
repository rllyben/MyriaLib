using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.NPCs;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

/// <summary>
/// Quest kill/item progress used to be tracked by Inventory.AddItem and CombatEncounter/
/// GroupCombatEncounter reaching directly into Quest. That's now inverted: Inventory and the
/// combat encounters only fire GameEvents (ItemReceived/MonsterKilled), and QuestManager
/// subscribes to them via GameService.InitializeGame() (NOT a static constructor — see
/// QuestManager.OnItemReceived's doc comment for why that would silently never fire). These
/// tests exercise that wiring end-to-end through the public GameEvents.Fire* entry points,
/// rather than calling QuestManager.UpdateItemProgress/UpdateKillProgress directly, so a
/// regression in the subscription itself (not just the logic) would be caught. That's also
/// why this needs [Collection("GameData")]: the fixture is what actually calls
/// GameService.InitializeGame() and wires the subscription before these tests run.
/// </summary>
[Collection("GameData")]
public class QuestEventIntegrationTests
{
    public QuestEventIntegrationTests(GameDataFixture _) { }

    private static Quest MakeQuest(
        Dictionary<int, int>? requiredKills = null,
        Dictionary<string, int>? requiredItems = null) => new()
    {
        Id = "test_quest",
        Name = "Test Quest",
        Description = "",
        Status = QuestStatus.InProgress,
        RequiredKills = requiredKills ?? new(),
        RequiredItems = requiredItems ?? new(),
    };

    [Fact]
    public void FireItemReceived_UpdatesQuestItemProgress_AndCompletesItemOnlyQuest()
    {
        var character = TestHelpers.CreateCharacter();
        var quest = MakeQuest(requiredItems: new() { ["iron_ore"] = 3 });
        character.ActiveQuests.Add(quest);

        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 3 };
        character.Inventory.Items.Add(item);

        GameEvents.FireItemReceived(character, item, 3);

        Assert.Equal(3, quest.ItemProgress["iron_ore"]);
        Assert.Equal(QuestStatus.Completed, quest.Status);
    }

    [Fact]
    public void FireItemReceived_PartialProgress_DoesNotCompleteQuest()
    {
        var character = TestHelpers.CreateCharacter();
        var quest = MakeQuest(requiredItems: new() { ["iron_ore"] = 5 });
        character.ActiveQuests.Add(quest);

        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 2 };
        character.Inventory.Items.Add(item);

        GameEvents.FireItemReceived(character, item, 2);

        Assert.Equal(2, quest.ItemProgress["iron_ore"]);
        Assert.Equal(QuestStatus.InProgress, quest.Status);
    }

    [Fact]
    public void FireMonsterKilled_UpdatesKillProgress_AndCompletesKillOnlyQuest()
    {
        var character = TestHelpers.CreateCharacter();
        var quest = MakeQuest(requiredKills: new() { [42] = 1 });
        character.ActiveQuests.Add(quest);

        var monster = new Monster(42, "Goblin", new MyriaLib.Entities.Stats(), "", 10);

        GameEvents.FireMonsterKilled(character, monster);

        Assert.Equal(1, quest.KillProgress[42]);
        Assert.Equal(QuestStatus.Completed, quest.Status);
    }

    [Fact]
    public void MixedKillAndItemQuest_OnlyCompletesWhenBothObjectivesAreMet()
    {
        // Regression test for the bug found while centralizing this logic: CombatEncounter and
        // GroupCombatEncounter each used to complete a quest as soon as kills were satisfied,
        // ignoring any still-unmet item requirement. QuestManager.TryCompleteQuest now checks both.
        var character = TestHelpers.CreateCharacter();
        var quest = MakeQuest(
            requiredKills: new() { [42] = 1 },
            requiredItems: new() { ["iron_ore"] = 1 });
        character.ActiveQuests.Add(quest);

        var monster = new Monster(42, "Goblin", new MyriaLib.Entities.Stats(), "", 10);
        GameEvents.FireMonsterKilled(character, monster);

        Assert.Equal(1, quest.KillProgress[42]);
        Assert.Equal(QuestStatus.InProgress, quest.Status); // item requirement still unmet

        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 1 };
        character.Inventory.Items.Add(item);
        GameEvents.FireItemReceived(character, item, 1);

        Assert.Equal(QuestStatus.Completed, quest.Status); // now both objectives are met
    }

    [Fact]
    public void KillProgress_NeverExceedsRequiredCount_ForRepeatKillsPastTheGoal()
    {
        var character = TestHelpers.CreateCharacter();
        var quest = MakeQuest(requiredKills: new() { [42] = 2 });
        character.ActiveQuests.Add(quest);

        var monster = new Monster(42, "Goblin", new MyriaLib.Entities.Stats(), "", 10);
        GameEvents.FireMonsterKilled(character, monster);
        GameEvents.FireMonsterKilled(character, monster);
        GameEvents.FireMonsterKilled(character, monster); // a third kill past the requirement

        Assert.Equal(2, quest.KillProgress[42]);
        Assert.Equal(QuestStatus.Completed, quest.Status);
    }

    [Fact]
    public void NotInProgressQuests_AreIgnoredByBothEventHandlers()
    {
        var character = TestHelpers.CreateCharacter();
        var completedQuest = MakeQuest(requiredKills: new() { [42] = 1 });
        completedQuest.Status = QuestStatus.NotStarted; // not accepted — should be untouched
        character.ActiveQuests.Add(completedQuest);

        var monster = new Monster(42, "Goblin", new MyriaLib.Entities.Stats(), "", 10);
        GameEvents.FireMonsterKilled(character, monster);

        Assert.Empty(completedQuest.KillProgress);
        Assert.Equal(QuestStatus.NotStarted, completedQuest.Status);
    }
}
