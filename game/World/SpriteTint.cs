using System;

namespace TheLastGate.World;

[Flags]
public enum SpriteTint
{
    None = 0,
    Grey = 1 << 0,
    Infrared = 1 << 1,
    Underwater = 1 << 2,
    Bloody = 1 << 3,
}
