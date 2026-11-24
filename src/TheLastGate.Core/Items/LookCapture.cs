using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Profiling;
using TheLastGate.State;

namespace TheLastGate.Items;

// Shared is the look reply both requests got; Released is everything else they caught, which belongs in the chat log.
public sealed record LookReply(IReadOnlyList<LogLine> Shared, IReadOnlyList<LogLine> Released);

// A background look is sent twice. Both replies are kept out of the chat log, and the lines they have in common are
// the item's text: anything else caught with them was other chat from the same tick.
public sealed class LookCapture
{
    private const int MaxQueued = 16;

    private readonly List<LogLine> fetch = [];
    private readonly Queue<(Role Role, int Tick)> queue = new();
    private readonly Action<LookReply> resolved;
    private readonly List<LogLine> sentinel = [];
    private Role? capturing;
    private bool fetchDone;
    private bool fetchWanted;
    private bool sentinelDone;

    public LookCapture(Action<LookReply> resolved) => this.resolved = resolved;

    public bool HasPending => queue.Count > 0;

    public void ExpectFetch(int tick)
    {
        fetchWanted = true;
        fetchDone = false;
        sentinelDone = false;
        fetch.Clear();
        sentinel.Clear();
        Queue(Role.Fetch, tick);
        Queue(Role.Sentinel, tick);
    }

    public void ExpectManual(int tick) => Queue(Role.Manual, tick);

    public bool Offer(LogLine line)
    {
        using var timing = Timings.Measure("tick.lookcapture");
        if (ItemDetailsParser.IsReplyStart(line.Text, line.Font))
        {
            CloseCapture();
            if (queue.Count == 0)
            {
                return false;
            }

            capturing = queue.Dequeue().Role;
        }

        return capturing switch
        {
            Role.Fetch => Append(fetch, line),
            Role.Sentinel => Append(sentinel, line),
            _ => false,
        };
    }

    // A reply is written within one tick, so it never continues into the next.
    public void EndOfTick() => CloseCapture();

    public IReadOnlyList<LogLine> Abandon()
    {
        var released = fetch.Concat(sentinel).ToList();
        fetch.Clear();
        sentinel.Clear();
        fetchWanted = false;
        capturing = null;
        queue.Clear();
        return released;
    }

    public void DropManualsSentBefore(int tick)
    {
        while (queue.Count > 0 && queue.Peek() is { Role: Role.Manual } manual && manual.Tick < tick)
        {
            queue.Dequeue();
        }
    }

    private static bool Append(List<LogLine> reply, LogLine line)
    {
        reply.Add(line);
        return true;
    }

    private void Queue(Role role, int tick)
    {
        if (queue.Count < MaxQueued)
        {
            queue.Enqueue((role, tick));
        }
    }

    private void CloseCapture()
    {
        fetchDone |= capturing == Role.Fetch;
        sentinelDone |= capturing == Role.Sentinel;
        capturing = null;
        if (fetchWanted && fetchDone && sentinelDone)
        {
            Resolve();
        }
    }

    private void Resolve()
    {
        var shared = 0;
        while (shared < fetch.Count && shared < sentinel.Count && fetch[shared] == sentinel[shared])
        {
            shared++;
        }

        var reply = new LookReply(fetch[..shared], fetch[shared..].Concat(sentinel[shared..]).ToList());
        fetch.Clear();
        sentinel.Clear();
        fetchWanted = false;
        resolved(reply);
    }

    private enum Role
    {
        Fetch,
        Sentinel,
        Manual,
    }
}
