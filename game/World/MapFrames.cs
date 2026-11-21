using System;
using TheLastGate.State;

namespace TheLastGate.World;

public sealed class MapFrames
{
    private readonly int[] itemSprites = new int[WorldMap.TileCount];
    private readonly Action<string> report;

    public MapFrames() => report = description => UnusualDataSeen?.Invoke(description);

    public int TickCount { get; private set; }

    public event Action<string>? UnusualDataSeen;

    public int ItemSprite(int tileIndex) => itemSprites[tileIndex];

    public void EndOfTick(WorldMap map)
    {
        TickCount++;
        for (var tileIndex = 0; tileIndex < WorldMap.TileCount; tileIndex++)
        {
            itemSprites[tileIndex] = ItemFrames.Sprite(map.Tiles[tileIndex], TickCount, report);
        }
    }
}
