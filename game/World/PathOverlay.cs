using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheLastGate.State;

namespace TheLastGate.World;

public readonly record struct PathTile(int Column, int Row, Color Colour);

public partial class PathOverlay : Node2D
{
    public const float Alpha = 0.35f;

    private const float HalfWidth = IsometricProjection.TileSize / 2f;
    private const float HalfHeight = IsometricProjection.TileSize / 4f;

    private static readonly Vector2 DiamondCentre = new(0, -7.5f);

    private readonly Vector2[] corners = new Vector2[4];
    private IReadOnlyList<PathTile> shown = [];

    public int ShownTiles => shown.Count;

    public static Color TileColour(uint rgb, float brightness) =>
        new(
            (byte)(rgb >> 16) / 255f * brightness,
            (byte)(rgb >> 8) / 255f * brightness,
            (byte)rgb / 255f * brightness,
            Alpha);

    public static IReadOnlyList<PathTile> TilesInView(
        WorldMap map,
        IReadOnlyDictionary<(int X, int Y), uint> colours,
        float worldGamma,
        Func<int, bool> floorShown)
    {
        if (colours.Count == 0)
        {
            return [];
        }

        var tiles = new List<PathTile>();

        for (var tileIndex = 0; tileIndex < WorldMap.TileCount; tileIndex++)
        {
            ref readonly var tile = ref map.Tiles[tileIndex];
            if (colours.TryGetValue((tile.X, tile.Y), out var rgb) && floorShown(tileIndex))
            {
                tiles.Add(
                    new PathTile(
                        tileIndex % WorldMap.Size,
                        tileIndex / WorldMap.Size,
                        TileColour(rgb, SpriteLighting.Brightness(tile.Light, worldGamma))));
            }
        }

        return tiles;
    }

    public void Show(IReadOnlyList<PathTile> tiles)
    {
        if (tiles.SequenceEqual(shown))
        {
            return;
        }

        shown = tiles;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var tile in shown)
        {
            var (x, y) = IsometricProjection.TilePosition(tile.Column, tile.Row);
            var centre = new Vector2(x, y) + DiamondCentre;
            corners[0] = centre + new Vector2(0, -HalfHeight);
            corners[1] = centre + new Vector2(HalfWidth, 0);
            corners[2] = centre + new Vector2(0, HalfHeight);
            corners[3] = centre + new Vector2(-HalfWidth, 0);
            DrawColoredPolygon(corners, tile.Colour);
        }
    }
}
