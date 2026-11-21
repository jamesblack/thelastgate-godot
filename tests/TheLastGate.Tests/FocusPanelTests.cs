using TheLastGate.Planner;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Tests;

public class FocusPanelTests
{
    private const int SwordSkill = 3;
    private const int BlessSkill = 21;
    private const int UnlearnedSkill = 7;
    private const int Agility = (int)AttributeSlot.Agility;

    private static readonly int SwordSlot = StatPlan.SkillSlot(SwordSkill);
    private readonly FocusCache cache = new();

    private readonly PlannerGame game = FocusInputsTests.SwordGame();

    [Fact]
    public void WithoutAFocus_ThePanelSaysHowToFocus()
    {
        var panel = Panel(null);

        Assert.Equal([new FocusLine(FocusPanel.Prompt, null, false, FocusTone.Normal)], panel.Lines);
        Assert.Empty(panel.Plan);
        Assert.False(panel.LostFocus);
    }

    [Fact]
    public void ACheapSkillPoint_IsTheCheapestPlainRoute()
    {
        game.Choices.SpellToggles = 0;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.Equal(
            [
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Cheapest +1", "4800 exp", false, FocusTone.Normal),
                new FocusLine("+1 Sword", "4800", true, FocusTone.SkillRaise),
                new FocusLine("Through attributes", "49476 exp", false, FocusTone.OtherRoute),
            ],
            panel.Lines);
        Assert.Equal([new FocusRaise(SwordSlot, 1, 4800)], panel.Plan);
        Assert.Equal(4800, panel.PlanCost);
        Assert.True(panel.Affordable);
    }

    [Fact]
    public void ADearSkillPoint_LosesToItsAttributes()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][3] = 40;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.Equal(
            [
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Cheapest +1", "49476 exp", false, FocusTone.Normal),
                new FocusLine("+1 Braveness", "9600", true, FocusTone.AttributeRaise),
                new FocusLine("+2 Agility", "19938", true, FocusTone.AttributeRaise),
                new FocusLine("+2 Strength", "19938", true, FocusTone.AttributeRaise),
                new FocusLine("Direct (+1 Sword)", "64000 exp", false, FocusTone.OtherRoute),
            ],
            panel.Lines);
        Assert.Equal(
            [new FocusRaise(0, 1, 9600), new FocusRaise(Agility, 2, 19938), new FocusRaise(4, 2, 19938)],
            panel.Plan);
    }

    [Fact]
    public void ARouteThatCantBeBought_SaysSo()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][2] = 40;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.Equal(
            new FocusLine("Direct (+1 Sword) not available", null, false, FocusTone.OtherRoute),
            panel.Lines[^1]);
    }

    [Fact]
    public void ASkillWithNothingLeftToBuy_CantBeRaised()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][2] = 40;
        foreach (var values in game.Attributes)
        {
            values[2] = 40;
        }

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.Equal(
            [
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Sword can't be raised further", null, false, FocusTone.Normal),
            ],
            panel.Lines);
        Assert.Empty(panel.Plan);
    }

    [Fact]
    public void TooFewPoints_LeaveTheRouteUnaffordable()
    {
        game.Choices.SpellToggles = 0;
        game.Points = 4000;

        Assert.False(Panel(PlannerFocus.OnSkill(SwordSkill)).Affordable);
    }

    [Fact]
    public void AShownBless_SearchesTheBuffModelAndListsTheRoutesThatLost()
    {
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.StartsWith("Sword  ", panel.Lines[0].Text);
        Assert.Equal(FocusTone.Heading, panel.Lines[0].Tone);
        Assert.Equal(new FocusLine("Cheapest +1", $"{panel.PlanCost} exp", false, FocusTone.Normal), panel.Lines[1]);
        Assert.Equal(panel.Plan.Sum(raise => raise.Cost), panel.PlanCost);
        var others = panel.Lines.Where(line => line.Tone == FocusTone.OtherRoute)
            .Select(line => line.Text.Replace(" not available", ""))
            .ToArray();
        Assert.Equal(2, others.Length);
        Assert.Subset(
            new HashSet<string> { "Direct (+1 Sword)", "Through attributes", "Through Bless" },
            others.ToHashSet());
    }

    [Fact]
    public void FocusingBless_HasNoRouteThroughBless()
    {
        var panel = Panel(PlannerFocus.OnSkill(BlessSkill));

        Assert.StartsWith("Bless  ", panel.Lines[0].Text);
        Assert.DoesNotContain(panel.Lines, line => line.Text.StartsWith("Through Bless"));
        Assert.Single(panel.Lines, line => line.Tone == FocusTone.OtherRoute);
    }

    [Fact]
    public void RouteLines_ColourSkillsGreenAndAttributesBlue()
    {
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        foreach (var (line, raise) in panel.Lines.Where(line => line.Indented).Zip(panel.Plan))
        {
            var skill = raise.RaiseIndex >= StatPlan.FirstSkillSlot;
            Assert.Equal(skill ? FocusTone.SkillRaise : FocusTone.AttributeRaise, line.Tone);
        }
    }

    [Fact]
    public void UnderTheReverseFool_ThePoolIsSplitIntoAttributes()
    {
        game.Choices.TarotCards = 1ul << (int)TarotCard.FoolReversed;
        game.Skills[SwordSkill][2] = 40;
        game.Skills[BlessSkill][2] = 30;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.NotEmpty(panel.Plan);
        Assert.All(panel.Plan, raise => Assert.InRange(raise.RaiseIndex, 0, AttributeAmounts.Count - 1));
        Assert.Equal(panel.PlanCost, panel.Plan.Sum(raise => raise.Cost));
    }

    [Fact]
    public void ACombatStat_ShowsItsCheapestRoute()
    {
        game.Choices.SpellToggles = 0;

        var panel = Panel(PlannerFocus.OnStat(CombatStat.Hit));

        Assert.Equal(new FocusLine("Hit Score  100 -> 101", null, false, FocusTone.Heading), panel.Lines[0]);
        Assert.Equal("Cheapest +1", panel.Lines[1].Text);
        Assert.NotEmpty(panel.Plan);
        Assert.DoesNotContain(panel.Lines, line => line.Tone == FocusTone.OtherRoute);
    }

    [Fact]
    public void ACombatStatWithoutHitAndParry_LosesItsFocus()
    {
        game.MetaStats.Remove("Parry Score");

        var panel = Panel(PlannerFocus.OnStat(CombatStat.Armor));

        Assert.True(panel.LostFocus);
        Assert.Equal(FocusPanel.Prompt, Assert.Single(panel.Lines).Text);
    }

    [Theory]
    [InlineData(UnlearnedSkill)]
    [InlineData(SwordSkill)]
    public void ASkillNotLearnedOrNotListed_LosesItsFocus(int skill)
    {
        game.SkillTexts[SwordSkill] = game.SkillTexts[SwordSkill] with { Show = 0 };

        var panel = Panel(PlannerFocus.OnSkill(skill));

        Assert.True(panel.LostFocus);
        Assert.Empty(panel.Plan);
    }

    [Fact]
    public void Planning_ARouteRaisesEveryPartOfIt()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][3] = 40;
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        Assert.True(
            game.Plan.RaiseAll(
                panel.Plan.Select(raise => (raise.RaiseIndex, raise.Points)).ToArray(),
                game.ValuesFor,
                game.Points));

        Assert.Equal(81, PlannerSheet.For(game).Skills.Single(row => row.Slot == SwordSlot).Preview);
        Assert.Equal(panel.PlanCost, game.Plan.PointsUsed);
    }

    private FocusPanel Panel(PlannerFocus? focus) => FocusPanel.For(PlannerSheet.For(game), focus, cache);
}
