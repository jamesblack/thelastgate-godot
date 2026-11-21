using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class StatLinesTests
{
    private const int Plenty = 1_000_000;
    private const int Dagger = 3;

    [TestCase(0, "Braveness")]
    [TestCase(1, "Willpower")]
    [TestCase(2, "Intuition")]
    [TestCase(3, "Agility")]
    [TestCase(4, "Strength")]
    public void Attribute_NamesTheFiveAttributes(int attribute, string name)
    {
        AssertThat(StatLines.Attribute(attribute, [10, 0, 50, 3, 0, 12], new StatPlan(), Plenty).Name).IsEqual(name);
    }

    [TestCase]
    public void Attribute_ShowsTheBaseTheScoreAndTheCubicCost()
    {
        var line = StatLines.Attribute(0, [10, 0, 50, 3, 1, 44], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Braveness", TextColour.Yellow, 10, 300, true, 150));
    }

    [TestCase(150, true)]
    [TestCase(149, false)]
    public void Attribute_OffersARaiseOnlyWhenThePointsCoverIt(int points, bool canRaise)
    {
        AssertThat(StatLines.Attribute(0, [10, 0, 50, 3, 0, 12], new StatPlan(), points).CanRaise).IsEqual(canRaise);
    }

    [TestCase]
    public void Attribute_HasNoCostAtItsLimit()
    {
        var line = StatLines.Attribute(0, [50, 0, 50, 3, 0, 55], new StatPlan(), Plenty);

        AssertThat(line.Cost).IsNull();
        AssertThat(line.CanRaise).IsFalse();
    }

    [TestCase]
    public void HitPoints_ShowsTheBaseTheMaximumAndALinearCost()
    {
        var line = StatLines.HitPoints([100, 0, 500, 4, 0, 1250], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Hitpoints", TextColour.Yellow, 100, 1250, true, 400));
    }

    [TestCase]
    public void Mana_ShowsTheBaseTheMaximumAndALinearCost()
    {
        var line = StatLines.Mana([60, 0, 500, 3, 0, 900], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Mana", TextColour.Yellow, 60, 900, true, 180));
    }

    [TestCase]
    public void Mana_HasNoCostAtItsLimit()
    {
        AssertThat(StatLines.Mana([500, 0, 500, 3, 0, 900], new StatPlan(), Plenty).Cost).IsNull();
    }

    [TestCase]
    public void Skill_ShowsADashForAHiddenSkill()
    {
        var line = StatLines.Skill(Dagger, "Dagger", 0, [10, 0, 50, 2, 0, 20], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(StatLine.Unknown);
    }

    [TestCase]
    public void Skill_ShowsAKnownSkillsBaseScoreAndCost()
    {
        var line = StatLines.Skill(Dagger, "Dagger", 1, [10, 0, 50, 2, 0, 20], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Dagger", TextColour.Yellow, 10, 20, true, 50));
    }

    [TestCase]
    public void Skill_CostsAtLeastItsValue()
    {
        AssertThat(StatLines.Skill(Dagger, "Dagger", 1, [3, 0, 50, 1, 0, 3], new StatPlan(), Plenty).Cost).IsEqual(3);
    }

    [TestCase]
    public void Skill_HidesTheBaseCostAndRaiseOfAnUnlearntSkill()
    {
        var line = StatLines.Skill(Dagger, "Stealth", 1, [0, 0, 50, 2, 0, 6], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Stealth", TextColour.Yellow, null, 6, false, null));
    }

    [TestCase]
    public void Skill_ShowsOnlyTheScoreInRedForAShowFour()
    {
        var line = StatLines.Skill(Dagger, "Immunity", 4, [10, 0, 50, 2, 1, 2], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Immunity", TextColour.Red, null, 258, false, null));
    }

    [TestCase]
    public void Skill_ShowsThreeHundredInOrangeForASkillNotLearntYet()
    {
        var line = StatLines.Skill(Dagger, "Surround Hit", 2, [0, 0, 50, 2, 0, 0], new StatPlan(), Plenty);

        AssertThat(line).IsEqual(new StatLine("Surround Hit", TextColour.Orange, null, 300, false, null));
    }

    [TestCase]
    public void Attribute_AddsPlannedRaisesToTheBaseAndScoreAndCostsTheNextPoint()
    {
        int[] values = [10, 0, 50, 3, 1, 44];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 2);

        var line = StatLines.Attribute(0, values, plan, Plenty);

        AssertThat(line).IsEqual(new StatLine("Braveness", TextColour.Yellow, 12, 302, true, 259, true));
    }

    [TestCase(349, true)]
    [TestCase(348, false)]
    public void Attribute_OffersARaiseOnlyWhenThePointsLeftCoverIt(int points, bool canRaise)
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, points, 1);

        AssertThat(StatLines.Attribute(0, values, plan, points).CanRaise).IsEqual(canRaise);
    }

    [TestCase]
    public void HitPoints_AddsPlannedRaisesToTheBaseAndMaximum()
    {
        int[] values = [100, 0, 500, 4, 0, 1250];
        var plan = new StatPlan();
        plan.Raise(StatSlots.HitPoints, values, Plenty, 1);

        var line = StatLines.HitPoints(values, plan, Plenty);

        AssertThat(line).IsEqual(new StatLine("Hitpoints", TextColour.Yellow, 101, 1251, true, 404, true));
    }

    [TestCase]
    public void Mana_AddsPlannedRaisesToTheBaseAndMaximum()
    {
        int[] values = [60, 0, 500, 3, 0, 900];
        var plan = new StatPlan();
        plan.Raise(StatSlots.Mana, values, Plenty, 1);

        var line = StatLines.Mana(values, plan, Plenty);

        AssertThat(line).IsEqual(new StatLine("Mana", TextColour.Yellow, 61, 901, true, 183, true));
    }

    [TestCase]
    public void Skill_AddsPlannedRaisesOfThatSkillNumber()
    {
        int[] values = [10, 0, 50, 2, 0, 20];
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), values, Plenty, 1);

        var line = StatLines.Skill(Dagger, "Dagger", 1, values, plan, Plenty);

        AssertThat(line).IsEqual(new StatLine("Dagger", TextColour.Yellow, 11, 21, true, 66, true));
    }
}
