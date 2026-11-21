using System;
using System.Collections.Generic;
using TheLastGate.State;

namespace TheLastGate.World;

public sealed class MapFrames
{
    private readonly (int X, int Y)[] characterOffsets = new (int X, int Y)[WorldMap.TileCount];
    private readonly int[] characterSprites = new int[WorldMap.TileCount];
    private readonly Dictionary<int, int> idleFramesByCharacter = [];
    private readonly int[] itemSprites = new int[WorldMap.TileCount];
    private readonly Action<string> report;

    public MapFrames() => report = description => UnusualDataSeen?.Invoke(description);

    public int TickCount { get; private set; }

    public event Action<string>? UnusualDataSeen;

    public int ItemSprite(int tileIndex) => itemSprites[tileIndex];

    public int CharacterSprite(int tileIndex) => characterSprites[tileIndex];

    public (int X, int Y) CharacterOffset(int tileIndex) => characterOffsets[tileIndex];

    // Runs before Core steps the character statuses at the end of the tick, so each frame shows the status it arrived in.
    public void EndOfTick(WorldMap map, int serverTick)
    {
        TickCount++;
        for (var tileIndex = 0; tileIndex < WorldMap.TileCount; tileIndex++)
        {
            ref readonly var tile = ref map.Tiles[tileIndex];
            itemSprites[tileIndex] = ItemFrames.Sprite(tile, TickCount, report);
            var frame = CharacterFrames.For(tile, serverTick, IdleFrame(tile, serverTick), report);
            characterSprites[tileIndex] = frame.Sprite;
            if (frame.Offset is { } offset)
            {
                characterOffsets[tileIndex] = offset;
            }
        }
    }

    private int IdleFrame(in MapTile tile, int serverTick)
    {
        idleFramesByCharacter.TryGetValue(tile.CharacterNumber, out var idleFrame);
        if (CharacterFrames.CyclesIdleFrame(tile, serverTick))
        {
            idleFrame = CharacterFrames.NextIdleFrame(idleFrame);
            idleFramesByCharacter[tile.CharacterNumber] = idleFrame;
        }

        return idleFrame;
    }
}
