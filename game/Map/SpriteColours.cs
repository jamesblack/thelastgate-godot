using System;
using TheLastGate.Assets;

namespace TheLastGate.Map;

public static class SpriteColours
{
    private const int BytesPerPixel = 4;

    // Magenta is the sprites' see-through colour, so it and fully transparent pixels don't count.
    public static uint Average(ReadOnlySpan<byte> rgbaPixels)
    {
        var sum = default(ColourSum);
        sum.Add(rgbaPixels);
        return sum.Average();
    }

    // The average of one region of a larger image, such as a sprite on its sheet, without copying it out.
    public static uint Average(ReadOnlySpan<byte> rgbaPixels, int imageWidth, PixelRect region)
    {
        var sum = default(ColourSum);
        for (var row = region.Y; row < region.Y + region.Height; row++)
        {
            sum.Add(rgbaPixels.Slice((row * imageWidth + region.X) * BytesPerPixel, region.Width * BytesPerPixel));
        }

        return sum.Average();
    }

    private struct ColourSum
    {
        private long red;
        private long green;
        private long blue;
        private int counted;

        public void Add(ReadOnlySpan<byte> rgbaPixels)
        {
            for (var offset = 0; offset + BytesPerPixel <= rgbaPixels.Length; offset += BytesPerPixel)
            {
                var pixel = rgbaPixels.Slice(offset, BytesPerPixel);
                if (pixel[3] == 0 || (pixel[0] == 255 && pixel[1] == 0 && pixel[2] == 255))
                {
                    continue;
                }

                red += pixel[0];
                green += pixel[1];
                blue += pixel[2];
                counted++;
            }
        }

        public readonly uint Average() =>
            counted == 0
                ? 0
                : MinimapColours.FromRgb((byte)(red / counted), (byte)(green / counted), (byte)(blue / counted));
    }
}
