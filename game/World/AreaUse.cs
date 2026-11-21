using System;
using System.Collections.Generic;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.World;

public readonly record struct AreaTarget(int TileIndex, int X, int Y, ClientCommand Command);

public static class AreaUse
{
    public const int MostTiles = 25;

    public static (int X, int Y) FitEnd((int X, int Y) start, (int X, int Y) end)
    {
        var (width, height) = (Math.Abs(end.X - start.X) + 1, Math.Abs(end.Y - start.Y) + 1);
        while (width * height > MostTiles)
        {
            if (width >= height)
            {
                width--;
            }
            else
            {
                height--;
            }
        }

        return (start.X + Math.Sign(end.X - start.X) * (width - 1),
            start.Y + Math.Sign(end.Y - start.Y) * (height - 1));
    }

    public static IReadOnlyList<AreaTarget> Pick(WorldMap map, IEnumerable<int> area)
    {
        var found = new List<AreaTarget>();
        foreach (var tileIndex in area)
        {
            var (column, row) = (tileIndex % WorldMap.Size, tileIndex / WorldMap.Size);
            ref readonly var tile = ref map.Tiles[tileIndex];
            var flags = (MapFlags)tile.Flags;
            if (!flags.HasFlag(MapFlags.IsItem) || flags.HasFlag(MapFlags.Invisible)
                || MapSpriteRules.HidesObjectsAndCharacters(tile, column, row))
            {
                continue;
            }

            var command = flags.HasFlag(MapFlags.IsUsable) ? ClientCommand.Use : ClientCommand.PickUp;
            found.Add(new AreaTarget(tileIndex, tile.X, tile.Y, command));
        }

        return NearestFirst(found, map.Tiles[WorldMap.PlayerTile].X, map.Tiles[WorldMap.PlayerTile].Y);
    }

    private static List<AreaTarget> NearestFirst(List<AreaTarget> left, int fromX, int fromY)
    {
        var ordered = new List<AreaTarget>(left.Count);
        while (left.Count > 0)
        {
            var nearestIndex = 0;
            var nearestDistance = int.MaxValue;
            for (var candidate = 0; candidate < left.Count; candidate++)
            {
                var (acrossDistance, downDistance) = (left[candidate].X - fromX, left[candidate].Y - fromY);
                var distance = acrossDistance * acrossDistance + downDistance * downDistance;
                if (distance < nearestDistance)
                {
                    (nearestIndex, nearestDistance) = (candidate, distance);
                }
            }

            var next = left[nearestIndex];
            left.RemoveAt(nearestIndex);
            ordered.Add(next);
            (fromX, fromY) = (next.X, next.Y);
        }

        return ordered;
    }
}
