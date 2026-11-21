using System;
using System.Collections.Generic;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Stats;

public sealed class StatPlan
{
    public const string UpdateHelp = "Make the changes permanent";

    private const int BaseValue = 0;
    private const int Limit = 2;
    private const int Difficulty = 3;
    private const int ShiftClickTimes = 10;
    private const int CtrlClickTimes = 90;

    private readonly int[] raised = new int[StatSlots.Count];

    public int PointsUsed { get; private set; }

    public static int ClickTimes(bool shift, bool ctrlOrAlt) =>
        ctrlOrAlt ? CtrlClickTimes : shift ? ShiftClickTimes : 1;

    public static string RaiseHelp(string name) => $"Raise {name}.";

    public static string LowerHelp(string name) => $"Lower {name}.";

    public int Raised(int slot) => raised[slot];

    public int? NextCost(int slot, int[] values) => CostAt(slot, values, raised[slot]);

    public int? CostAfter(int slot, int[] values, int extra) => CostAt(slot, values, raised[slot] + extra);

    // All or nothing: when any point can't be bought, the plan is left as it was.
    public bool RaiseAll(IReadOnlyList<(int Slot, int Points)> raises, Func<int, int[]> valuesFor, int points)
    {
        var raisedBefore = (int[])raised.Clone();
        var usedBefore = PointsUsed;
        foreach (var (slot, count) in raises)
        {
            var values = valuesFor(slot);
            for (var point = 0; point < count; point++)
            {
                if (!TryRaise(slot, values, points))
                {
                    raisedBefore.CopyTo(raised, 0);
                    PointsUsed = usedBefore;
                    return false;
                }
            }
        }

        return true;
    }

    public void Raise(int slot, int[] values, int points, int times)
    {
        for (var time = 0; time < times; time++)
        {
            if (!TryRaise(slot, values, points))
            {
                return;
            }
        }
    }

    public void Lower(int slot, int[] values, int times)
    {
        for (var time = 0; time < times && raised[slot] > 0; time++)
        {
            raised[slot]--;
            PointsUsed -= CostAt(slot, values, raised[slot]) ?? 0;
        }
    }

    public void Reset()
    {
        Array.Clear(raised);
        PointsUsed = 0;
    }

    public IReadOnlyList<byte[]> Commit()
    {
        var packets = new List<byte[]>();
        for (var slot = 0; slot < StatSlots.Count; slot++)
        {
            packets.AddRange(StatPackets.Raise(slot, raised[slot]));
        }

        Reset();
        return packets;
    }

    private bool TryRaise(int slot, int[] values, int points)
    {
        var cost = NextCost(slot, values);
        if (cost == null || cost > points - PointsUsed)
        {
            return false;
        }

        PointsUsed += cost.Value;
        raised[slot]++;
        return true;
    }

    private static int? CostAt(int slot, int[] values, int planned)
    {
        var value = values[BaseValue] + planned;
        if (values[BaseValue] <= 0 || value >= values[Limit])
        {
            return null;
        }

        var difficulty = values[Difficulty];
        return slot switch
        {
            < PlayerState.AttributeCount => value * value * value * difficulty / 20,
            StatSlots.HitPoints or StatSlots.Mana => value * difficulty,
            _ => Math.Max(value, value * value * value * difficulty / 40),
        };
    }
}
