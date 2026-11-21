using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Tests;

public class IsometricProjectionTests
{
    private const int Centre = WorldMap.Size / 2;

    [Fact]
    public void TilePosition_PutsTheFirstTileWhereTheCurrentClientDoes()
    {
        Assert.Equal((-50, 460), IsometricProjection.TilePosition(0, 0));
    }

    [Fact]
    public void TilePosition_StepsAlongTheDiamondGrid()
    {
        var origin = IsometricProjection.TilePosition(0, 0);

        Assert.Equal((origin.X + 16, origin.Y + 8), IsometricProjection.TilePosition(1, 0));
        Assert.Equal((origin.X + 16, origin.Y - 8), IsometricProjection.TilePosition(0, 1));
    }

    [Fact]
    public void TilePosition_PutsThePlayersTileInTheMiddleOnceTheMapShifts()
    {
        var (x, y) = IsometricProjection.TilePosition(Centre, Centre);

        Assert.Equal((638, 460), (x + IsometricProjection.BaseMapShiftX, y));
    }

    [Theory]
    [InlineData(64, 32, -32, -32)]
    [InlineData(32, 64, -16, -64)]
    [InlineData(50, 75, -16, -64)]
    [InlineData(20, 10, 0, 0)]
    public void SpriteOffset_CountsOnlyWholeTilesOfTheSpritesSize(int width, int height, int offsetX, int offsetY)
    {
        Assert.Equal((offsetX, offsetY), IsometricProjection.SpriteOffset(width, height));
    }

    [Fact]
    public void MapShift_StaysPutWhileThePlayerStandsStill()
    {
        Assert.Equal((IsometricProjection.BaseMapShiftX, 0), IsometricProjection.MapShift((0, 0)));
    }

    [Fact]
    public void MapShift_SlidesTheMapAgainstThePlayersWalk()
    {
        Assert.Equal((IsometricProjection.BaseMapShiftX + 6, -3), IsometricProjection.MapShift((-6, 3)));
    }
}
