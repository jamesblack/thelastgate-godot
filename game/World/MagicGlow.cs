using System;
using TheLastGate.State;

namespace TheLastGate.World;

public readonly record struct MagicGlow(bool Red, bool Green, bool Blue, int Strength)
{
    public const int Left = -32;
    public const int Top = -64;
    public const int Size = 64;

    private const int RedShift = 22;
    private const int GreenShift = 25;
    private const int BlueShift = 28;

    public static MagicGlow? For(in MapTile tile)
    {
        var red = Count(tile.Flags, MapFlags.RedMagic, RedShift);
        var green = Count(tile.Flags, MapFlags.GreenMagic, GreenShift);
        var blue = Count(tile.Flags, MapFlags.BlueMagic, BlueShift);
        var strength = Math.Max(red, Math.Max(green, blue));
        return strength == 0 ? null : new MagicGlow(red != 0, green != 0, blue != 0, strength);
    }

    private static int Count(uint flags, MapFlags bits, int shift) => (int)((flags & (uint)bits) >> shift);
}
