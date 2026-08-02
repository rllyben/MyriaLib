using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Skills;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

/// <summary>
/// SkillTarget was a closed enum; it's now a string-constant class (matching CharacterClass/
/// CharacterRace/ItemRarity/GatheringType/EquipmentType), moved from Entities/Skills/Skill.cs to
/// Systems/Enums/SkillTarget.cs alongside the others. skills.json/SQL always store the current
/// string form, but character saves can carry a Skill snapshot from before this conversion with
/// Target as the old enum's raw int - unlike GatheringType, this DOES need legacy-int migration,
/// handled via SkillTarget.FromLegacyInt + SkillTargetJsonConverter (same pattern as
/// CharacterClass/CharacterRace). Also covers the GroupCombatEncounter bug fix: its execution
/// switch previously had no default arm, so an unrecognized/mod-added Target silently did nothing
/// (mana already spent); it now falls back to self-cast, matching the resolution switch's own
/// fallback.
/// </summary>
[Collection("GameData")]
public class SkillTargetTests
{
    public SkillTargetTests(GameDataFixture _) { }

    [Fact]
    public void AllBuiltIn_ContainsAllFiveTargetsInDeclarationOrder()
    {
        Assert.Equal(
            new[] { SkillTarget.SingleEnemy, SkillTarget.AllEnemies, SkillTarget.Self, SkillTarget.SingleAlly, SkillTarget.AllAllies },
            SkillTarget.AllBuiltIn);
    }

    [Fact]
    public void SkillFactory_LoadsRealSkillsJsonTarget_AsTheCorrectStringId()
    {
        // quick_shot has "Target": "SingleEnemy" in skills.json.
        var skill = SkillFactory.GetSkill("quick_shot");
        Assert.NotNull(skill);
        Assert.Equal(SkillTarget.SingleEnemy, skill!.Target);
    }

    [Theory]
    [InlineData(0, SkillTarget.SingleEnemy)]
    [InlineData(2, SkillTarget.Self)]
    [InlineData(4, SkillTarget.AllAllies)]
    public void FromLegacyInt_MapsOldEnumOrdinalsToTheCorrectStringId(int legacyOrdinal, string expected)
    {
        Assert.Equal(expected, SkillTarget.FromLegacyInt[legacyOrdinal]);
    }

    [Fact]
    public void Skill_DeserializesLegacyRawIntTarget_ToTheCorrectStringId()
    {
        // Real shape of a Skill snapshot inside a pre-conversion character save.
        var json = """{ "Id": "quick_shot", "Name": "Quick Shot", "Description": "", "Target": 0 }""";

        var skill = System.Text.Json.JsonSerializer.Deserialize<Skill>(json);

        Assert.NotNull(skill);
        Assert.Equal(SkillTarget.SingleEnemy, skill!.Target);
    }

    [Fact]
    public void GroupCombatEncounter_UnrecognizedTarget_SelfCastsInsteadOfSilentlyDoingNothing()
    {
        var caster = TestHelpers.CreateCharacter();
        caster.TakeDamage(caster.MaxHealth / 2); // leave room to observe a heal
        int damagedHealth = caster.CurrentHealth;

        var monster = new Monster(42, "Goblin", new MyriaLib.Entities.Stats(), "", 10);
        var encounter = new GroupCombatEncounter(new[] { caster }, new[] { monster });

        var skill = new Skill
        {
            Id = "test_unknown_target_heal",
            Name = "Test Heal",
            Description = "",
            ManaCost = 0,
            Type = SkillType.Magical,
            Target = "SomeModAddedTarget", // not one of the 5 built-ins
            IsHealing = true,
            ScalingFactor = 5f,
            StatToScaleFrom = "SPR",
            Effects = new(),
        };

        bool result = encounter.CharacterCastSkill(caster.Name, skill, targetIndex: 0);

        Assert.True(result);
        Assert.True(caster.CurrentHealth > damagedHealth); // healed itself — proves ExecuteSkillOnCharacter ran
        Assert.Equal(monster.MaxHealth, monster.CurrentHealth); // monster untouched
        Assert.Contains(encounter.Log, e => e.Key == "pg.fight.log.heal");
    }
}
