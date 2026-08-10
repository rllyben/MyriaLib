using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Xunit;

namespace Myria.Lib.Tests;

/// <summary>
/// GameConfig's setters just forward to other static classes' settable properties (see
/// GameConfig.cs) — these tests verify that forwarding, and restore every touched value
/// to its snapshot afterward, since all of them are shared process-wide state.
/// </summary>
public class GameConfigTests
{
    [Fact]
    public void SetCurrencyRatios_UpdatesMoneyDenominationLadder()
    {
        long origBS = Money.BRONZE_PER_SILVER, origSG = Money.SILVER_PER_GOLD,
             origGP = Money.GOLD_PER_PLATINUM, origPC = Money.PLATINUM_PER_CRYSTAL;
        try
        {
            GameConfig.SetCurrencyRatios(bronzePerSilver: 10, silverPerGold: 10, goldPerPlatinum: 10, platinumPerCrystal: 10);

            Assert.Equal(10, Money.BRONZE_PER_SILVER);
            Assert.Equal(10, Money.SILVER_PER_GOLD);
            Assert.Equal(10, Money.GOLD_PER_PLATINUM);
            Assert.Equal(10, Money.PLATINUM_PER_CRYSTAL);
            Assert.Equal(100, Money.BRONZE_PER_GOLD);       // derived: 10 * 10
            Assert.Equal(1_000, Money.BRONZE_PER_PLATINUM); // derived: 100 * 10
        }
        finally
        {
            GameConfig.SetCurrencyRatios(origBS, origSG, origGP, origPC);
        }
    }

    [Fact]
    public void SetInventoryPageSize_UpdatesInventoryPageSize()
    {
        int original = Inventory.PageSize;
        try
        {
            GameConfig.SetInventoryPageSize(20);
            Assert.Equal(20, Inventory.PageSize);
        }
        finally
        {
            GameConfig.SetInventoryPageSize(original);
        }
    }

    [Fact]
    public void SetSkillSlotBreakpoints_UpdatesCharacterSkillSlotBreakpoints()
    {
        var original = Character.SkillSlotBreakpoints;
        try
        {
            var custom = new[] { (5, 42) };
            GameConfig.SetSkillSlotBreakpoints(custom);
            Assert.Equal(custom, Character.SkillSlotBreakpoints);
        }
        finally
        {
            GameConfig.SetSkillSlotBreakpoints(original);
        }
    }

    [Fact]
    public void SetClassProgression_UpdatesClassXpService()
    {
        int origMax = ClassXpService.MaxLevel;
        long origCost = ClassXpService.XpCostBase;
        try
        {
            GameConfig.SetClassProgression(maxLevel: 30, xpCostBase: 1_234);

            Assert.Equal(30, ClassXpService.MaxLevel);
            Assert.Equal(1_234, ClassXpService.XpCostBase);
        }
        finally
        {
            GameConfig.SetClassProgression(origMax, origCost);
        }
    }

    [Fact]
    public void SetJobProgression_UpdatesJobXpService_IncludingOptionalBonusParameters()
    {
        int origMax = JobXpService.MaxLevel;
        long origCost = JobXpService.XpCostBase;
        double origFame = JobXpService.MaxFameBonus, origSkill = JobXpService.MaxSkillBonus;
        try
        {
            GameConfig.SetJobProgression(maxLevel: 80, xpCostBase: 2_000, maxFameBonus: 0.75, maxSkillBonus: 1.25);

            Assert.Equal(80, JobXpService.MaxLevel);
            Assert.Equal(2_000, JobXpService.XpCostBase);
            Assert.Equal(0.75, JobXpService.MaxFameBonus);
            Assert.Equal(1.25, JobXpService.MaxSkillBonus);
        }
        finally
        {
            GameConfig.SetJobProgression(origMax, origCost, origFame, origSkill);
        }
    }

    [Fact]
    public void SetClassCooldown_UpdatesClassManagerCooldown()
    {
        var original = ClassManager.ClassChangeCooldown;
        try
        {
            GameConfig.SetClassCooldown(TimeSpan.FromDays(3));
            Assert.Equal(TimeSpan.FromDays(3), ClassManager.ClassChangeCooldown);
        }
        finally
        {
            GameConfig.SetClassCooldown(original);
        }
    }

    [Fact]
    public void SetClassPenaltyPerDay_UpdatesClassManagerPenalty()
    {
        long original = ClassManager.PenaltyPerDay;
        try
        {
            GameConfig.SetClassPenaltyPerDay(999);
            Assert.Equal(999, ClassManager.PenaltyPerDay);
        }
        finally
        {
            GameConfig.SetClassPenaltyPerDay(original);
        }
    }

    [Fact]
    public void SetJobCooldown_UpdatesJobManagerCooldown()
    {
        var original = JobManager.JobChangeCooldown;
        try
        {
            GameConfig.SetJobCooldown(TimeSpan.FromDays(1));
            Assert.Equal(TimeSpan.FromDays(1), JobManager.JobChangeCooldown);
        }
        finally
        {
            GameConfig.SetJobCooldown(original);
        }
    }

    [Fact]
    public void SetJobDailyMechanics_UpdatesAllFourJobManagerValues()
    {
        long origFameTick = JobManager.FameTickPerDay, origDecayCap = JobManager.SkillDecayCap, origDecayDiv = JobManager.FameDecayDivisor;
        double origBonus = JobManager.ActiveJobBonusFraction;
        try
        {
            GameConfig.SetJobDailyMechanics(fameTickPerDay: 10, skillDecayCap: 25, fameDecayDivisor: 100, activeJobBonusFraction: 0.25);

            Assert.Equal(10, JobManager.FameTickPerDay);
            Assert.Equal(25, JobManager.SkillDecayCap);
            Assert.Equal(100, JobManager.FameDecayDivisor);
            Assert.Equal(0.25, JobManager.ActiveJobBonusFraction);
        }
        finally
        {
            GameConfig.SetJobDailyMechanics(origFameTick, origDecayCap, origDecayDiv, origBonus);
        }
    }
}
