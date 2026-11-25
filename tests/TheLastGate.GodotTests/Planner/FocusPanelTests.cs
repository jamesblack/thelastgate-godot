using GdUnit4;
using TheLastGate.Planner;
using TheLastGate.Rules;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class FocusPanelTests
{
    private const int SwordSkill = 3;
    private const int BlessSkill = 21;
    private const int UnlearnedSkill = 7;
    private const int Agility = (int)AttributeSlot.Agility;

    private static readonly int SwordSlot = StatSlots.Skill(SwordSkill);
    private FocusCache cache = new(FocusJobTests.RunNow);

    private PlannerGame game = null!;

    [BeforeTest]
    public void StartAGame() => (cache, game) = (new FocusCache(FocusJobTests.RunNow), FocusInputsTests.SwordGame());

    [TestCase]
    public void WithoutAFocus_ThePanelSaysHowToFocus()
    {
        var panel = Panel(null);

        AssertThat(panel.Lines).ContainsExactly(new FocusLine(FocusPanel.Prompt, null, false, FocusTone.Normal));
        AssertThat(panel.Plan).IsEmpty();
        AssertThat(panel.LostFocus).IsFalse();
    }

    [TestCase]
    public void ACheapSkillPoint_IsTheCheapestPlainRoute()
    {
        game.Choices.SpellToggles = 0;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Lines)
            .ContainsExactly(
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Cheapest +1", "4800 exp", false, FocusTone.Normal),
                new FocusLine("+1 Sword", "4800", true, FocusTone.SkillRaise),
                new FocusLine("Through attributes", "49476 exp", false, FocusTone.OtherRoute));
        AssertThat(panel.Plan).ContainsExactly(new FocusRaise(SwordSlot, 1, 4800));
        AssertThat(panel.PlanCost).IsEqual(4800);
        AssertThat(panel.Affordable).IsTrue();
    }

    [TestCase]
    public void ADearSkillPoint_LosesToItsAttributes()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][3] = 40;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Lines)
            .ContainsExactly(
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Cheapest +1", "49476 exp", false, FocusTone.Normal),
                new FocusLine("+1 Braveness", "9600", true, FocusTone.AttributeRaise),
                new FocusLine("+2 Agility", "19938", true, FocusTone.AttributeRaise),
                new FocusLine("+2 Strength", "19938", true, FocusTone.AttributeRaise),
                new FocusLine("Direct (+1 Sword)", "64000 exp", false, FocusTone.OtherRoute));
        AssertThat(panel.Plan)
            .ContainsExactly(
                new FocusRaise(0, 1, 9600),
                new FocusRaise(Agility, 2, 19938),
                new FocusRaise(4, 2, 19938));
    }

    [TestCase]
    public void ARouteThatCantBeBought_SaysSo()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][2] = 40;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Lines[^1])
            .IsEqual(new FocusLine("Direct (+1 Sword) not available", null, false, FocusTone.OtherRoute));
    }

    [TestCase]
    public void ASkillWithNothingLeftToBuy_CantBeRaised()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][2] = 40;
        foreach (var values in game.Attributes)
        {
            values[2] = 40;
        }

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Lines)
            .ContainsExactly(
                new FocusLine("Sword  80 -> 81", null, false, FocusTone.Heading),
                new FocusLine("Sword can't be raised further", null, false, FocusTone.Normal));
        AssertThat(panel.Plan).IsEmpty();
    }

    [TestCase]
    public void TooFewPoints_LeaveTheRouteUnaffordable()
    {
        game.Choices.SpellToggles = 0;
        game.Points = 4000;

        AssertThat(Panel(PlannerFocus.OnSkill(SwordSkill)).Affordable).IsFalse();
    }

    [TestCase]
    public void AShownBless_SearchesTheBuffModelAndListsTheRoutesThatLost()
    {
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Lines[0].Text).StartsWith("Sword  ");
        AssertThat(panel.Lines[0].Tone).IsEqual(FocusTone.Heading);
        AssertThat(panel.Lines[1])
            .IsEqual(new FocusLine("Cheapest +1", $"{panel.PlanCost} exp", false, FocusTone.Normal));
        AssertThat(panel.PlanCost).IsEqual(panel.Plan.Sum(raise => raise.Cost));
        var others = panel.Lines.Where(line => line.Tone == FocusTone.OtherRoute)
            .Select(line => line.Text.Replace(" not available", ""))
            .ToArray();
        AssertThat(others.Length).IsEqual(2);
        string[] routes = ["Direct (+1 Sword)", "Through attributes", "Through Bless"];
        AssertThat(others.Except(routes).ToArray()).IsEmpty();
    }

    [TestCase]
    public void WhileItsSearchRuns_ABuffedSkillIsWorkingAndOffersNoPlan()
    {
        cache = new FocusCache(_ => { });

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Working).IsTrue();
        AssertThat(panel.Lines)
            .ContainsExactly(
                new FocusLine("Sword", null, false, FocusTone.Heading),
                new FocusLine(FocusPanel.WorkingText, null, false, FocusTone.Normal));
        AssertThat(panel.Plan).IsEmpty();
        AssertThat(panel.LostFocus).IsFalse();
    }

    [TestCase]
    public void WhileItsSearchRuns_ACombatStatIsWorking()
    {
        cache = new FocusCache(_ => { });

        var panel = Panel(PlannerFocus.OnStat(CombatStat.Hit));

        AssertThat(panel.Working).IsTrue();
        AssertThat(panel.Lines[0]).IsEqual(new FocusLine("Hit Score", null, false, FocusTone.Heading));
        AssertThat(panel.Plan).IsEmpty();
    }

    [TestCase]
    public void OnceItsSearchFinishes_ABuffedSkillShowsItsRoute()
    {
        var held = new List<Action>();
        cache = new FocusCache(held.Add);
        Panel(PlannerFocus.OnSkill(SwordSkill));

        held.ForEach(search => search());

        AssertThat(cache.TakeFinished()).IsTrue();
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));
        AssertThat(panel.Working).IsFalse();
        AssertThat(panel.Plan).IsNotEmpty();
    }

    [TestCase]
    public void FocusingBless_HasNoRouteThroughBless()
    {
        var panel = Panel(PlannerFocus.OnSkill(BlessSkill));

        AssertThat(panel.Lines[0].Text).StartsWith("Bless  ");
        AssertThat(panel.Lines.Any(line => line.Text.StartsWith("Through Bless"))).IsFalse();
        AssertThat(panel.Lines.Count(line => line.Tone == FocusTone.OtherRoute)).IsEqual(1);
    }

    [TestCase]
    public void RouteLines_ColourSkillsGreenAndAttributesBlue()
    {
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        foreach (var (line, raise) in panel.Lines.Where(line => line.Indented).Zip(panel.Plan))
        {
            var skill = raise.RaiseIndex >= StatSlots.FirstSkill;
            AssertThat(line.Tone).IsEqual(skill ? FocusTone.SkillRaise : FocusTone.AttributeRaise);
        }
    }

    [TestCase]
    public void UnderTheReverseFool_ThePoolIsSplitIntoAttributes()
    {
        game.Choices.TarotCards = 1ul << (int)TarotCard.FoolReversed;
        game.Skills[SwordSkill][2] = 40;
        game.Skills[BlessSkill][2] = 30;

        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(panel.Plan).IsNotEmpty();
        AssertThat(panel.Plan.All(raise => raise.RaiseIndex is >= 0 and < AttributeAmounts.Count)).IsTrue();
        AssertThat(panel.Plan.Sum(raise => raise.Cost)).IsEqual(panel.PlanCost);
    }

    [TestCase]
    public void ACombatStat_ShowsItsCheapestRoute()
    {
        game.Choices.SpellToggles = 0;

        var panel = Panel(PlannerFocus.OnStat(CombatStat.Hit));

        AssertThat(panel.Lines[0]).IsEqual(new FocusLine("Hit Score  100 -> 101", null, false, FocusTone.Heading));
        AssertThat(panel.Lines[1].Text).IsEqual("Cheapest +1");
        AssertThat(panel.Plan).IsNotEmpty();
        AssertThat(panel.Lines.Any(line => line.Tone == FocusTone.OtherRoute)).IsFalse();
    }

    [TestCase]
    public void ACombatStatWithoutHitAndParry_LosesItsFocus()
    {
        game.MetaStats.Remove("Parry Score");

        var panel = Panel(PlannerFocus.OnStat(CombatStat.Armor));

        AssertThat(panel.LostFocus).IsTrue();
        AssertThat(panel.Lines.Single().Text).IsEqual(FocusPanel.Prompt);
    }

    [TestCase(UnlearnedSkill)]
    [TestCase(SwordSkill)]
    public void ASkillNotLearnedOrNotListed_LosesItsFocus(int skill)
    {
        game.SkillTexts[SwordSkill] = game.SkillTexts[SwordSkill] with { Show = 0 };

        var panel = Panel(PlannerFocus.OnSkill(skill));

        AssertThat(panel.LostFocus).IsTrue();
        AssertThat(panel.Plan).IsEmpty();
    }

    [TestCase]
    public void Planning_ARouteRaisesEveryPartOfIt()
    {
        game.Choices.SpellToggles = 0;
        game.Skills[SwordSkill][3] = 40;
        var panel = Panel(PlannerFocus.OnSkill(SwordSkill));

        AssertThat(
                game.Plan.RaiseAll(
                    panel.Plan.Select(raise => (raise.RaiseIndex, raise.Points)).ToArray(),
                    game.ValuesFor,
                    game.Points))
            .IsTrue();

        AssertThat(PlannerSheet.For(game).Skills.Single(row => row.Slot == SwordSlot).Preview).IsEqual(81);
        AssertThat(game.Plan.PointsUsed).IsEqual(panel.PlanCost);
    }

    private FocusPanel Panel(PlannerFocus? focus) => FocusPanel.For(PlannerSheet.For(game), focus, cache);
}
