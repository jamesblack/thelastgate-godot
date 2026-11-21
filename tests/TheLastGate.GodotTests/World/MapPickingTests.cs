using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MapPickingTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsOnly = 0b0000_0010;

    [TestCase]
    public void TileAt_FindsThePlayersTileAtTheMiddleOfItsDiamond()
    {
        AssertThat(MapPicking.TileAt(638, 452).GetValueOrDefault()).IsEqual((27, 27));
    }

    [TestCase]
    public void TileAt_FindsTheNextTileAHalfTileAcrossAndAQuarterDown()
    {
        AssertThat(MapPicking.TileAt(654, 460).GetValueOrDefault()).IsEqual((28, 27));
    }

    [TestCase]
    public void TileAt_GivesAPixelOnAnEdgeToTheTileAfterIt()
    {
        AssertThat(MapPicking.TileAt(643, 457).GetValueOrDefault()).IsEqual((27, 27));
        AssertThat(MapPicking.TileAt(644, 457).GetValueOrDefault()).IsEqual((28, 27));
    }

    [TestCase]
    public void TileAt_IncludesTheOuterRowsOfTheClickableArea()
    {
        AssertThat(MapPicking.TileAt(304, 284).GetValueOrDefault()).IsEqual((6, 27));
        AssertThat(MapPicking.TileAt(973, 619).GetValueOrDefault()).IsEqual((48, 27));
    }

    [TestCase]
    public void TileAt_CutsOffTheOuterRowsWhereTheCurrentClientDoes()
    {
        AssertThat(MapPicking.TileAt(298, 282)).IsNull();
        AssertThat(MapPicking.TileAt(978, 622)).IsNull();
    }

    [TestCase]
    public void TileAt_FindsNothingAwayFromTheMap()
    {
        AssertThat(MapPicking.TileAt(0, 0)).IsNull();
        AssertThat(MapPicking.TileAt(1279, 719)).IsNull();
    }

    [TestCase]
    public void TileAt_CanPickTheTileDrawnUnderTheMouseWhileThePlayerIsMidStep()
    {
        AssertThat(MapPicking.TileAt(638, 452, 16, 8).GetValueOrDefault()).IsEqual((28, 27));
    }

    [TestCase]
    public void PointAt_GivesTheExactPositionWithinTheTile()
    {
        AssertThat(MapPicking.PointAt(638, 452).GetValueOrDefault()).IsEqual((27.5f, 27.5f));
        AssertThat(MapPicking.PointAt(638, 445).GetValueOrDefault()).IsEqual((27.0625f, 27.9375f));
    }

    [TestCase]
    public void PointAt_FindsNothingWhereTileAtFindsNothing()
    {
        AssertThat(MapPicking.PointAt(298, 282)).IsNull();
        AssertThat(MapPicking.PointAt(0, 0)).IsNull();
    }

    [TestCase]
    public void NearestTile_TakesTheTileUnderTheMouseFirst()
    {
        var map = MapWith(MapFlags.IsItem, (27, 27), (28, 27));

        AssertThat(MapPicking.NearestTile(map, MapFlags.IsItem, (27.9f, 27.5f)).GetValueOrDefault()).IsEqual((27, 27));
    }

    [TestCase]
    public void NearestTile_TakesTheClosestNeighbourToTheMouse()
    {
        var map = MapWith(MapFlags.IsItem, (26, 27), (28, 27));

        AssertThat(MapPicking.NearestTile(map, MapFlags.IsItem, (27.9f, 27.5f)).GetValueOrDefault()).IsEqual((28, 27));
    }

    [TestCase]
    public void NearestTile_BreaksATieByTheOrderTheNeighboursAreSearched()
    {
        var map = MapWith(MapFlags.IsItem, (28, 27), (26, 27));

        AssertThat(MapPicking.NearestTile(map, MapFlags.IsItem, (27.5f, 27.5f)).GetValueOrDefault()).IsEqual((26, 27));
    }

    [TestCase]
    public void NearestTile_PrefersTheFirstRingEvenWhenTheSecondIsCloser()
    {
        var map = MapWith(MapFlags.IsCharacter, (26, 26), (29, 27));

        AssertThat(MapPicking.NearestTile(map, MapFlags.IsCharacter, (27.99f, 27.99f)).GetValueOrDefault())
            .IsEqual((26, 26));
    }

    [TestCase]
    public void NearestTile_LooksTwoTilesOutAtMost()
    {
        var twoOut = MapWith(MapFlags.IsItem, (29, 29));
        var threeOut = MapWith(MapFlags.IsItem, (30, 27));

        AssertThat(MapPicking.NearestTile(twoOut, MapFlags.IsItem, (27.5f, 27.5f)).GetValueOrDefault())
            .IsEqual((29, 29));
        AssertThat(MapPicking.NearestTile(threeOut, MapFlags.IsItem, (27.5f, 27.5f))).IsNull();
    }

    [TestCase]
    public void NearestTile_OnlyCountsTilesWithTheWantedFlag()
    {
        var map = MapWith(MapFlags.IsCharacter, (27, 27));

        AssertThat(MapPicking.NearestTile(map, MapFlags.IsItem, (27.5f, 27.5f))).IsNull();
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
