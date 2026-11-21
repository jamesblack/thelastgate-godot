using GdUnit4;
using TheLastGate.Planner;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class FocusSearchTests
{
    private const uint Everyone = uint.MaxValue;

    [TestCase]
    public void Cheapest_FindsTheCheapestAcceptedMix()
    {
        FocusCandidate[] candidates = [Candidate(0, 10, 10, 10), Candidate(1, 15, 15)];

        var route = FocusSearch.Cheapest(candidates, Everyone, points => points.Sum() >= 2);

        AssertThat(route?.TotalCost).IsEqual(20);
        AssertThat(route!.Raises).ContainsExactly(new FocusRaise(0, 2, 20));
    }

    [TestCase]
    public void ATie_GoesToFewerPoints()
    {
        FocusCandidate[] candidates = [Candidate(0, 10, 10), Candidate(1, 20)];

        var route = FocusSearch.Cheapest(candidates, Everyone, points => points[0] >= 2 || points[1] >= 1);

        AssertThat(route!.Raises).ContainsExactly(new FocusRaise(1, 1, 20));
    }

    [TestCase]
    public void ATieOnPoints_GoesToTheEarlierCandidate()
    {
        FocusCandidate[] candidates = [Candidate(0, 10), Candidate(1, 10)];

        var route = FocusSearch.Cheapest(candidates, Everyone, points => points.Sum() >= 1);

        AssertThat(route!.Raises).ContainsExactly(new FocusRaise(0, 1, 10));
    }

    [TestCase]
    public void ACandidateNotAllowed_GetsNoPoints()
    {
        FocusCandidate[] candidates = [Candidate(0, 10), Candidate(1, 50)];

        var route = FocusSearch.Cheapest(candidates, 1u << 1, points => points.Sum() >= 1);

        AssertThat(route!.Raises).ContainsExactly(new FocusRaise(1, 1, 50));
    }

    [TestCase]
    public void ACappedPoint_CantBeBought()
    {
        FocusCandidate[] candidates = [Candidate(0, 10, SkillFocus.NoCost, 10)];

        AssertThat(FocusSearch.Cheapest(candidates, Everyone, points => points[0] >= 2)).IsNull();
    }

    [TestCase]
    public void ACandidate_TakesNoMoreThanItsMostPoints()
    {
        var candidate = Candidate(0, 10, 10, 10);
        candidate.MostPoints = 2;

        AssertThat(FocusSearch.Cheapest([candidate], Everyone, points => points[0] >= 3)).IsNull();
    }

    [TestCase]
    public void Raises_AreInRaiseSlotOrder()
    {
        FocusCandidate[] candidates = [Candidate(8, 10), Candidate(2, 10)];

        var route = FocusSearch.Cheapest(candidates, Everyone, points => points[0] >= 1 && points[1] >= 1);

        AssertThat(route!.Raises).ContainsExactly(new FocusRaise(2, 1, 10), new FocusRaise(8, 1, 10));
    }

    [TestCase]
    public void NoPoints_IsNeverARoute()
    {
        AssertThat(FocusSearch.Cheapest([Candidate(0)], Everyone, _ => true)).IsNull();
    }

    [TestCase]
    public void ASearchThatFindsNothing_StopsAtTheStateLimit()
    {
        var candidates = Enumerable.Range(0, FocusSearch.MostCandidates)
            .Select(index => Candidate(index, Enumerable.Repeat(1, FocusSearch.MostPoints).ToArray()))
            .ToArray();

        AssertThat(FocusSearch.Cheapest(candidates, Everyone, _ => false)).IsNull();
    }

    private static FocusCandidate Candidate(int raiseIndex, params int[] stepCosts)
    {
        var candidate = new FocusCandidate { RaiseIndex = raiseIndex, MostPoints = stepCosts.Length };
        stepCosts.CopyTo(candidate.StepCosts, 0);
        return candidate;
    }
}
