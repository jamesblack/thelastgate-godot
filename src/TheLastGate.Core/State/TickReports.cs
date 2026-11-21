using System;
using TheLastGate.Net;

namespace TheLastGate.State;

public sealed class TickReports
{
    public Action<byte[]> Send { get; set; } = _ => { };

    public uint TicksPlayed { get; private set; }

    internal void TickPlayed()
    {
        TicksPlayed++;
        if (TickReport.IsDue(TicksPlayed))
        {
            Send(TickReport.Create(TicksPlayed));
        }
    }
}
