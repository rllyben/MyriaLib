using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// RuneWordService holds static, process-wide word/family/pair data (see README §21,
/// Known Limitations). Each test loads its own small, controlled vocabulary via the
/// in-memory Load() overload and resets to empty in `finally`.
/// <para>
/// Note this resets to *empty*, not to whatever was loaded before (unlike
/// <see cref="ClassManagerTests"/>, which snapshots and restores) — RuneWordService
/// exposes no public accessor for its loaded word-pair data, so there's nothing to
/// snapshot. This is safe today because no other test in this suite depends on the
/// real Data/common/rune_*.json content being loaded; if a future test needs that,
/// it must not share a test run with these without accounting for this reset.
/// </para>
/// </summary>
public class RuneEvaluatorTests
{
    private static BaseRuneData MakeTestRune() => new()
    {
        Id = "test_rune",
        Name = "Test Rune",
        Description = "A test rune.",
        CoreWordId = "ember",
        Class = "RunicMage",
        BaseManaCost = 20,
        BaseScalingFactor = 1.0f,
        StatToScaleFrom = "MATK",
        Target = "SingleEnemy",
        IsHealing = false,
    };

    private static void WithVocabulary(List<RuneWord> words, List<WordFamily> families, List<WordPairRelation> pairs, Action test)
    {
        try
        {
            RuneWordService.Load(words, families, pairs);
            test();
        }
        finally
        {
            RuneWordService.Load(new List<RuneWord>(), new List<WordFamily>(), new List<WordPairRelation>());
        }
    }

    [Fact]
    public void Evaluate_SameFamilyWord_IsSupportAndIncreasesScaling()
    {
        var words = new List<RuneWord>
        {
            new() { Id = "ember", EnglishName = "Ember", RunicScript = "?", FamilyId = "fire" },
            new() { Id = "blaze", EnglishName = "Blaze", RunicScript = "?", FamilyId = "fire" },
        };

        WithVocabulary(words, new List<WordFamily>(), new List<WordPairRelation>(), () =>
        {
            var result = RuneEvaluator.Evaluate(MakeTestRune(), new List<RuneWord> { words[1] });

            Assert.NotNull(result);
            // Same family always resolves to Support: 1.0 + 1 * SupportBonus(0.25) = 1.25
            Assert.Equal(1.25f, result!.ScalingFactor, precision: 3);
            Assert.Equal(20, result.ManaCost); // unchanged — no Neutral pairs
            Assert.Equal("Test Rune: Blaze", result.Name);
            Assert.Equal("rune_test_rune_blaze", result.Id);
        });
    }

    [Fact]
    public void Evaluate_FamilyLevelContradiction_IncreasesScalingByContradictionBonus()
    {
        var words = new List<RuneWord>
        {
            new() { Id = "ember", EnglishName = "Ember", RunicScript = "?", FamilyId = "fire" },
            new() { Id = "frost", EnglishName = "Frost", RunicScript = "?", FamilyId = "ice" },
        };
        var families = new List<WordFamily>
        {
            new() { Id = "fire", Name = "Fire", FamilyRelations = new() { ["ice"] = WordRelationship.Contradiction } },
        };

        WithVocabulary(words, families, new List<WordPairRelation>(), () =>
        {
            var result = RuneEvaluator.Evaluate(MakeTestRune(), new List<RuneWord> { words[1] });

            Assert.NotNull(result);
            // 1.0 + 1 * ContradictionBonus(0.20) = 1.20
            Assert.Equal(1.20f, result!.ScalingFactor, precision: 3);
            Assert.Equal(20, result.ManaCost);
        });
    }

    [Fact]
    public void Evaluate_UnrelatedFamilies_FallBackToNeutralAndAddManaCost()
    {
        var words = new List<RuneWord>
        {
            new() { Id = "ember", EnglishName = "Ember", RunicScript = "?", FamilyId = "fire" },
            new() { Id = "stone", EnglishName = "Stone", RunicScript = "?", FamilyId = "earth" },
        };
        // No WordFamily entries at all — fire/earth have no defined relationship in either direction.

        WithVocabulary(words, new List<WordFamily>(), new List<WordPairRelation>(), () =>
        {
            var result = RuneEvaluator.Evaluate(MakeTestRune(), new List<RuneWord> { words[1] });

            Assert.NotNull(result);
            Assert.Equal(1.0f, result!.ScalingFactor, precision: 3); // Neutral doesn't touch scaling
            // Both words in the pair count toward the penalty once each: 20 + 2 * NeutralMpPenalty(15) = 50
            Assert.Equal(50, result.ManaCost);
        });
    }

    [Fact]
    public void Evaluate_ExplicitPairOverride_TakesPrecedenceOverFamilyDefault()
    {
        var words = new List<RuneWord>
        {
            new() { Id = "ember", EnglishName = "Ember", RunicScript = "?", FamilyId = "fire" },
            new() { Id = "frost", EnglishName = "Frost", RunicScript = "?", FamilyId = "ice" },
        };
        // Family-level default would be Neutral (no WordFamily entries), but an explicit
        // pair override says these two specific words Support each other instead.
        var pairs = new List<WordPairRelation>
        {
            new() { WordIdA = "ember", WordIdB = "frost", Relationship = WordRelationship.Support },
        };

        WithVocabulary(words, new List<WordFamily>(), pairs, () =>
        {
            var result = RuneEvaluator.Evaluate(MakeTestRune(), new List<RuneWord> { words[1] });

            Assert.NotNull(result);
            Assert.Equal(1.25f, result!.ScalingFactor, precision: 3); // Support, not Neutral
            Assert.Equal(20, result.ManaCost); // no Neutral penalty applied
        });
    }

    [Fact]
    public void Evaluate_AreaFamilyWord_PromotesTargetToAllEnemies()
    {
        var words = new List<RuneWord>
        {
            new() { Id = "ember", EnglishName = "Ember", RunicScript = "?", FamilyId = "fire" },
            new() { Id = "ocean", EnglishName = "Ocean", RunicScript = "?", FamilyId = "area" },
        };

        WithVocabulary(words, new List<WordFamily>(), new List<WordPairRelation>(), () =>
        {
            var result = RuneEvaluator.Evaluate(MakeTestRune(), new List<RuneWord> { words[1] });

            Assert.NotNull(result);
            Assert.Equal(SkillTarget.AllEnemies, result!.Target); // overrides the rune's default SingleEnemy
        });
    }

    [Fact]
    public void FindTransforms_ExplicitTransformPair_ReturnsResultRuneId()
    {
        var pairs = new List<WordPairRelation>
        {
            new() { WordIdA = "gaton", WordIdB = "xuton", Relationship = WordRelationship.Transform, TransformResultRuneId = "rune_vael" },
        };

        WithVocabulary(new List<RuneWord>(), new List<WordFamily>(), pairs, () =>
        {
            var results = RuneEvaluator.FindTransforms(new List<string> { "gaton", "xuton" });

            Assert.Single(results);
            Assert.Equal("rune_vael", results[0]);
        });
    }

    [Fact]
    public void FindTransforms_NoMatchingPair_ReturnsEmptyList()
    {
        WithVocabulary(new List<RuneWord>(), new List<WordFamily>(), new List<WordPairRelation>(), () =>
        {
            var results = RuneEvaluator.FindTransforms(new List<string> { "gaton", "xuton" });
            Assert.Empty(results);
        });
    }
}
