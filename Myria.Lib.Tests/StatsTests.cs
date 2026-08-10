using Myria.Lib.Core.Entities;
using Myria.Lib.Core.Entities.Monsters;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// Stats' Strength/Dexterity/.../*Bonus properties used to be independent auto-properties. They're
/// now pass-throughs over two Dictionary&lt;string,int&gt; backing stores (BaseValues/BonusValues,
/// keyed by "STR"/"DEX"/"END"/"INT"/"SPR" — the same convention ClassManager/RaceProfile/
/// ClassProfile's StatGrowth dictionaries and Skill.StatToScaleFrom already used), so a mod-added
/// stat key can be stored and leveled up generically. CombatEntity's derived-stat formulas are
/// deliberately NOT touched in this pass — a mod-added stat won't do anything in combat math yet,
/// only Stats' own storage and Character.LevelUp/RecalculateUnusedPoints are generic now.
/// </summary>
public class StatsTests
{
    [Fact]
    public void DefaultValues_MatchThePreConversionDefaults()
    {
        var stats = new Stats();

        Assert.Equal(10, stats.Strength);
        Assert.Equal(10, stats.Dexterity);
        Assert.Equal(10, stats.Endurance);
        Assert.Equal(10, stats.Intelligence);
        Assert.Equal(10, stats.Spirit);
        Assert.Equal(0, stats.StrengthBonus);
        Assert.Equal(30, stats.BaseHealth);
        Assert.Equal(30, stats.BaseMana);
    }

    [Fact]
    public void NamedProperties_RoundTripThroughGenericAccessors()
    {
        var stats = new Stats();

        stats.Strength = 15;
        stats.StrengthBonus = 3;

        Assert.Equal(15, stats.GetBase("STR"));
        Assert.Equal(3, stats.GetBonus("STR"));
        Assert.Equal(18, stats.GetTotal("STR"));
        Assert.Equal(18, stats.TotalStrength);
        Assert.Equal(3, stats.StrengthAdded); // compat alias
    }

    [Fact]
    public void SetBase_SetBonus_WriteThroughToNamedProperties()
    {
        var stats = new Stats();

        stats.SetBase("DEX", 20);
        stats.SetBonus("DEX", 5);

        Assert.Equal(20, stats.Dexterity);
        Assert.Equal(5, stats.DexterityBonus);
    }

    [Fact]
    public void ModAddedStatKey_IsStoredAndSummedGenerically_EvenThoughNothingReadsItInCombatYet()
    {
        var stats = new Stats();
        stats.SetBase("LCK", 5);
        stats.SetBonus("LCK", 2);

        Assert.Equal(7, stats.GetTotal("LCK"));
        Assert.Contains("LCK", stats.BaseValues.Keys);
        Assert.Equal(2, stats.BonusValues["LCK"]); // included in RecalculateUnusedPoints' generic sum
    }

    [Fact]
    public void Clone_ProducesAnIndependentDeepCopy_NotASharedDictionaryReference()
    {
        var original = new Stats { Strength = 12 };
        var clone = original.Clone();

        clone.Strength = 99;
        clone.SetBase("LCK", 1);

        Assert.Equal(12, original.Strength); // unaffected by mutating the clone
        Assert.DoesNotContain("LCK", original.BaseValues.Keys);
    }

    [Fact]
    public void MonsterClone_UsesStatsClone_AndIsIndependent()
    {
        var monster = new Monster(1, "Goblin", new Stats { Strength = 8 }, "", 5);
        var clone = monster.Clone();

        clone.Stats.Strength = 50;

        Assert.Equal(8, monster.Stats.Strength);
    }

    [Fact]
    public void GetAddedStatBonus_StillComputesFromNamedBonusFields()
    {
        var stats = new Stats { EnduranceBonus = 4 };
        Assert.Equal(40, stats.GetAddedStatBonus(DerivedStatType.MaxHealth));
    }
}
