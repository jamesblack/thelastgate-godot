using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly Action<IReadOnlyList<LogLine>> identifyAnswered;
    private readonly Action<ManualLook, IReadOnlyList<LogLine>> manualAnswered;
    private readonly Queue<(Role Role, int Tick, ManualLook? Look)> queue = new();
    private readonly Action<LookReply> resolved;
    private readonly List<LogLine> sentinel = [];
    private readonly List<LogLine> shown = [];
    private Role? capturing;
    private ManualLook? capturingLook;
    private bool fetchDone;
    private bool fetchWanted;
    private int? identifySentTick;
    private bool sentinelDone;

    public LookCapture(
        Action<LookReply> resolved,
        Action<ManualLook, IReadOnlyList<LogLine>> manualAnswered,
        Action<IReadOnlyList<LogLine>> identifyAnswered)
    {
        this.resolved = resolved;
        this.manualAnswered = manualAnswered;
        this.identifyAnswered = identifyAnswered;
    }

    public bool HasPending => queue.Count > 0 || identifySentTick != null;

    public void ExpectFetch(int tick)
    {
        fetchWanted = true;
        fetchDone = false;
        sentinelDone = false;
        fetch.Clear();
        sentinel.Clear();
        Queue(Role.Fetch, tick, null);
        Queue(Role.Sentinel, tick, null);
    }

    public void ExpectManual(int tick, ManualLook look) => Queue(Role.Manual, tick, look);

    // The server runs a skill when the character is next free, and a failed one says nothing, so this reply can't
    // take a place in the queue: it is whichever reply turns up while no look is waiting.
    public void ExpectIdentify(int tick) => identifySentTick = tick;

    public bool Offer(LogLine line)
    {
        using var timing = Timings.Measure("tick.lookcapture");
        if (ItemDetailsParser.IsReplyStart(line.Text, line.Font))
        {
            CloseCapture();
            if (queue.Count > 0)
            {
                (capturing, _, capturingLook) = queue.Dequeue();
            }
            else if (identifySentTick != null)
            {
                capturing = Role.Identify;
                identifySentTick = null;
            }
            else
            {
                return false;
            }
        }

        switch (capturing)
        {
            case Role.Fetch:
                fetch.Add(line);
                return true;
            case Role.Sentinel:
                sentinel.Add(line);
                return true;
            case Role.Manual or Role.Identify:
                shown.Add(line);
                return false;
            case null:
                return false;
            default:
                throw new UnreachableException();
        }
    }

    // A reply is written within one tick, so it never continues into the next.
    public void EndOfTick() => CloseCapture();

    public IReadOnlyList<LogLine> Abandon()
    {
        var released = fetch.Concat(sentinel).ToList();
        fetch.Clear();
        sentinel.Clear();
        shown.Clear();
        fetchWanted = false;
        capturing = null;
        capturingLook = null;
        identifySentTick = null;
        queue.Clear();
        return released;
    }

    public void DropUnansweredSentBefore(int tick)
    {
        if (identifySentTick < tick)
        {
            identifySentTick = null;
        }

        while (queue.Count > 0 && queue.Peek() is { Role: Role.Manual } unanswered && unanswered.Tick < tick)
        {
            queue.Dequeue();
        }
    }

    private void Queue(Role role, int tick, ManualLook? look)
    {
        if (queue.Count < MaxQueued)
        {
            queue.Enqueue((role, tick, look));
        }
    }

    private void CloseCapture()
    {
        fetchDone |= capturing == Role.Fetch;
        sentinelDone |= capturing == Role.Sentinel;
        var lines = shown.ToList();
        shown.Clear();
        if (capturingLook is { } look)
        {
            manualAnswered(look, lines);
        }
        else if (capturing == Role.Identify)
        {
            identifyAnswered(lines);
        }

        capturing = null;
        capturingLook = null;
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
        Identify,
    }
}
