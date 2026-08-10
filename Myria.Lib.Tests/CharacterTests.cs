using Myria.Lib.Core.Entities.Characters;
using Xunit;

namespace Myria.Lib.Tests;

public class CharacterTests
{
    [Fact]
    public void SkillSlotCount_FollowsDefaultBreakpoints()
    {
        Assert.Equal(1, TestHelpers.CreateCharacter(level: 1).SkillSlotCount);
        Assert.Equal(2, TestHelpers.CreateCharacter(level: 3).SkillSlotCount);
        Assert.Equal(2, TestHelpers.CreateCharacter(level: 8).SkillSlotCount); // just below the next breakpoint
        Assert.Equal(3, TestHelpers.CreateCharacter(level: 9).SkillSlotCount);
        Assert.Equal(10, TestHelpers.CreateCharacter(level: 100).SkillSlotCount);
    }

    [Fact]
    public void FusionSlotCount_SharesTheSameBreakpointsAsSkillSlotCount()
    {
        var character = TestHelpers.CreateCharacter(level: 45);
        Assert.Equal(character.SkillSlotCount, character.FusionSlotCount);
        Assert.Equal(7, character.FusionSlotCount);
    }

    [Fact]
    public void SkillSlotBreakpoints_IsSettable_AndAffectsBothSlotCounts()
    {
        var original = Character.SkillSlotBreakpoints;
        try
        {
            Character.SkillSlotBreakpoints = new[] { (5, 99) };
            var character = TestHelpers.CreateCharacter(level: 10);

            Assert.Equal(99, character.SkillSlotCount);
            Assert.Equal(99, character.FusionSlotCount);
        }
        finally
        {
            Character.SkillSlotBreakpoints = original;
        }
    }

    [Fact]
    public void SkillSlotBreakpoints_BelowLowestBreakpoint_FallsBackToOne()
    {
        var original = Character.SkillSlotBreakpoints;
        try
        {
            Character.SkillSlotBreakpoints = new[] { (5, 99) };
            Assert.Equal(1, TestHelpers.CreateCharacter(level: 4).SkillSlotCount);
        }
        finally
        {
            Character.SkillSlotBreakpoints = original;
        }
    }
}
