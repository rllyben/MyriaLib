using Myria.Lib.Core.Systems;
using Xunit;

namespace Myria.Lib.Tests;

public class CombatSystemTests
{
    [Fact]
    public void DamageSteepness_IsSettable_AndAffectsExponentialDamage()
    {
        float original = CombatSystem.DamageSteepness;
        try
        {
            // Zero steepness collapses the exponential term to 1 regardless of the atk/def gap.
            CombatSystem.DamageSteepness = 0f;
            Assert.Equal(40f, CombatSystem.ExponentialDamage(100f, 0f), precision: 3);
        }
        finally
        {
            CombatSystem.DamageSteepness = original;
        }
    }

    [Fact]
    public void TryHit_AimEqualToEvasion_IsGuaranteedHit()
    {
        var attacker = new TestCombatant { TotalAim = 50 };
        var defender = new TestCombatant { TotalEvasion = 50 };

        // aim >= evasion is a guaranteed hit — repeat many times to prove it's not
        // merely a high-probability roll that happened to pass once.
        for (int i = 0; i < 100; i++)
            Assert.True(CombatSystem.TryHit(attacker, defender));
    }

    [Fact]
    public void TryHit_AimHigherThanEvasion_IsGuaranteedHit()
    {
        var attacker = new TestCombatant { TotalAim = 200 };
        var defender = new TestCombatant { TotalEvasion = 10 };

        for (int i = 0; i < 100; i++)
            Assert.True(CombatSystem.TryHit(attacker, defender));
    }

    [Fact]
    public void ExponentialDamage_EqualAttackAndDefense_IsExactlyFortyPercentOfAttack()
    {
        // Gap is zero, so the exponential term is exp(0) = 1.
        Assert.Equal(40f, CombatSystem.ExponentialDamage(100f, 100f), precision: 3);
    }

    [Fact]
    public void ExponentialDamage_ZeroAttack_IsZeroRegardlessOfDefense()
    {
        Assert.Equal(0f, CombatSystem.ExponentialDamage(0f, 100f));
    }

    [Fact]
    public void ExponentialDamage_ZeroDefense_MatchesFormulaWithFullExponentialBonus()
    {
        // atk * 0.4 * exp(0.8 * (atk - 0) / (atk + 0)) = atk * 0.4 * exp(0.8)
        float expected = 100f * 0.4f * MathF.Exp(0.8f);
        Assert.Equal(expected, CombatSystem.ExponentialDamage(100f, 0f), precision: 3);
    }

    [Fact]
    public void ExponentialDamage_ZeroSum_ReturnsZeroInsteadOfDividingByZero()
    {
        Assert.Equal(0f, CombatSystem.ExponentialDamage(0f, 0f));
    }

    [Fact]
    public void CalculateDamage_GuaranteedHitNoBlockNoMagic_ReturnsExactExponentialDamage()
    {
        var attacker = new TestCombatant
        {
            TotalAim = 100,
            TotalPhysicalAttack = 100,
            TotalMagicAttack = 0,
        };
        var defender = new TestCombatant
        {
            TotalEvasion = 0,          // attacker's aim >= this, so always hits
            TotalPhysicalDefense = 100,
            TotalMagicDefense = 100,
            BlockChance = 0f,          // never blocks
        };

        // Equal physical atk/def -> ExponentialDamage(100,100) == 40; magic side is 0
        // attack -> 0 damage; CalculateDamage takes the max of the two, then floors to int.
        int dmg = CombatSystem.CalculateDamage(attacker, defender);

        Assert.Equal(40, dmg);
    }

    [Fact]
    public void CalculateDamage_MissedAttack_ReturnsZero()
    {
        var attacker = new TestCombatant { TotalAim = 0, TotalPhysicalAttack = 999 };
        var defender = new TestCombatant { TotalEvasion = int.MaxValue, TotalPhysicalDefense = 1 };

        // aim (0) is never >= evasion (huge), and the resulting hitChance (0/huge) rounds to
        // ~0, so across many attempts this should never register as a hit.
        for (int i = 0; i < 50; i++)
            Assert.Equal(0, CombatSystem.CalculateDamage(attacker, defender));
    }

    [Fact]
    public void CalculateDamage_NeverReturnsLessThanOne_OnAHit()
    {
        // Guaranteed hit, but attack is tiny relative to defense — CalculateDamage still
        // floors at 1 rather than returning 0 for a landed hit.
        var attacker = new TestCombatant { TotalAim = 100, TotalPhysicalAttack = 1, TotalMagicAttack = 0 };
        var defender = new TestCombatant { TotalEvasion = 0, TotalPhysicalDefense = 1000, TotalMagicDefense = 1000, BlockChance = 0f };

        int dmg = CombatSystem.CalculateDamage(attacker, defender);
        Assert.True(dmg >= 1);
    }
}
