using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Planner;

// Something the focus may put points into, by its stat raise slot: 0-4 an attribute, 5 Hitpoints, 7 Mana, 8 and up a
// skill.
public sealed class FocusCandidate : IEquatable<FocusCandidate>
{
    public int RaiseIndex { get; set; }

    public int MostPoints { get; set; }

    // The cost of the 1st..nth extra point; NoCost when the stat is capped.
    public int[] StepCosts { get; } = new int[FocusSearch.MostPoints];

    public bool Equals(FocusCandidate? other) =>
        other != null
        && (RaiseIndex, MostPoints) == (other.RaiseIndex, other.MostPoints)
        && StepCosts.SequenceEqual(other.StepCosts);

    public override bool Equals(object? other) => Equals(other as FocusCandidate);

    public override int GetHashCode() => HashCode.Combine(RaiseIndex, MostPoints, StepCosts[0]);
}

public sealed record FocusRaise(int RaiseIndex, int Points, int Cost);

// The raises are in raise slot order.
public sealed record FocusRoute(int TotalCost, IReadOnlyList<FocusRaise> Raises);

// The cheapest mix of extra points over the candidates that the accept test takes: every mix is tried cheapest first,
// a point at a time, within each candidate's bound. A mix is a base-17 number with a digit per candidate; 17^15 still
// fits in 64 bits.
public static class FocusSearch
{
    public const int MostCandidates = 15;
    public const int MostPoints = 16;

    // Bounds the search when nothing is accepted, so it always ends.
    public const int MostStates = 65536;

    private const int PointRadix = MostPoints + 1;

    // A tie goes to fewer points, then to the lower mix key.
    public static FocusRoute? Cheapest(
        IReadOnlyList<FocusCandidate> candidates,
        uint allowedCandidates,
        Func<int[], bool> accept)
    {
        var digitValues = new ulong[candidates.Count];
        for (var index = 0; index < candidates.Count; index++)
        {
            digitValues[index] = index == 0 ? 1 : digitValues[index - 1] * PointRadix;
        }

        var queue = new PriorityQueue<ulong, (int Cost, int PointTotal, ulong Key)>();
        var seen = new HashSet<ulong> { 0 };
        queue.Enqueue(0, (0, 0, 0));
        while (queue.TryDequeue(out var key, out var entry))
        {
            var points = Decode(candidates.Count, key);
            if (key != 0 && accept(points))
            {
                return Route(candidates, points, entry.Cost);
            }

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var step = points[index];
                if ((allowedCandidates & (1u << index)) == 0
                    || step >= Math.Clamp(candidate.MostPoints, 0, MostPoints)
                    || candidate.StepCosts[step] == SkillFocus.NoCost
                    || seen.Count >= MostStates)
                {
                    continue;
                }

                var next = key + digitValues[index];
                if (seen.Add(next))
                {
                    queue.Enqueue(next, (entry.Cost + candidate.StepCosts[step], entry.PointTotal + 1, next));
                }
            }
        }

        return null;
    }

    private static int[] Decode(int candidateCount, ulong key)
    {
        var points = new int[candidateCount];
        for (var index = 0; index < candidateCount; index++)
        {
            points[index] = (int)(key % PointRadix);
            key /= PointRadix;
        }

        return points;
    }

    private static FocusRoute Route(IReadOnlyList<FocusCandidate> candidates, int[] points, int totalCost)
    {
        var raises = new List<FocusRaise>();
        for (var index = 0; index < candidates.Count; index++)
        {
            if (points[index] > 0)
            {
                var candidate = candidates[index];
                raises.Add(
                    new FocusRaise(candidate.RaiseIndex, points[index], candidate.StepCosts.Take(points[index]).Sum()));
            }
        }

        return new FocusRoute(totalCost, raises.OrderBy(raise => raise.RaiseIndex).ToArray());
    }
}
