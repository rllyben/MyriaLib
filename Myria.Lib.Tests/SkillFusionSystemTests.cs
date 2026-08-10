using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Models.BaseModel;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// SkillFusionSystem.Fuse() is a pure function that, as of this writing, nothing in the
/// production app actually calls (see README §21, Known Limitations — the fusion-skill
/// creation UI doesn't exist yet). These tests exercise the algorithm directly so the
/// rules are verified by *something* even though no player-facing path reaches them yet.
/// </summary>
public class SkillFusionSystemTests
{
    private static BaseSkillData MakeComponent(
        string id, string name, string[] componentTypes, int manaCost, float scalingFactor,
        string statToScaleFrom, int requiredLevel) => new()
    {
        Id = id,
        Name = name,
        Description = "",
        ComponentType = componentTypes.ToList(),
        Class = "Fighter",
        ManaCost = manaCost,
        ScalingFactor = scalingFactor,
        StatToScaleFrom = statToScaleFrom,
        RequiredLevel = requiredLevel,
    };

    [Fact]
    public void Fuse_TwoDifferentComponents_DerivesStatsFromHigherScalingComponent()
    {
        var slash = MakeComponent("slash", "Slash", new[] { "Physical" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "ATK", requiredLevel: 2);
        var fireBolt = MakeComponent("fire_bolt", "Fire Bolt", new[] { "Fire", "Magic" }, manaCost: 15, scalingFactor: 1.2f, statToScaleFrom: "MATK", requiredLevel: 3);

        var result = SkillFusionSystem.Fuse(new List<BaseSkillData> { slash, fireBolt });

        Assert.NotNull(result);
        // scalingFactor = max(1.0, 1.2) + (count-1) * ScalingPerComponent(0.3) = 1.2 + 0.3 = 1.5
        Assert.Equal(1.5f, result!.ScalingFactor, precision: 3);
        // StatToScaleFrom comes from whichever component has the highest ScalingFactor (fire_bolt).
        Assert.Equal("MATK", result.StatToScaleFrom);
        Assert.False(result.IsHealing);
        Assert.Equal(SkillType.Magical, result.Type); // allTypes contains "Magic"
        Assert.Equal(SkillTarget.SingleEnemy, result.Target); // no Self/Area/MultiHit, no id repeated 3x
        // manaCost = max(10,15) + (components[1].ManaCost * 0.6) = 15 + (int)(15*0.6) = 15 + 9 = 24
        Assert.Equal(24, result.ManaCost);
        Assert.Equal(3, result.MinLevel); // max(RequiredLevel) across components
        Assert.Equal("fusion_fire_bolt_slash", result.Id); // ids joined in sorted (ordinal) order
        Assert.Equal("Slash Fire Bolt", result.Name); // names joined in the caller-supplied order
    }

    [Fact]
    public void Fuse_ThreeIdenticalComponents_TriggersGrandNamingAndAoeTargeting()
    {
        var fireBolt = MakeComponent("fire_bolt", "Fire Bolt", new[] { "Fire" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "MATK", requiredLevel: 1);
        var components = new List<BaseSkillData> { fireBolt, fireBolt, fireBolt };

        var result = SkillFusionSystem.Fuse(components);

        Assert.NotNull(result);
        Assert.Equal("Grand Fire Bolt", result!.Name); // all components share the same Id
        Assert.Equal(SkillTarget.AllEnemies, result.Target); // stacking the same skill 3x (AoeStackThreshold) promotes to AoE
        Assert.Equal(SkillType.Physical, result.Type); // no "Magic" tag present, despite the Fire element
        // scalingFactor = 1.0 + (3-1)*0.3 = 1.6
        Assert.Equal(1.6f, result.ScalingFactor, precision: 3);
        // manaCost = max(10,10,10) + 2 * (int)(10*0.6) = 10 + 6 + 6 = 22
        Assert.Equal(22, result.ManaCost);
    }

    [Fact]
    public void Fuse_SelfComponent_ForcesSelfTargetEvenWithOtherComponents()
    {
        var heal = MakeComponent("minor_heal", "Minor Heal", new[] { "Heal", "Self" }, manaCost: 8, scalingFactor: 0.8f, statToScaleFrom: "SPR", requiredLevel: 1);

        var result = SkillFusionSystem.Fuse(new List<BaseSkillData> { heal });

        Assert.NotNull(result);
        Assert.True(result!.IsHealing);
        Assert.Equal(SkillTarget.Self, result.Target);
    }

    [Fact]
    public void Fuse_EmptyComponentList_ReturnsNull()
    {
        Assert.Null(SkillFusionSystem.Fuse(new List<BaseSkillData>()));
    }

    [Fact]
    public void Fuse_NamedRecipeOverride_ReplacesIdentityAndAppliesStatOverrides()
    {
        var slash = MakeComponent("slash", "Slash", new[] { "Physical" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "ATK", requiredLevel: 1);
        var fireBolt = MakeComponent("fire_bolt", "Fire Bolt", new[] { "Fire" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "ATK", requiredLevel: 1);

        // FusionRecipeService is static/process-wide state (see README §21) with no public
        // accessor for its loaded recipes, so there's nothing to snapshot — register a
        // recipe just for this test, then reset to empty (not restored) afterward. Safe
        // today because fusion_recipes.json doesn't exist yet (see README §21) so no other
        // test in this suite depends on real content being loaded here.
        try
        {
            FusionRecipeService.Load(new List<FusionRecipe>
            {
                new()
                {
                    ComponentIds = new List<string> { "fire_bolt", "slash" },
                    ResultId = "flaming_strike",
                    ResultName = "Flaming Strike",
                    ResultDescription = "A blazing blade technique.",
                    ScalingFactorOverride = 9.9f,
                    ManaCostOverride = 42,
                    TargetOverride = "AllEnemies",
                },
            });

            var result = SkillFusionSystem.Fuse(new List<BaseSkillData> { slash, fireBolt });

            Assert.NotNull(result);
            Assert.Equal("flaming_strike", result!.Id);
            Assert.Equal("Flaming Strike", result.Name);
            Assert.Equal("A blazing blade technique.", result.Description);
            Assert.Equal(9.9f, result.ScalingFactor, precision: 3);
            Assert.Equal(42, result.ManaCost);
            Assert.Equal(SkillTarget.AllEnemies, result.Target);
        }
        finally
        {
            FusionRecipeService.Load(new List<FusionRecipe>()); // reset shared static state
        }
    }

    [Fact]
    public void DeriveTargetOverride_WhenSet_ReplacesTheBuiltInTargetingRules()
    {
        var original = SkillFusionSystem.DeriveTargetOverride;
        try
        {
            // Default rules would resolve this single, non-Self/Area component to SingleEnemy —
            // the override forces AllAllies instead, proving it fully replaces (not augments) the
            // built-in DeriveTarget logic.
            SkillFusionSystem.DeriveTargetOverride = (_, _) => SkillTarget.AllAllies;
            var slash = MakeComponent("slash", "Slash", new[] { "Physical" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "ATK", requiredLevel: 1);

            var result = SkillFusionSystem.Fuse(new List<BaseSkillData> { slash });

            Assert.Equal(SkillTarget.AllAllies, result!.Target);
        }
        finally
        {
            SkillFusionSystem.DeriveTargetOverride = original;
        }
    }

    [Fact]
    public void BuildFusionNameOverride_WhenSet_ReplacesTheBuiltInNamingRules()
    {
        var original = SkillFusionSystem.BuildFusionNameOverride;
        try
        {
            SkillFusionSystem.BuildFusionNameOverride = _ => "Custom Fusion Name";
            var slash = MakeComponent("slash", "Slash", new[] { "Physical" }, manaCost: 10, scalingFactor: 1.0f, statToScaleFrom: "ATK", requiredLevel: 1);

            var result = SkillFusionSystem.Fuse(new List<BaseSkillData> { slash });

            Assert.Equal("Custom Fusion Name", result!.Name);
        }
        finally
        {
            SkillFusionSystem.BuildFusionNameOverride = original;
        }
    }
}
