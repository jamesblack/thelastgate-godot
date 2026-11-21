using System.Collections.Generic;

namespace TheLastGate.Planner;

public sealed record AttributePoolPick(int TotalCost, IReadOnlyList<int> Points);

// Buys a number of attribute points wherever they are cheapest. Each attribute's points only get dearer, so taking the
// cheapest next point every time gives the cheapest total. All or nothing: with too few points to buy, nothing is.
public static class AttributePool
{
    public const int MostPoints = 20;
    public const int NoCost = -1;

    public static AttributePoolPick? Cheapest(int[][] stepCosts, int pointCount)
    {
        if (pointCount < 0 || pointCount > MostPoints * stepCosts.Length)
        {
            return null;
        }

        var points = new int[stepCosts.Length];
        var totalCost = 0;
        for (var bought = 0; bought < pointCount; bought++)
        {
            var cheapest = -1;
            var cheapestCost = 0;
            for (var attribute = 0; attribute < stepCosts.Length; attribute++)
            {
                if (points[attribute] >= MostPoints)
                {
                    continue;
                }

                var cost = stepCosts[attribute][points[attribute]];
                if (cost != NoCost && (cheapest < 0 || cost < cheapestCost))
                {
                    cheapest = attribute;
                    cheapestCost = cost;
                }
            }

            if (cheapest < 0)
            {
                return null;
            }

            points[cheapest]++;
            totalCost += cheapestCost;
        }

        return new AttributePoolPick(totalCost, points);
    }
}
