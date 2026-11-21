using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Planner;

public sealed class SkillFocusInput
{
    // Null marks a slot the skill doesn't use.
    public AttributeSlot?[] Attributes { get; } = new AttributeSlot?[3];

    // The values the preview sums, planned raises included.
    public int[] AttributeValues { get; } = new int[3];

    // The cost of each slot's 1st..6th extra point; a repeated attribute uses its first slot's costs.
    public int[][] AttributeStepCosts { get; } =
        [new int[SkillFocus.MostSteps], new int[SkillFocus.MostSteps], new int[SkillFocus.MostSteps]];

    public int Divisor { get; set; }

    // NoCost when the skill itself is capped.
    public int DirectCost { get; set; }

    // Under the Reverse Fool any attribute point counts, so the route buys the cheapest points across all five.
    public bool FoolReversed { get; set; }

    public int[][] PoolStepCosts { get; } =
    [
        new int[AttributePool.MostPoints], new int[AttributePool.MostPoints], new int[AttributePool.MostPoints],
        new int[AttributePool.MostPoints], new int[AttributePool.MostPoints],
    ];
}

public sealed record SkillFocusRaise(AttributeSlot Attribute, int Points, int Cost);

// The raises are in attribute order; the direct route has none.
public sealed record SkillFocusRoute(int TotalCost, IReadOnlyList<SkillFocusRaise> Raises);

public sealed record SkillFocusSuggestion(SkillFocusRoute? Direct, SkillFocusRoute? Attributes)
{
    // A tie goes to the direct route.
    public bool UseDirect => Direct != null && (Attributes == null || Direct.TotalCost <= Attributes.TotalCost);

    public bool AnyAvailable => Direct != null || Attributes != null;
}

// The cheapest way to give one skill +1: a point in the skill itself, or the cheapest attribute points that push the
// skill's attribute total over the next divisor boundary.
public static class SkillFocus
{
    // No attribute ever needs more points than the largest divisor to reach the next boundary.
    public const int MostSteps = SkillPreview.HardModeDivisor;
    public const int NoCost = AttributePool.NoCost;

    private const int FoolMostBreakpoints = 4;

    public static SkillFocusSuggestion Suggest(SkillFocusInput input) =>
        new(
            input.DirectCost == NoCost ? null : new SkillFocusRoute(input.DirectCost, []),
            input.FoolReversed ? FoolReversedRoute(input) : CheapestAttributeRoute(input));

    // The cost of the first points, or NoCost if any of them can't be bought.
    private static int PrefixCost(int[] stepCosts, int pointCount)
    {
        var total = 0;
        for (var step = 0; step < pointCount; step++)
        {
            if (stepCosts[step] == NoCost)
            {
                return NoCost;
            }

            total += stepCosts[step];
        }

        return total;
    }

    // Each attribute the skill uses once, in attribute order, with how many of its slots name it.
    private static (AttributeSlot Attribute, int Appearances, int[] StepCosts)[] DistinctAttributes(
        SkillFocusInput input)
    {
        var distinct = new List<(AttributeSlot Attribute, int Appearances, int[] StepCosts)>();
        for (var slot = 0; slot < input.Attributes.Length; slot++)
        {
            if (input.Attributes[slot] is not { } attribute)
            {
                continue;
            }

            var known = distinct.FindIndex(entry => entry.Attribute == attribute);
            if (known >= 0)
            {
                distinct[known] = distinct[known] with { Appearances = distinct[known].Appearances + 1 };
            }
            else
            {
                distinct.Add((attribute, 1, input.AttributeStepCosts[slot]));
            }
        }

        return distinct.OrderBy(entry => entry.Attribute).ToArray();
    }

    // Every combination of 0 to divisor points for each distinct attribute; a tie in cost goes to fewer points.
    private static SkillFocusRoute? CheapestAttributeRoute(SkillFocusInput input)
    {
        var distinct = DistinctAttributes(input);
        if (distinct.Length == 0 || input.Divisor <= 0)
        {
            return null;
        }

        var attributeSum = 0;
        for (var slot = 0; slot < input.Attributes.Length; slot++)
        {
            if (input.Attributes[slot] != null)
            {
                attributeSum += input.AttributeValues[slot];
            }
        }

        var needed = input.Divisor - attributeSum % input.Divisor;
        var mostPoints = Math.Min(input.Divisor, MostSteps);
        int[]? bestCounts = null;
        var bestCost = 0;
        var bestPoints = 0;
        foreach (var counts in Combinations(distinct.Length, mostPoints))
        {
            var gain = 0;
            var cost = 0;
            var pointSum = 0;
            var buyable = true;
            for (var index = 0; index < distinct.Length && buyable; index++)
            {
                var stepCost = PrefixCost(distinct[index].StepCosts, counts[index]);
                buyable = stepCost != NoCost;
                cost += stepCost;
                gain += counts[index] * distinct[index].Appearances;
                pointSum += counts[index];
            }

            if (buyable && gain >= needed
                && (bestCounts == null || cost < bestCost || (cost == bestCost && pointSum < bestPoints)))
            {
                bestCounts = counts;
                bestCost = cost;
                bestPoints = pointSum;
            }
        }

        if (bestCounts == null)
        {
            return null;
        }

        var raises = distinct.Select((entry, index) => (entry, points: bestCounts[index]))
            .Where(pick => pick.points > 0)
            .Select(pick => new SkillFocusRaise(
                pick.entry.Attribute,
                pick.points,
                PrefixCost(pick.entry.StepCosts, pick.points)))
            .ToArray();
        return new SkillFocusRoute(bestCost, raises);
    }

    // In C's order: the first attribute's count changes slowest.
    private static IEnumerable<int[]> Combinations(int attributeCount, int mostPoints)
    {
        var counts = new int[attributeCount];
        while (true)
        {
            yield return (int[])counts.Clone();
            var position = attributeCount - 1;
            while (position >= 0 && counts[position] == mostPoints)
            {
                counts[position] = 0;
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            counts[position]++;
        }
    }

    private static int SlotSumAfterBreakpoints(SkillFocusInput input, int breakpoints)
    {
        var sum = 0;
        for (var slot = 0; slot < input.Attributes.Length; slot++)
        {
            if (input.Attributes[slot] != null)
            {
                sum += TarotMath.FoolReversedTotal(
                    input.AttributeValues[slot],
                    breakpoints * TarotMath.FoolPointsPerBreakpoint);
            }
        }

        return sum;
    }

    // Under the Reverse Fool every attribute point moves all five attributes, so the route is just enough
    // breakpoints, bought where the points are cheapest.
    private static SkillFocusRoute? FoolReversedRoute(SkillFocusInput input)
    {
        if (input.Divisor <= 0)
        {
            return null;
        }

        var currentShare = SlotSumAfterBreakpoints(input, 0) / input.Divisor;
        var breakpoints = Enumerable.Range(1, FoolMostBreakpoints)
            .FirstOrDefault(count => SlotSumAfterBreakpoints(input, count) / input.Divisor > currentShare);
        if (breakpoints == 0
            || AttributePool.Cheapest(input.PoolStepCosts, breakpoints * TarotMath.FoolPointsPerBreakpoint) is not
                { } pick)
        {
            return null;
        }

        var raises = Enum.GetValues<AttributeSlot>()
            .Where(attribute => pick.Points[(int)attribute] > 0)
            .Select(attribute => new SkillFocusRaise(
                attribute,
                pick.Points[(int)attribute],
                PrefixCost(input.PoolStepCosts[(int)attribute], pick.Points[(int)attribute])))
            .ToArray();
        return new SkillFocusRoute(pick.TotalCost, raises);
    }
}
