using System;

namespace TheLastGate.Assets;

public static class SpritePixels
{
    public const int Rgba8BytesPerPixel = 4;

    // Only fully opaque magenta becomes transparent. Images without an alpha channel decode as
    // opaque, so for them this is an RGB-only match.
    public static void ApplyMagentaKey(Span<byte> rgba)
    {
        for (var offset = 0; offset + Rgba8BytesPerPixel <= rgba.Length; offset += Rgba8BytesPerPixel)
        {
            if (rgba[offset] == 255 && rgba[offset + 1] == 0 && rgba[offset + 2] == 255 && rgba[offset + 3] == 255)
            {
                rgba[offset + 3] = 0;
            }
        }
    }
}
