using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class StatPlanTests
{
    private const int Plenty = 1_000_000;
    private const int Dagger = 3;

    [TestCase]
    public void NextCost_OfAnAttributeIsCubicInTheValueAfterPlannedRaises()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        AssertThat(plan.NextCost(0, values)).IsEqual(150);
        plan.Raise(0, values, Plenty, 1);
        AssertThat(plan.NextCost(0, values)).IsEqual(199);
    }

    [TestCase(StatSlots.HitPoints)]
    [TestCase(StatSlots.Mana)]
    public void NextCost_OfHitPointsAndManaIsLinear(int slot)
    {
        AssertThat(new StatPlan().NextCost(slot, [100, 0, 500, 4, 0, 1250])).IsEqual(400);
    }

    [TestCase]
    public void NextCost_OfASkillIsAtLeastItsValue()
    {
        AssertThat(new StatPlan().NextCost(StatSlots.Skill(Dagger), [3, 0, 50, 1, 0, 3])).IsEqual(3);
    }

    [TestCase]
    public void NextCost_IsNullAtTheLimit()
    {
        int[] values = [49, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        AssertThat(plan.NextCost(0, values)).IsNull();
    }

    [TestCase]
    public void NextCost_IsNullForAnUnlearntSkill()
    {
        AssertThat(new StatPlan().NextCost(StatSlots.Skill(Dagger), [0, 0, 50, 2, 0, 6])).IsNull();
    }

    [TestCase]
    public void Raise_PlansThePointsAndAddsUpTheirCost()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, Plenty, 2);

        AssertThat(plan.Raised(0)).IsEqual(2);
        AssertThat(plan.PointsUsed).IsEqual(150 + 199);
    }

    [TestCase]
    public void Raise_StopsWhenThePointsLeftDoNotCoverTheNextPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, 400, 10);

        AssertThat(plan.Raised(0)).IsEqual(2);
    }

    [TestCase]
    public void Raise_StopsAtTheLimit()
    {
        int[] values = [48, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        plan.Raise(0, values, Plenty, 10);

        AssertThat(plan.Raised(0)).IsEqual(2);
    }

    [TestCase]
    public void Lower_RefundsTheLastPlannedPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 2);

        plan.Lower(0, values, 1);

        AssertThat(plan.Raised(0)).IsEqual(1);
        AssertThat(plan.PointsUsed).IsEqual(150);
    }

    [TestCase]
    public void Lower_StopsAtNothingPlanned()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        plan.Lower(0, values, 10);

        AssertThat(plan.Raised(0)).IsEqual(0);
        AssertThat(plan.PointsUsed).IsEqual(0);
    }

    [TestCase]
    public void Commit_SendsOneStatCommandPerPlannedSlotInSlotOrder()
    {
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), [10, 0, 50, 2, 0, 20], Plenty, 1);
        plan.Raise(StatSlots.Mana, [60, 0, 500, 3, 0, 900], Plenty, 3);
        plan.Raise(2, [10, 0, 50, 3, 0, 12], Plenty, 2);

        AssertThat(plan.Commit())
            .ContainsExactly(
                ClientPackets.ShortAndInt(ClientCommand.Stat, 2, 2),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Mana, 3),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.FirstSkill + Dagger, 1));
    }

    [TestCase]
    public void Commit_SplitsSkillRaisesIntoAmountsTheServerAccepts()
    {
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), [1, 0, 300, 0, 0, 1], Plenty, 200);

        AssertThat(plan.Commit())
            .ContainsExactly(
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Skill(Dagger), 135),
                ClientPackets.ShortAndInt(ClientCommand.Stat, StatSlots.Skill(Dagger), 65));
    }

    [TestCase]
    public void Commit_ClearsThePlan()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        plan.Commit();

        AssertThat(plan.Raised(0)).IsEqual(0);
        AssertThat(plan.PointsUsed).IsEqual(0);
        AssertThat(plan.Commit()).IsEmpty();
    }

    [TestCase(false, false, 1)]
    [TestCase(true, false, 10)]
    [TestCase(false, true, 90)]
    [TestCase(true, true, 90)]
    public void ClickTimes_RaisesTenWithShiftAndNinetyWithCtrlOrAlt(bool shift, bool ctrlOrAlt, int times)
    {
        AssertThat(StatPlan.ClickTimes(shift, ctrlOrAlt)).IsEqual(times);
    }

    [TestCase]
    public void Help_NamesTheStatBeingRaisedOrLowered()
    {
        AssertThat(StatPlan.RaiseHelp("Dagger")).IsEqual("Raise Dagger.");
        AssertThat(StatPlan.LowerHelp("Mana")).IsEqual("Lower Mana.");
    }

    [TestCase]
    public void CostAfter_IsTheCostOfAPointBeyondThePlannedOnes()
    {
        int[] values = [10, 0, 13, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 1);

        AssertThat(plan.CostAfter(0, values, 0)).IsEqual(199);
        AssertThat(plan.CostAfter(0, values, 1)).IsEqual(12 * 12 * 12 * 3 / 20);
        AssertThat(plan.CostAfter(0, values, 2)).IsNull();
    }

    [TestCase]
    public void RaiseAll_PlansEveryPoint()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        AssertThat(plan.RaiseAll([(0, 2), (1, 1)], _ => values, Plenty)).IsTrue();

        AssertThat((plan.Raised(0), plan.Raised(1))).IsEqual((2, 1));
        AssertThat(plan.PointsUsed).IsEqual(150 + 199 + 150);
    }

    [TestCase]
    public void RaiseAll_PlansNothingWhenAPointCantBeBought()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        int[] capped = [10, 0, 11, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(2, values, Plenty, 1);

        AssertThat(plan.RaiseAll([(0, 1), (1, 2)], slot => slot == 1 ? capped : values, Plenty)).IsFalse();

        AssertThat((plan.Raised(0), plan.Raised(1), plan.Raised(2))).IsEqual((0, 0, 1));
        AssertThat(plan.PointsUsed).IsEqual(150);
    }

    [TestCase]
    public void RaiseAll_PlansNothingWithTooFewPoints()
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();

        AssertThat(plan.RaiseAll([(0, 1), (1, 1)], _ => values, 200)).IsFalse();

        AssertThat(plan.PointsUsed).IsEqual(0);
    }

    [TestCase]
    public void Reset_ForgetsThePlan()
    {
        var plan = new StatPlan();
        int[] values = [10, 0, 100, 1, 0, 0];
        plan.Raise(0, values, 100000, 3);

        plan.Reset();

        AssertThat(plan.Raised(0)).IsEqual(0);
        AssertThat(plan.PointsUsed).IsEqual(0);
        AssertThat(plan.Commit()).IsEmpty();
    }
}
