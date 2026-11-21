using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Tests;

public class MapPickingTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsOnly = 0b0000_0010;

    [Fact]
    public void TileAt_FindsThePlayersTileAtTheMiddleOfItsDiamond()
    {
        Assert.Equal((27, 27), MapPicking.TileAt(638, 452));
    }

    [Fact]
    public void TileAt_FindsTheNextTileAHalfTileAcrossAndAQuarterDown()
    {
        Assert.Equal((28, 27), MapPicking.TileAt(654, 460));
    }

    [Fact]
    public void TileAt_GivesAPixelOnAnEdgeToTheTileAfterIt()
    {
        Assert.Equal((27, 27), MapPicking.TileAt(643, 457));
        Assert.Equal((28, 27), MapPicking.TileAt(644, 457));
    }

    [Fact]
    public void TileAt_IncludesTheOuterRowsOfTheClickableArea()
    {
        Assert.Equal((6, 27), MapPicking.TileAt(304, 284));
        Assert.Equal((48, 27), MapPicking.TileAt(973, 619));
    }

    [Fact]
    public void TileAt_CutsOffTheOuterRowsWhereTheCurrentClientDoes()
    {
        Assert.Null(MapPicking.TileAt(298, 282));
        Assert.Null(MapPicking.TileAt(978, 622));
    }

    [Fact]
    public void TileAt_FindsNothingAwayFromTheMap()
    {
        Assert.Null(MapPicking.TileAt(0, 0));
        Assert.Null(MapPicking.TileAt(1279, 719));
    }

    [Fact]
    public void TileAt_CanPickTheTileDrawnUnderTheMouseWhileThePlayerIsMidStep()
    {
        Assert.Equal((28, 27), MapPicking.TileAt(638, 452, 16, 8));
    }

    [Fact]
    public void PointAt_GivesTheExactPositionWithinTheTile()
    {
        Assert.Equal((27.5f, 27.5f), MapPicking.PointAt(638, 452));
        Assert.Equal((27.0625f, 27.9375f), MapPicking.PointAt(638, 445));
    }

    [Fact]
    public void PointAt_FindsNothingWhereTileAtFindsNothing()
    {
        Assert.Null(MapPicking.PointAt(298, 282));
        Assert.Null(MapPicking.PointAt(0, 0));
    }

    [Fact]
    public void NearestTile_TakesTheTileUnderTheMouseFirst()
    {
        var map = MapWith(MapFlags.IsItem, (27, 27), (28, 27));

        Assert.Equal((27, 27), MapPicking.NearestTile(map, MapFlags.IsItem, (27.9f, 27.5f)));
    }

    [Fact]
    public void NearestTile_TakesTheClosestNeighbourToTheMouse()
    {
        var map = MapWith(MapFlags.IsItem, (26, 27), (28, 27));

        Assert.Equal((28, 27), MapPicking.NearestTile(map, MapFlags.IsItem, (27.9f, 27.5f)));
    }

    [Fact]
    public void NearestTile_BreaksATieByTheOrderTheNeighboursAreSearched()
    {
        var map = MapWith(MapFlags.IsItem, (28, 27), (26, 27));

        Assert.Equal((26, 27), MapPicking.NearestTile(map, MapFlags.IsItem, (27.5f, 27.5f)));
    }

    [Fact]
    public void NearestTile_PrefersTheFirstRingEvenWhenTheSecondIsCloser()
    {
        var map = MapWith(MapFlags.IsCharacter, (26, 26), (29, 27));

        Assert.Equal((26, 26), MapPicking.NearestTile(map, MapFlags.IsCharacter, (27.99f, 27.99f)));
    }

    [Fact]
    public void NearestTile_LooksTwoTilesOutAtMost()
    {
        Assert.Equal(
            (29, 29),
            MapPicking.NearestTile(MapWith(MapFlags.IsItem, (29, 29)), MapFlags.IsItem, (27.5f, 27.5f)));
        Assert.Null(MapPicking.NearestTile(MapWith(MapFlags.IsItem, (30, 27)), MapFlags.IsItem, (27.5f, 27.5f)));
    }

    [Fact]
    public void NearestTile_OnlyCountsTilesWithTheWantedFlag()
    {
        var map = MapWith(MapFlags.IsCharacter, (27, 27));

        Assert.Null(MapPicking.NearestTile(map, MapFlags.IsItem, (27.5f, 27.5f)));
    }

    private static WorldMap MapWith(MapFlags flags, params (int Column, int Row)[] tiles)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                tiles.Select(tile => TestCommand.Of(AbsoluteSetMap)
                        .Byte(FlagsOnly)
                        .UInt16((ushort)(tile.Column + tile.Row * WorldMap.Size))
                        .UInt32((uint)flags))
                    .ToArray()));
        return state.Map;
    }
}
