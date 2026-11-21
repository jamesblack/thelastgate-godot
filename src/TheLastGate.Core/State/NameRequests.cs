using System;
using TheLastGate.Net;

namespace TheLastGate.State;

public sealed class NameRequests
{
    public const int TicksBetweenRequests = 24;

    private const int Centre = WorldMap.Size / 2;

    private int ticksSinceRequest;

    public Action<byte[]> Send { get; set; } = _ => { };

    public void EndOfTick(WorldMap map, CharacterNames names)
    {
        ticksSinceRequest++;
        if (ticksSinceRequest <= TicksBetweenRequests || NearestUnnamed(map, names) is not { } characterNumber)
        {
            return;
        }

        Send(MapCommandPackets.ForCharacter(ClientCommand.AutoLook, characterNumber));
        ticksSinceRequest = 0;
    }

    private static int? NearestUnnamed(WorldMap map, CharacterNames names)
    {
        int? nearest = null;
        var nearestDistance = int.MaxValue;
        for (var tileIndex = 0; tileIndex < map.Tiles.Length; tileIndex++)
        {
            ref readonly var tile = ref map.Tiles[tileIndex];
            if (tile.CharacterNumber == 0
                || ((MapFlags)tile.Flags).HasFlag(MapFlags.Invisible)
                || names.NameOf(tile.CharacterNumber, tile.CharacterId).Length > 0)
            {
                continue;
            }

            var (across, down) = (tileIndex % WorldMap.Size - Centre, tileIndex / WorldMap.Size - Centre);
            var distance = across * across + down * down;
            if (distance < nearestDistance)
            {
                (nearest, nearestDistance) = (tile.CharacterNumber, distance);
            }
        }

        return nearest;
    }
}
