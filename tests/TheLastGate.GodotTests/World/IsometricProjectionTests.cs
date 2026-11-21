using GdUnit4;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class IsometricProjectionTests
{
    private const int Centre = WorldMap.Size / 2;

    [TestCase]
    public void TilePosition_PutsTheFirstTileWhereTheCurrentClientDoes()
    {
        AssertThat(IsometricProjection.TilePosition(0, 0)).IsEqual((-50, 460));
    }

    [TestCase]
    public void TilePosition_StepsAlongTheDiamondGrid()
    {
        var origin = IsometricProjection.TilePosition(0, 0);

        AssertThat(IsometricProjection.TilePosition(1, 0)).IsEqual((origin.X + 16, origin.Y + 8));
        AssertThat(IsometricProjection.TilePosition(0, 1)).IsEqual((origin.X + 16, origin.Y - 8));
    }

    [TestCase]
    public void TilePosition_PutsThePlayersTileInTheMiddleOnceTheMapShifts()
    {
        var (x, y) = IsometricProjection.TilePosition(Centre, Centre);

        AssertThat((x + IsometricProjection.BaseMapShiftX, y)).IsEqual((638, 460));
    }

    [TestCase(64, 32, -32, -32)]
    [TestCase(32, 64, -16, -64)]
    [TestCase(50, 75, -16, -64)]
    [TestCase(20, 10, 0, 0)]
    public void SpriteOffset_CountsOnlyWholeTilesOfTheSpritesSize(int width, int height, int offsetX, int offsetY)
    {
        AssertThat(IsometricProjection.SpriteOffset(width, height)).IsEqual((offsetX, offsetY));
    }

    [TestCase]
    public void MapShift_StaysPutWhileThePlayerStandsStill()
    {
        AssertThat(IsometricProjection.MapShift((0, 0))).IsEqual((IsometricProjection.BaseMapShiftX, 0));
    }

    [TestCase]
    public void MapShift_SlidesTheMapAgainstThePlayersWalk()
    {
        AssertThat(IsometricProjection.MapShift((-6, 3))).IsEqual((IsometricProjection.BaseMapShiftX + 6, -3));
    }
}
