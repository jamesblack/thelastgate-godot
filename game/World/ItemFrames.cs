using System;
using TheLastGate.State;

namespace TheLastGate.World;

public static class ItemFrames
{
    private const int LastStill = 1;
    private const int TickCounted = 21;
    private const int TickCountedFrameMask = 63;
    private const int TicksPerLoopFrame = 2;

    private static readonly ItemLoop[] Loops =
    [
        new(2, 5, 2),
        new(6, 7, 1),
        new(8, 15, 1),
        new(16, 20, 1),
    ];

    // The server sends only the status a loop starts from and never steps it, so the frame comes from the tick count.
    public static int Sprite(in MapTile tile, int tickCount, Action<string> reportUnusualData)
    {
        var status = tile.ItemStatus;
        if (tile.ItemSprite == 0 || status <= LastStill)
        {
            return tile.ItemSprite;
        }

        if (status == TickCounted)
        {
            return tile.ItemSprite + (tickCount & TickCountedFrameMask);
        }

        foreach (var loop in Loops)
        {
            if (status >= loop.FirstStatus && status <= loop.LastStatus)
            {
                var length = loop.LastStatus - loop.FirstStatus + 1;
                var frame = (status - loop.FirstStatus + tickCount / TicksPerLoopFrame) % length;
                return tile.ItemSprite + frame * loop.SpritesPerFrame;
            }
        }

        reportUnusualData($"An item has status {status}, which the current client doesn't animate.");
        return tile.ItemSprite;
    }

    private readonly record struct ItemLoop(int FirstStatus, int LastStatus, int SpritesPerFrame);
}
