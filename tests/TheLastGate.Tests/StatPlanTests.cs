using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Tests;

public class StatPlanTests
{
    private const int Plenty = 1_000_000;
    private const int Dagger = 3;

    [Fact]
    public void NextCost_OfAnAttributeIsCubicInTheValueAfterPlannedRaises()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        Assert.Equal(150, plan.NextCost(0, values));
        plan.Raise(0, values, Plenty, 1);
        Assert.Equal(199, plan.NextCost(0, values));
    }

    [Theory]
    [InlineData(StatSlots.HitPoints)]
    [InlineData(StatSlots.Mana)]
    public void NextCost_OfHitPointsAndManaIsLinear(int slot)
    {
        Assert.Equal(400, new StatPlan().NextCost(slot, [100, 0, 500, 4, 0, 1250]));
    }

    [Fact]
    public void NextCost_OfASkillIsAtLeastItsValue()
    {
        Assert.Equal(3, new StatPlan().NextCost(StatSlots.Skill(Dagger), [3, 0, 50, 1, 0, 3]));
    }

    [Fact]
    public void NextCost_IsNullAtTheLimit()
    {
        int[] values = [49, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        Assert.Null(plan.NextCost(0, values));
    }

    [Fact]
    public void NextCost_IsNullForAnUnlearntSkill()
    {
        Assert.Null(new StatPlan().NextCost(StatSlots.Skill(Dagger), [0, 0, 50, 2, 0, 6]));
    }

    [Fact]
    public void Raise_PlansThePointsAndAddsUpTheirCost()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, Plenty, 2);

        Assert.Equal(2, plan.Raised(0));
        Assert.Equal(150 + 199, plan.PointsUsed);
    }

    [Fact]
    public void Raise_StopsWhenThePointsLeftDoNotCoverTheNextPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, 400, 10);

        Assert.Equal(2, plan.Raised(0));
    }

    [Fact]
    public void Raise_StopsAtTheLimit()
    {
        int[] values = [48, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, Plenty, 10);

        Assert.Equal(2, plan.Raised(0));
    }

    [Fact]
    public void Lower_RefundsTheLastPlannedPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 2);

        plan.Lower(0, values, 1);

        Assert.Equal(1, plan.Raised(0));
        Assert.Equal(150, plan.PointsUsed);
    }

    [Fact]
    public void Lower_StopsAtNothingPlanned()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        plan.Lower(0, values, 10);

        Assert.Equal(0, plan.Raised(0));
        Assert.Equal(0, plan.PointsUsed);
    }

    [Fact]
    public void Commit_SendsOneStatCommandPerPlannedSlotInSlotOrder()
    {
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), [10, 0, 50, 2, 0, 20], Plenty, 1);
        plan.Raise(StatSlots.Mana, [60, 0, 500, 3, 0, 900], Plenty, 3);
        plan.Raise(2, [10, 0, 50, 3, 0, 12], Plenty, 2);

        Assert.Equal(
            [
                ClientPackets.ShortAndInt(ClientCommand.Stat, 2, 2),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Mana, 3),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.FirstSkill + Dagger, 1),
            ],
            plan.Commit());
    }

    [Fact]
    public void Commit_SplitsSkillRaisesIntoAmountsTheServerAccepts()
    {
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), [1, 0, 300, 0, 0, 1], Plenty, 200);

        Assert.Equal(
            [
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Skill(Dagger), 135),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Skill(Dagger), 65),
            ],
            plan.Commit());
    }

    [Fact]
    public void Commit_ClearsThePlan()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        plan.Commit();

        Assert.Equal(0, plan.Raised(0));
        Assert.Equal(0, plan.PointsUsed);
        Assert.Empty(plan.Commit());
    }

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 10)]
    [InlineData(false, true, 90)]
    [InlineData(true, true, 90)]
    public void ClickTimes_RaisesTenWithShiftAndNinetyWithCtrlOrAlt(bool shift, bool ctrlOrAlt, int times)
    {
        Assert.Equal(times, StatPlan.ClickTimes(shift, ctrlOrAlt));
    }

    [Fact]
    public void Help_NamesTheStatBeingRaisedOrLowered()
    {
        Assert.Equal("Raise Dagger.", StatPlan.RaiseHelp("Dagger"));
        Assert.Equal("Lower Mana.", StatPlan.LowerHelp("Mana"));
    }

    [Fact]
    public void CostAfter_IsTheCostOfAPointBeyondThePlannedOnes()
    {
        int[] values = [10, 0, 13, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        Assert.Equal(199, plan.CostAfter(0, values, 0));
        Assert.Equal(12 * 12 * 12 * 3 / 20, plan.CostAfter(0, values, 1));
        Assert.Null(plan.CostAfter(0, values, 2));
    }

    [Fact]
    public void RaiseAll_PlansEveryPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        Assert.True(plan.RaiseAll([(0, 2), (1, 1)], _ => values, Plenty));

        Assert.Equal((2, 1), (plan.Raised(0), plan.Raised(1)));
        Assert.Equal(150 + 199 + 150, plan.PointsUsed);
    }

    [Fact]
    public void RaiseAll_PlansNothingWhenAPointCantBeBought()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        int[] capped = [10, 0, 11, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(2, values, Plenty, 1);

        Assert.False(plan.RaiseAll([(0, 1), (1, 2)], slot => slot == 1 ? capped : values, Plenty));

        Assert.Equal((0, 0, 1), (plan.Raised(0), plan.Raised(1), plan.Raised(2)));
        Assert.Equal(150, plan.PointsUsed);
    }

    [Fact]
    public void RaiseAll_PlansNothingWithTooFewPoints()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        Assert.False(plan.RaiseAll([(0, 1), (1, 1)], _ => values, 200));

        Assert.Equal(0, plan.PointsUsed);
    }

    [Fact]
    public void Reset_ForgetsThePlan()
    {
        var plan = new StatPlan();
        int[] values = [10, 0, 100, 1, 0, 0];
        plan.Raise(0, values, 100000, 3);

        plan.Reset();

        Assert.Equal(0, plan.Raised(0));
        Assert.Equal(0, plan.PointsUsed);
        Assert.Empty(plan.Commit());
    }
}
