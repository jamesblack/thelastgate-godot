using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Tests;

public class StatLinesTests
{
    private const int Plenty = 1_000_000;
    private const int Dagger = 3;

    [Theory]
    [InlineData(0, "Braveness")]
    [InlineData(1, "Willpower")]
    [InlineData(2, "Intuition")]
    [InlineData(3, "Agility")]
    [InlineData(4, "Strength")]
    public void Attribute_NamesTheFiveAttributes(int attribute, string name)
    {
        Assert.Equal(name, StatLines.Attribute(attribute, [10, 0, 50, 3, 0, 12], new StatPlan(), Plenty).Name);
    }

    [Fact]
    public void Attribute_ShowsTheBaseTheScoreAndTheCubicCost()
    {
        var line = StatLines.Attribute(0, [10, 0, 50, 3, 1, 44], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Braveness", TextColour.Yellow, 10, 300, true, 150), line);
    }

    [Theory]
    [InlineData(150, true)]
    [InlineData(149, false)]
    public void Attribute_OffersARaiseOnlyWhenThePointsCoverIt(int points, bool canRaise)
    {
        Assert.Equal(canRaise, StatLines.Attribute(0, [10, 0, 50, 3, 0, 12], new StatPlan(), points).CanRaise);
    }

    [Fact]
    public void Attribute_HasNoCostAtItsLimit()
    {
        var line = StatLines.Attribute(0, [50, 0, 50, 3, 0, 55], new StatPlan(), Plenty);

        Assert.Null(line.Cost);
        Assert.False(line.CanRaise);
    }

    [Fact]
    public void HitPoints_ShowsTheBaseTheMaximumAndALinearCost()
    {
        var line = StatLines.HitPoints([100, 0, 500, 4, 0, 1250], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Hitpoints", TextColour.Yellow, 100, 1250, true, 400), line);
    }

    [Fact]
    public void Mana_ShowsTheBaseTheMaximumAndALinearCost()
    {
        var line = StatLines.Mana([60, 0, 500, 3, 0, 900], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Mana", TextColour.Yellow, 60, 900, true, 180), line);
    }

    [Fact]
    public void Mana_HasNoCostAtItsLimit()
    {
        Assert.Null(StatLines.Mana([500, 0, 500, 3, 0, 900], new StatPlan(), Plenty).Cost);
    }

    [Fact]
    public void Skill_ShowsADashForAHiddenSkill()
    {
        var line = StatLines.Skill(Dagger, "Dagger", 0, [10, 0, 50, 2, 0, 20], new StatPlan(), Plenty);

        Assert.Equal(StatLine.Unknown, line);
    }

    [Fact]
    public void Skill_ShowsAKnownSkillsBaseScoreAndCost()
    {
        var line = StatLines.Skill(Dagger, "Dagger", 1, [10, 0, 50, 2, 0, 20], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Dagger", TextColour.Yellow, 10, 20, true, 50), line);
    }

    [Fact]
    public void Skill_CostsAtLeastItsValue()
    {
        Assert.Equal(3, StatLines.Skill(Dagger, "Dagger", 1, [3, 0, 50, 1, 0, 3], new StatPlan(), Plenty).Cost);
    }

    [Fact]
    public void Skill_HidesTheBaseCostAndRaiseOfAnUnlearntSkill()
    {
        var line = StatLines.Skill(Dagger, "Stealth", 1, [0, 0, 50, 2, 0, 6], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Stealth", TextColour.Yellow, null, 6, false, null), line);
    }

    [Fact]
    public void Skill_ShowsOnlyTheScoreInRedForAShowFour()
    {
        var line = StatLines.Skill(Dagger, "Immunity", 4, [10, 0, 50, 2, 1, 2], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Immunity", TextColour.Red, null, 258, false, null), line);
    }

    [Fact]
    public void Skill_ShowsThreeHundredInOrangeForASkillNotLearntYet()
    {
        var line = StatLines.Skill(Dagger, "Surround Hit", 2, [0, 0, 50, 2, 0, 0], new StatPlan(), Plenty);

        Assert.Equal(new StatLine("Surround Hit", TextColour.Orange, null, 300, false, null), line);
    }

    [Fact]
    public void Attribute_AddsPlannedRaisesToTheBaseAndScoreAndCostsTheNextPoint()
    {
        int[] values = [10, 0, 50, 3, 1, 44];
        var plan = new StatPlan();
        plan.Raise(0, values, Plenty, 2);

        var line = StatLines.Attribute(0, values, plan, Plenty);

        Assert.Equal(new StatLine("Braveness", TextColour.Yellow, 12, 302, true, 259, true), line);
    }

    [Theory]
    [InlineData(349, true)]
    [InlineData(348, false)]
    public void Attribute_OffersARaiseOnlyWhenThePointsLeftCoverIt(int points, bool canRaise)
    {
        int[] values = [10, 0, 50, 3, 0, 12];
        var plan = new StatPlan();
        plan.Raise(0, values, points, 1);

        Assert.Equal(canRaise, StatLines.Attribute(0, values, plan, points).CanRaise);
    }

    [Fact]
    public void HitPoints_AddsPlannedRaisesToTheBaseAndMaximum()
    {
        int[] values = [100, 0, 500, 4, 0, 1250];
        var plan = new StatPlan();
        plan.Raise(StatSlots.HitPoints, values, Plenty, 1);

        var line = StatLines.HitPoints(values, plan, Plenty);

        Assert.Equal(new StatLine("Hitpoints", TextColour.Yellow, 101, 1251, true, 404, true), line);
    }

    [Fact]
    public void Mana_AddsPlannedRaisesToTheBaseAndMaximum()
    {
        int[] values = [60, 0, 500, 3, 0, 900];
        var plan = new StatPlan();
        plan.Raise(StatSlots.Mana, values, Plenty, 1);

        var line = StatLines.Mana(values, plan, Plenty);

        Assert.Equal(new StatLine("Mana", TextColour.Yellow, 61, 901, true, 183, true), line);
    }

    [Fact]
    public void Skill_AddsPlannedRaisesOfThatSkillNumber()
    {
        int[] values = [10, 0, 50, 2, 0, 20];
        var plan = new StatPlan();
        plan.Raise(StatSlots.Skill(Dagger), values, Plenty, 1);

        var line = StatLines.Skill(Dagger, "Dagger", 1, values, plan, Plenty);

        Assert.Equal(new StatLine("Dagger", TextColour.Yellow, 11, 21, true, 66, true), line);
    }
}
