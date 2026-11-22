using System;
using TheLastGate.State;

namespace TheLastGate.World;

public static class MapSpriteRules
{
    private const int Centre = WorldMap.Size / 2;
    private const int Waypoint = 598;
    private const int LitWaypoint = 599;
    private const int FirstMineWall = 16350;
    private const int LastMineWall = 16421;
    private const int MineWallsPerLowWall = 8;
    private const int FacingRight = 1;
    private const int FacingLeft = 2;
    private const int FacingUp = 3;
    private const int FacingDown = 4;
    private const int ShimmeringFloor = 16980;
    private const int FlickerTicks = 10;
    private const int FlickerCycle = 666;

    private static readonly int[] LowMineWalls = [457, 456, 455, 466, 459, 458, 468, 467, 467];

    private static readonly int[] WaypointFloors =
    [
        1002, 1008, 1013, 1034, 1010, 1052, 1100, 1099, 1012, 1109, 1118, 1141, 1158, 1145, 1014, 1005,
        1006, 1007, 402, 500, 520, 531, 542, 551, 558, 543, 544, 545, 546, 547, 548, 549,
    ];

    public static int FloorSprite(in MapTile tile, int clientTick)
    {
        if (((MapFlags)tile.Flags).HasFlag(MapFlags.Invisible))
        {
            return 0;
        }

        return tile.BackgroundSprite == ShimmeringFloor
            ? ShimmeringFloor + FlickerStep(tile.X, tile.Y, clientTick) / 2
            : tile.BackgroundSprite;
    }

    public static int ObjectSprite(
        in MapTile tile,
        int itemSprite,
        int column,
        int row,
        PlayerState player,
        bool hideWalls)
    {
        if (itemSprite == 0 || HidesObjectsAndCharacters(tile, column, row))
        {
            return 0;
        }

        var hidesHere = hideWalls && !IsInFrontOfPlayer(column, row);
        if (hidesHere && !((MapFlags)tile.Flags).HasFlag(MapFlags.IsItem))
        {
            return itemSprite + 1;
        }

        if (hidesHere && !IsFacedByPlayer(column, row, player.Direction)
            && LowMineWall(itemSprite) is { } lowWall)
        {
            return lowWall;
        }

        return itemSprite == Waypoint && IsFoundWaypoint(tile.BackgroundSprite, player.Waypoints)
            ? LitWaypoint
            : itemSprite;
    }

    public static int CharacterSprite(in MapTile tile, int characterSprite, int column, int row) =>
        HidesObjectsAndCharacters(tile, column, row) ? 0 : characterSprite;

    public static SpriteTint EnvironmentTint(in MapTile tile, bool plainInfravision)
    {
        var flags = (MapFlags)tile.Flags;
        return (flags.HasFlag(MapFlags.Infrared) && !plainInfravision ? SpriteTint.Infrared : SpriteTint.None)
            | (flags.HasFlag(MapFlags.Underwater) ? SpriteTint.Underwater : SpriteTint.None);
    }

    public static SpriteTint CharacterTint(in MapTile tile, bool plainInfravision)
    {
        var flags = (MapFlags)tile.Flags;
        return EnvironmentTint(tile, plainInfravision)
            | (flags.HasFlag(MapFlags.Stoned) ? SpriteTint.Grey : SpriteTint.None)
            | (flags.HasFlag(MapFlags.Bloody) ? SpriteTint.Bloody : SpriteTint.None);
    }

    public static bool HidesObjectsAndCharacters(in MapTile tile, int column, int row) =>
        ((MapFlags)tile.Flags).HasFlag(MapFlags.Invisible) || IsInHiddenCorner(column, row);

    // The current client draws no objects or characters in this corner of the map, which lies along the bottom edge of
    // the screen.
    private static bool IsInHiddenCorner(int column, int row) =>
        (row <= 5 && column >= WorldMap.Size - 9)
        || (row <= 6 && column >= WorldMap.Size - 8)
        || (row <= 7 && column >= WorldMap.Size - 7)
        || (row <= 8 && column >= WorldMap.Size - 6)
        || (row <= 9 && column >= WorldMap.Size - 5);

    private static bool IsInFrontOfPlayer(int column, int row) => column < Centre && row > Centre;

    private static bool IsFacedByPlayer(int column, int row, int direction) => direction switch
    {
        FacingRight => column == Centre + 1 && row == Centre,
        FacingLeft => column == Centre - 1 && row == Centre,
        FacingUp => column == Centre && row == Centre - 1,
        FacingDown => column == Centre && row == Centre + 1,
        _ => false,
    };

    // The last wall in each group of eight has no low version.
    private static int? LowMineWall(int wall)
    {
        var offset = wall - FirstMineWall;
        return wall is >= FirstMineWall and <= LastMineWall && offset % MineWallsPerLowWall != MineWallsPerLowWall - 1
            ? LowMineWalls[offset / MineWallsPerLowWall]
            : null;
    }

    // Each shimmering floor tile takes its turn to flicker for ten ticks out of every 6660, picked by a hash of its
    // position.
    private static int FlickerStep(int x, int y, int clientTick)
    {
        var turn = (x * 43 + y * 77 + x * y + x * 24 + y * 39) % FlickerCycle;
        return clientTick / FlickerTicks % FlickerCycle == turn ? clientTick % FlickerTicks : 0;
    }

    private static bool IsFoundWaypoint(int floor, int foundWaypoints)
    {
        var waypoint = Array.IndexOf(WaypointFloors, floor);
        return waypoint >= 0 && (foundWaypoints & (1 << waypoint)) != 0;
    }
}
