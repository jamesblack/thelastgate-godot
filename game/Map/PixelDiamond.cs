using System;
using Godot;

namespace TheLastGate.Map;

public static class PixelDiamond
{
    public static Image Solid(int radius) => Make(radius, 1, true);

    public static Image Dotted(int radius) => Make(radius, 2, false);

    private static Image Make(int radius, int rowStep, bool filled)
    {
        var side = radius * 2 + 1;
        var image = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
        for (var row = -radius; row <= radius; row += rowStep)
        {
            var halfWidth = radius - Math.Abs(row);
            for (var column = -halfWidth; column <= halfWidth; column++)
            {
                if (filled || Math.Abs(column) == halfWidth)
                {
                    image.SetPixel(radius + column, radius + row, Colors.White);
                }
            }
        }

        return image;
    }
}
