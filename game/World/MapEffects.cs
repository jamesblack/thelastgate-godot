using TheLastGate.State;

namespace TheLastGate.World;

public static class MapEffects
{
    private const int FirstInjury = 1079;
    private const int FirstCriticalInjury = 1206;
    private const int FirstDeath = 280;
    private const int FirstTomb = 240;
    private const int DeathShift = 17;
    private const int TombShift = 12;
    private const MapFlags InjuryBits = MapFlags.Injured | MapFlags.Injured1 | MapFlags.Injured2;

    public static int InjurySprite(in MapTile tile)
    {
        var flags = (MapFlags)tile.Flags;
        var first = flags.HasFlag(MapFlags.CriticallyHit) ? FirstCriticalInjury : FirstInjury;
        return (flags & InjuryBits) switch
        {
            MapFlags.Injured => first,
            MapFlags.Injured | MapFlags.Injured1 => first + 1,
            MapFlags.Injured | MapFlags.Injured2 => first + 2,
            InjuryBits => first + 3,
            _ => 0,
        };
    }

    public static int DeathSprite(in MapTile tile) =>
        Step(tile.Flags, MapFlags.Death, DeathShift) is var step and not 0 ? FirstDeath + step - 1 : 0;

    public static bool DeathFollowsCharacter(int characterSprite) => characterSprite != 0;

    public static int TombSprite(in MapTile tile) =>
        Step(tile.Flags, MapFlags.Tomb, TombShift) is var step and not 0 ? FirstTomb + step - 1 : 0;

    private static int Step(uint flags, MapFlags bits, int shift) => (int)((flags & (uint)bits) >> shift);
}
