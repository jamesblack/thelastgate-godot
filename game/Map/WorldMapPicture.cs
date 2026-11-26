using System;

namespace TheLastGate.Map;

// The explored map as one picture for the world map window: map y across, map x down, over the top half of the
// stored map, which is all of the world there is. Unexplored tiles are black.
public static class WorldMapPicture
{
    public const int Width = 2048;
    public const int Height = 1024;
    public const int BlockSize = ExploredMap.BlockSize;
    public const int BlocksAcross = Width / BlockSize;
    public const int BlocksDown = Height / BlockSize;

    private const int BytesPerPixel = 4;

    // A block covers map x from down * BlockSize and map y from across * BlockSize.
    public static void RenderBlock(ExploredMap map, int across, int down, Span<byte> rgba)
    {
        var (left, top) = (across * BlockSize, down * BlockSize);
        for (var row = 0; row < BlockSize; row++)
        {
            for (var column = 0; column < BlockSize; column++)
            {
                var colour = map.ColourAt(top + row, left + column);
                var pixel = rgba.Slice((row * BlockSize + column) * BytesPerPixel, BytesPerPixel);
                pixel[0] = (byte)(colour >> 16);
                pixel[1] = (byte)(colour >> 8);
                pixel[2] = (byte)colour;
                pixel[3] = 255;
            }
        }
    }
}
