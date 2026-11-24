using System;
using TheLastGate.Profiling;
using TheLastGate.State;

namespace TheLastGate.Buffs;

// Keeps the power of the player's active Bless, Enhance and Protect. The client only sees buff icons, so whenever one
// of those three appears, is refreshed or recast, "#buffs" is sent in the background and its reply is taken out
// of the chat and read. A reply nobody asked for, because the player typed the command, is left for the chat.
public sealed class BuffWatch
{
    public const string Command = "#buffs";
    public const string FloodReply = "Oops, you're a bit too fast for me!";

    private const int TimeoutTicks = 30;

    // Every command counts against the server's chat flood limit, so the hidden requests are spaced out: closer while
    // a buff waits for its first reading, further apart when they only refresh readings already on show.
    private const int MissingGapTicks = 40;
    private const int RefreshGapTicks = 100;

    // The server sends a cast's slot on its next character update, at most 16 ticks after the cast message.
    private const int CastSlotDelayTicks = 16;

    // Further tries after a lost or refused request.
    private const int RetryLimit = 2;

    private readonly int[] castTick = new int[Enum.GetValues<BuffKind>().Length];
    private readonly PlayerState player;

    private bool capturing;
    private bool fetchPending;
    private int lastSendTick;

    // The tick a buff not read yet appeared; only a request sent after it can make the readings whole again.
    private int newBuffTick;
    private BuffListParser parser = new();
    private bool readingsFresh;
    private bool requestInFlight;
    private int requestTick;
    private int retriesLeft;
    private BuffListParser stored = new();
    private int tickNow;

    public BuffWatch(PlayerState player)
    {
        this.player = player;
        Reset();
    }

    public Action<string> Say { get; set; } = _ => { };

    // True while a buff change hasn't been answered yet.
    public bool Waiting => !readingsFresh;

    // Forgets every reading and request, as at login.
    public void Reset()
    {
        parser = new BuffListParser();
        stored = new BuffListParser();
        fetchPending = false;
        requestInFlight = false;
        capturing = false;
        readingsFresh = true;
        tickNow = 0;
        requestTick = 0;
        lastSendTick = -RefreshGapTicks;
        retriesLeft = RetryLimit;
        newBuffTick = -1;
        Array.Fill(castTick, -CastSlotDelayTicks - 1);
    }

    // A refresh or recast keeps showing the last reading while a new one is fetched; a new buff has none yet.
    public void SpellPacket(int slot, BuffSlot old, BuffSlot updated)
    {
        if (!BuffList.SlotNeedsFetch(old.Sprite, old.Active, updated.Sprite, updated.Active)
            || BuffList.SpriteKind(updated.Sprite) is not { } kind
            || tickNow - castTick[(int)kind] <= CastSlotDelayTicks)
        {
            return;
        }

        FetchSoon();
        if (updated.Sprite != old.Sprite)
        {
            MarkNewBuff();
        }
    }

    // True when the line was part of the watch's own request and must not reach the chat.
    public bool Capture(LogLine line)
    {
        if (capturing)
        {
            if (!parser.Feed(line.Text))
            {
                FinishCapture();
            }

            return true;
        }

        if (BuffList.CastKind(line.Text) is { } cast)
        {
            castTick[(int)cast] = tickNow;
            if (!IsActive(cast))
            {
                MarkNewBuff();
            }

            FetchSoon();
            return false;
        }

        if (requestInFlight && BuffList.IsHeader(line.Text))
        {
            parser = new BuffListParser();
            capturing = true;
            return true;
        }

        // The server turned the request away for flooding; that refusal is the watch's, not the player's.
        if (requestInFlight && line.Text == FloodReply)
        {
            RequestFailed();
            return true;
        }

        return false;
    }

    // The reply comes in one go, so a capture never runs on into the next tick.
    public void EndOfTick()
    {
        using var timing = Timings.Measure("tick.buffwatch");
        tickNow++;
        if (capturing)
        {
            FinishCapture();
        }

        if (requestInFlight && !capturing && tickNow - requestTick > TimeoutTicks)
        {
            RequestFailed();
        }

        if (!fetchPending || requestInFlight
            || tickNow - lastSendTick < (readingsFresh ? RefreshGapTicks : MissingGapTicks))
        {
            return;
        }

        Say(Command);
        fetchPending = false;
        requestInFlight = true;
        requestTick = tickNow;
        lastSendTick = tickNow;
    }

    public bool IsActive(BuffKind kind) =>
        Array.Exists(player.Buffs, slot => slot.Sprite > 0 && BuffList.SpriteKind(slot.Sprite) == kind);

    // The buff's reading, only while it is active and has been read since it last changed.
    public BuffReading? Reading(BuffKind kind)
    {
        var reading = stored.Readings[(int)kind];
        return readingsFresh && IsActive(kind) && reading.Known ? reading : null;
    }

    private void MarkNewBuff()
    {
        readingsFresh = false;
        newBuffTick = tickNow;
    }

    private void FetchSoon()
    {
        fetchPending = true;
        retriesLeft = RetryLimit;
    }

    private void RequestFailed()
    {
        requestInFlight = false;
        if (retriesLeft > 0)
        {
            retriesLeft--;
            fetchPending = true;
        }
    }

    private void FinishCapture()
    {
        capturing = false;
        requestInFlight = false;
        stored = parser;
        if (requestTick > newBuffTick)
        {
            readingsFresh = true;
        }

        retriesLeft = RetryLimit;
    }
}
