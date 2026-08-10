using Myria.Lib.Core.Entities.Characters;
using Xunit;

namespace Myria.Lib.Tests;

public class MoneyTests
{
    [Fact]
    public void FromComponents_ThenDecompose_RoundTripsExactly()
    {
        var money = Money.FromComponents(crystals: 2, platinum: 3, gold: 4, silver: 5, bronze: 6);

        money.Decompose(out long crystals, out long platinum, out long gold, out long silver, out long bronze);

        Assert.Equal(2, crystals);
        Assert.Equal(3, platinum);
        Assert.Equal(4, gold);
        Assert.Equal(5, silver);
        Assert.Equal(6, bronze);
    }

    [Fact]
    public void FromComponents_UsesCorrectDenominationLadder()
    {
        // 1 silver = 1,000 bronze; 1 gold = 1,000 silver = 1,000,000 bronze;
        // 1 platinum = 100 gold; 1 crystal = 100 platinum.
        Assert.Equal(1_000L, Money.FromComponents(0, 0, 0, 1, 0).BronzeTotal);
        Assert.Equal(1_000_000L, Money.FromComponents(0, 0, 1, 0, 0).BronzeTotal);
        Assert.Equal(100_000_000L, Money.FromComponents(0, 1, 0, 0, 0).BronzeTotal);
        Assert.Equal(10_000_000_000L, Money.FromComponents(1, 0, 0, 0, 0).BronzeTotal);
    }

    [Fact]
    public void Arithmetic_AddAndSubtract_OperateOnBronzeTotal()
    {
        var a = new Money(500);
        var b = new Money(300);

        Assert.Equal(800, (a + b).BronzeTotal);
        Assert.Equal(200, (a - b).BronzeTotal);
        Assert.Equal(-500, (-a).BronzeTotal);
    }

    [Theory]
    [InlineData(100, 200, true)]
    [InlineData(200, 100, false)]
    [InlineData(100, 100, false)]
    public void LessThan_ComparesBronzeTotal(long left, long right, bool expected)
    {
        Assert.Equal(expected, new Money(left) < new Money(right));
    }

    [Fact]
    public void Equality_ComparesByBronzeTotal_NotReferenceIdentity()
    {
        var a = new Money(1234);
        var b = new Money(1234);

        Assert.True(a == b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_RawFormat_ShowsBronzeTotalUnconverted()
    {
        // Explicit InvariantCulture: N0's group separator depends on the running
        // machine's locale (e.g. de-DE uses '.' instead of ','), so this pins the
        // format regardless of where the test suite runs.
        var money = Money.FromComponents(0, 0, 1, 0, 0); // 1 gold = 1,000,000 bronze
        Assert.Equal("1,000,000 Bronze", money.ToString("B", System.Globalization.CultureInfo.InvariantCulture));
    }
}

public class MoneyBagTests
{
    [Fact]
    public void CanAfford_ReflectsCurrentBalance()
    {
        var bag = new MoneyBag { Balance = new Money(500) };

        Assert.True(bag.CanAfford(500));
        Assert.True(bag.CanAfford(499));
        Assert.False(bag.CanAfford(501));
    }

    [Fact]
    public void TryAdd_RespectsCapacityCeiling()
    {
        var bag = new MoneyBag { Capacity = 1_000, Balance = new Money(900) };

        Assert.True(bag.TryAdd(100));   // exactly fills capacity
        Assert.Equal(1_000, bag.Balance.BronzeTotal);

        Assert.False(bag.TryAdd(1));    // would exceed capacity
        Assert.Equal(1_000, bag.Balance.BronzeTotal); // unchanged on failure
    }

    [Fact]
    public void TrySpend_FailsWithoutMutatingBalance_WhenInsufficientFunds()
    {
        var bag = new MoneyBag { Balance = new Money(50) };

        Assert.False(bag.TrySpend(51));
        Assert.Equal(50, bag.Balance.BronzeTotal);

        Assert.True(bag.TrySpend(50));
        Assert.Equal(0, bag.Balance.BronzeTotal);
    }

    [Fact]
    public void IsUnlimited_TrueOnlyAtLongMaxValueCapacity()
    {
        var unlimited = new MoneyBag { Capacity = long.MaxValue };
        var limited = new MoneyBag { Capacity = 1_000 };

        Assert.True(unlimited.IsUnlimited);
        Assert.False(limited.IsUnlimited);
        Assert.True(unlimited.CanHold(long.MaxValue - 1)); // no ceiling to violate
    }
}
