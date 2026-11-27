using GdUnit4;
using Godot;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class PathOverlayTests
{
    private const int OriginX = 500;
    private const int OriginY = 400;
    private const int Column = 30;
    private const int Row = 20;
    private const int TileIndex = Column + Row * WorldMap.Size;

    [TestCase]
    public void TileColour_ScalesByBrightness()
    {
        var colour = PathOverlay.TileColour(0xFF8000, 0.5f);

        AssertThat(colour.R).IsEqual(0.5f);
        AssertThat(Math.Abs(colour.G - 128 / 255f * 0.5f) < 0.001f).IsTrue();
        AssertThat(colour.B).IsEqual(0f);
        AssertThat(colour.A).IsEqual(PathOverlay.Alpha);
    }

    [TestCase]
    public void TilesInView_FindsPathTilesByWorldPosition()
    {
        var map = MapAtOrigin();
        ref readonly var tile = ref map.Tiles[TileIndex];
        var colours = new Dictionary<(int X, int Y), uint> { [(tile.X, tile.Y)] = 0xFFFFFF };

        var tiles = PathOverlay.TilesInView(map, colours, Gamma.Normal, _ => true);

        AssertThat(tiles.Select(found => (found.Column, found.Row)).ToArray()).ContainsExactly((Column, Row));
    }

    [TestCase]
    public void TilesInView_SkipsTilesWithNoFloor()
    {
        var map = MapAtOrigin();
        ref readonly var tile = ref map.Tiles[TileIndex];
        var colours = new Dictionary<(int X, int Y), uint> { [(tile.X, tile.Y)] = 0xFFFFFF };

        AssertThat(PathOverlay.TilesInView(map, colours, Gamma.Normal, _ => false)).IsEmpty();
    }

    [TestCase]
    public void TilesInView_SkipsPathTilesOutsideTheMap()
    {
        var colours = new Dictionary<(int X, int Y), uint> { [(2000, 2000)] = 0xFFFFFF };

        AssertThat(PathOverlay.TilesInView(MapAtOrigin(), colours, Gamma.Normal, _ => true)).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NoNotes_DrawsNothing()
    {
        var overlay = new PathOverlay();
        overlay.Show([new PathTile(Column, Row, Colors.Red)]);
        AssertThat(overlay.ShownTiles).IsEqual(1);

        overlay.Show([]);

        AssertThat(overlay.ShownTiles).IsEqual(0);
        overlay.Free();
    }

    private static WorldMap MapAtOrigin()
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(TestCommand.Of(ServerCommand.SetOrigin).UInt16(OriginX).UInt16(OriginY)));
        return state.Map;
    }
}
