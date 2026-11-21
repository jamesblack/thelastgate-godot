using System;
using TheLastGate.State;

namespace TheLastGate.World;

public sealed class MapFrames
{
    public int TickCount => throw new NotImplementedException();

    public event Action<string>? UnusualDataSeen;

    public int ItemSprite(int tileIndex) => throw new NotImplementedException();

    public void EndOfTick(WorldMap map) => throw new NotImplementedException();
}
