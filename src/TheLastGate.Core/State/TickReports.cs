using System;

namespace TheLastGate.State;

public sealed class TickReports
{
    public Action<byte[]> Send { get; set; } = _ => { };

    public uint TicksPlayed => throw new NotImplementedException();
}
