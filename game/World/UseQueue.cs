using System;
using System.Collections.Generic;
using TheLastGate.Assets;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.World;

public enum UseQueueAdd
{
    Ignored,
    Full,
    Queued,
}

// Uses queued with Ctrl+Shift, or uses and pickups queued for an area, done one after another: the first is sent at
// once when the player is idle, and the next when the server says the action before it has ended. A queued action
// stays first until the server starts it.
public sealed class UseQueue(TextLog chat)
{
    public const int MostQueued = 5;

    private const string FullMessage = "Your queue is full.";

    private readonly List<(int X, int Y, ClientCommand Command)> queued = [];
    private bool enabled;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!value)
            {
                queued.Clear();
            }
        }
    }

    public Action<byte[]> Send { get; set; } = _ => { };

    public int Count => queued.Count;

    // 1 for the next use, 0 when the tile isn't queued.
    public int PositionOf(int x, int y) => queued.FindIndex(entry => (entry.X, entry.Y) == (x, y)) + 1;

    public UseQueueAdd Add(int x, int y, PlayerTarget target)
    {
        if (queued.Count >= MostQueued && !IsInProgress(x, y, target) && PositionOf(x, y) == 0)
        {
            chat.Add(new LogLine(FullMessage, (int)TextColour.Red));
            return UseQueueAdd.Full;
        }

        return Enqueue(x, y, ClientCommand.Use, target);
    }

    public int QueueArea(IEnumerable<AreaTarget> targets, PlayerTarget target)
    {
        queued.Clear();
        var count = 0;
        foreach (var areaTarget in targets)
        {
            if (Enqueue(areaTarget.X, areaTarget.Y, areaTarget.Command, target) == UseQueueAdd.Queued)
            {
                count++;
            }
        }

        return count;
    }

    private UseQueueAdd Enqueue(int x, int y, ClientCommand command, PlayerTarget target)
    {
        if (IsInProgress(x, y, target) || PositionOf(x, y) > 0)
        {
            return UseQueueAdd.Ignored;
        }

        queued.Add((x, y, command));
        if (target.MiscAction == MiscActions.Idle && queued.Count == 1)
        {
            SendNext();
        }

        return UseQueueAdd.Queued;
    }

    public void TargetChanged(PlayerTarget previous, PlayerTarget now)
    {
        if (IsBusy(previous))
        {
            if (!IsBusy(now))
            {
                SendNext();
            }
        }
        else if (IsBusy(now) && queued.Count > 0
            && (queued[0].X, queued[0].Y) == (now.MiscTarget1, now.MiscTarget2))
        {
            queued.RemoveAt(0);
        }
    }

    // The commands the current client sends through its cmd(): those aimed at a tile, a shop or the smith, the
    // fighting mode, Escape's reset and stat raises.
    public void Sent(byte[] packet)
    {
        if ((ClientCommand)packet[0] is ClientCommand.Move or ClientCommand.Turn or ClientCommand.PickUp
            or ClientCommand.Drop or ClientCommand.Use or ClientCommand.LookItem or ClientCommand.Shop
            or ClientCommand.Smith or ClientCommand.Mode or ClientCommand.Reset or ClientCommand.Stat)
        {
            queued.Clear();
        }
    }

    private void SendNext()
    {
        if (queued.Count > 0)
        {
            Send(MapCommandPackets.ForTile(queued[0].Command, queued[0].X, queued[0].Y));
        }
    }

    private static bool IsInProgress(int x, int y, PlayerTarget target) =>
        IsBusy(target) && (target.MiscTarget1, target.MiscTarget2) == (x, y);

    private static bool IsBusy(PlayerTarget target) => target.MiscAction is MiscActions.Use or MiscActions.PickUp;
}
