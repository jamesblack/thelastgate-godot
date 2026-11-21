using GdUnit4;
using TheLastGate.Planner;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class SkillFocusTests
{
    [TestCase]
    public void Direct_WinsWhenCheaper()
    {
        var input = Input(0, 1, 2, 5);
        SetValues(input, 10, 10, 20);
        input.DirectCost = 300;

        var suggestion = SkillFocus.Suggest(input);

        AssertThat(suggestion.UseDirect).IsTrue();
        AssertThat(suggestion.Direct?.TotalCost).IsEqual(300);
        AssertThat(suggestion.Attributes?.TotalCost).IsEqual(500);
    }

    [TestCase]
    public void Attributes_SplitWhereCheapest()
    {
        var input = Input(0, 1, 2, 5);
        SetValues(input, 14, 14, 15);
        SetCosts(input, 0, 100, 150);
        SetCosts(input, 1, 120, 200);
        SetCosts(input, 2, 500, 500);

        var suggestion = SkillFocus.Suggest(input);

        AssertThat(suggestion.UseDirect).IsFalse();
        AssertThat(suggestion.Attributes!.TotalCost).IsEqual(220);
        AssertThat(suggestion.Attributes.Raises)
            .ContainsExactly(
                new SkillFocusRaise(AttributeSlot.Braveness, 1, 100),
                new SkillFocusRaise(AttributeSlot.Willpower, 1, 120));
    }

    [TestCase]
    public void OnABoundary_AWholeDivisorIsNeeded()
    {
        var input = Input(0, 1, 2, 5);
        SetValues(input, 15, 15, 15);
        FillCosts(input, 10);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(50);
        AssertThat(route.Raises.Sum(raise => raise.Points)).IsEqual(5);
    }

    [TestCase]
    public void ADoubledAttribute_LosesToCheaperSinglePoints()
    {
        var input = Input(3, 3, 4, 5);
        SetValues(input, 10, 10, 11);
        SetDoubledCosts(input, 300);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(260);
        AssertThat(route.Raises).ContainsExactly(new SkillFocusRaise(AttributeSlot.Strength, 4, 260));
    }

    [TestCase]
    public void ADoubledAttribute_CountsTwiceWhenCheap()
    {
        var input = Input(3, 3, 4, 5);
        SetValues(input, 10, 10, 11);
        SetDoubledCosts(input, 40);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(80);
        AssertThat(route.Raises).ContainsExactly(new SkillFocusRaise(AttributeSlot.Agility, 2, 80));
    }

    [TestCase]
    public void ACappedAttribute_IsSplitAround()
    {
        var input = Input(0, 1, 2, 5);
        SetValues(input, 14, 14, 15);
        SetCosts(input, 0, 10, SkillFocus.NoCost);
        SetCosts(input, 1, 50, 100);
        SetCosts(input, 2, 60, 100);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(60);
        AssertThat(route.Raises.Count).IsEqual(2);
    }

    [TestCase]
    public void EveryAttributeCapped_LeavesOnlyDirect()
    {
        var input = Input(0, 1, 2, 5);
        FillCosts(input, SkillFocus.NoCost);

        var suggestion = SkillFocus.Suggest(input);

        AssertThat(suggestion.Attributes).IsNull();
        AssertThat(suggestion.UseDirect).IsTrue();
        AssertThat(suggestion.AnyAvailable).IsTrue();
    }

    [TestCase]
    public void EverythingCapped_LeavesNothing()
    {
        var input = Input(0, 1, 2, 5);
        FillCosts(input, SkillFocus.NoCost);
        input.DirectCost = SkillFocus.NoCost;

        var suggestion = SkillFocus.Suggest(input);

        AssertThat(suggestion.Direct).IsNull();
        AssertThat(suggestion.Attributes).IsNull();
        AssertThat(suggestion.AnyAvailable).IsFalse();
    }

    [TestCase]
    public void ATie_GoesToDirect()
    {
        var input = Input(0, 1, 2, 5);
        SetValues(input, 10, 10, 20);
        input.DirectCost = 500;

        var suggestion = SkillFocus.Suggest(input);

        AssertThat(suggestion.Attributes?.TotalCost).IsEqual(500);
        AssertThat(suggestion.UseDirect).IsTrue();
    }

    [TestCase]
    public void HardMode_NeedsSixPoints()
    {
        var input = Input(0, 1, 2, 6);
        SetValues(input, 12, 12, 12);
        FillCosts(input, 10);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(60);
        AssertThat(route.Raises.Sum(raise => raise.Points)).IsEqual(6);
    }

    [TestCase]
    public void AnUnusedSlot_IsIgnored()
    {
        var input = Input(0, null, 2, 5);
        SetValues(input, 14, 99, 15);
        SetCosts(input, 0, 30, 100);
        SetCosts(input, 1, 1, 1);
        SetCosts(input, 2, 20, 100);

        var route = SkillFocus.Suggest(input).Attributes!;

        AssertThat(route.TotalCost).IsEqual(20);
        AssertThat(route.Raises).ContainsExactly(new SkillFocusRaise(AttributeSlot.Intuition, 1, 20));
    }

    [TestCase]
    public void FoolReversed_BuysTwoBreakpointsWhereCheapest()
    {
        var suggestion = SkillFocus.Suggest(FoolReversedInput(5));

        AssertThat(suggestion.Attributes?.TotalCost).IsEqual(100);
        AssertThat(suggestion.Attributes!.Raises).ContainsExactly(new SkillFocusRaise(AttributeSlot.Agility, 10, 100));
        AssertThat(suggestion.UseDirect).IsFalse();
    }

    [TestCase]
    public void FoolReversed_InHardMode()
    {
        var route = SkillFocus.Suggest(FoolReversedInput(6)).Attributes!;

        AssertThat(route.Raises.Sum(raise => raise.Points)).IsEqual(5);
        AssertThat(route.TotalCost).IsEqual(50);
    }

    [TestCase]
    public void FoolReversed_HasNoRouteWhenThePoolIsCapped()
    {
        var input = FoolReversedInput(5);
        for (var attribute = 0; attribute < AttributeAmounts.Count; attribute++)
        {
            for (var step = attribute == 0 ? 3 : 0; step < AttributePool.MostPoints; step++)
            {
                input.PoolStepCosts[attribute][step] = SkillFocus.NoCost;
            }
        }

        AssertThat(SkillFocus.Suggest(input).Attributes).IsNull();
    }

    private static SkillFocusInput Input(int? first, int? second, int? third, int divisor)
    {
        var input = new SkillFocusInput { Divisor = divisor, DirectCost = 1000 };
        int?[] attributes = [first, second, third];
        for (var slot = 0; slot < 3; slot++)
        {
            input.Attributes[slot] = (AttributeSlot?)attributes[slot];
        }

        FillCosts(input, 100);
        foreach (var costs in input.PoolStepCosts)
        {
            Array.Fill(costs, SkillFocus.NoCost);
        }

        return input;
    }

    private static SkillFocusInput FoolReversedInput(int divisor)
    {
        var input = Input(0, 1, 2, divisor);
        input.FoolReversed = true;
        SetValues(input, 105, 105, 105);
        for (var attribute = 0; attribute < AttributeAmounts.Count; attribute++)
        {
            Array.Fill(input.PoolStepCosts[attribute], attribute == (int)AttributeSlot.Agility ? 10 : 100);
        }

        return input;
    }

    private static void SetValues(SkillFocusInput input, int first, int second, int third)
    {
        input.AttributeValues[0] = first;
        input.AttributeValues[1] = second;
        input.AttributeValues[2] = third;
    }

    private static void SetCosts(SkillFocusInput input, int slot, int first, int second)
    {
        input.AttributeStepCosts[slot][0] = first;
        input.AttributeStepCosts[slot][1] = second;
    }

    private static void FillCosts(SkillFocusInput input, int cost)
    {
        foreach (var costs in input.AttributeStepCosts)
        {
            Array.Fill(costs, cost);
        }
    }

    private static void SetDoubledCosts(SkillFocusInput input, int doubledCost)
    {
        for (var step = 0; step < SkillFocus.MostSteps; step++)
        {
            input.AttributeStepCosts[0][step] = doubledCost;
            input.AttributeStepCosts[1][step] = doubledCost;
            input.AttributeStepCosts[2][step] = 50 + step * 10;
        }
    }
}
