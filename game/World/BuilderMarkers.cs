using System.Collections.Generic;
using TheLastGate.State;

namespace TheLastGate.World;

public static class BuilderMarkers
{
    private static readonly (TileRules Rule, int Sprite)[] MarkersInDrawOrder =
    [
        (TileRules.MoveBlock, 55),
        (TileRules.SightBlock, 84),
        (TileRules.Indoors, 56),
        (TileRules.Underwater, 75),
        (TileRules.NoFight, 58),
        (TileRules.NoMonsters, 59),
        (TileRules.Bank, 60),
        (TileRules.Tavern, 61),
        (TileRules.NoMagic, 62),
        (TileRules.DeathTrap, 73),
        (TileRules.NoLag, 57),
        (TileRules.Arena, 76),
        (TileRules.NoPlayers, 77),
        (TileRules.NoExpire, 82),
        (TileRules.UnnamedTopBit, 72),
    ];

    public static IReadOnlyList<int> For(in MapTile tile)
    {
        var rules = (TileRules)tile.Flags2;
        if (rules == TileRules.None)
        {
            return [];
        }

        var sprites = new List<int>();
        foreach (var (rule, sprite) in MarkersInDrawOrder)
        {
            if (rules.HasFlag(rule))
            {
                sprites.Add(sprite);
            }
        }

        return sprites;
    }
}
