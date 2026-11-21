using GdUnit4;
using TheLastGate.Planner;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class AttributePoolTests
{
    [TestCase]
    public void Cheapest_TakesTheCheapestPointsFirst()
    {
        var costs = Costs(30);
        for (var step = 0; step < AttributePool.MostPoints; step++)
        {
            costs[3][step] = step < 3 ? 10 : 50;
        }

        var pick = AttributePool.Cheapest(costs, 5);

        AssertThat(pick).IsNotNull();
        AssertThat(pick!.TotalCost).IsEqual(90);
        AssertThat(pick.Points).ContainsExactly(2, 0, 0, 3, 0);
    }

    [TestCase]
    public void Cheapest_SkipsAttributesThatCantBeRaised()
    {
        var costs = Costs(10);
        Array.Fill(costs[0], AttributePool.NoCost);

        var pick = AttributePool.Cheapest(costs, 5);

        AssertThat(pick).IsNotNull();
        AssertThat(pick!.Points[0]).IsEqual(0);
        AssertThat(pick.TotalCost).IsEqual(50);
    }

    [TestCase]
    public void Cheapest_IsNothingWhenThereArentEnoughPoints()
    {
        var costs = Costs(AttributePool.NoCost);
        costs[1][0] = 10;
        costs[1][1] = 10;
        costs[2][0] = 10;

        AssertThat(AttributePool.Cheapest(costs, 5)).IsNull();
    }

    private static int[][] Costs(int cost) =>
        Enumerable.Range(0, PlayerState.AttributeCount)
            .Select(_ => Enumerable.Repeat(cost, AttributePool.MostPoints).ToArray())
            .ToArray();
}
