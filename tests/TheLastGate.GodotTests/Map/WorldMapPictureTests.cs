using GdUnit4;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class WorldMapPictureTests
{
    private const int BytesPerPixel = 4;

    [TestCase]
    public void RenderBlock_SpreadsMapYAcrossAndMapXDownOverTheWorldsTopHalf()
    {
        var map = new ExploredMap();
        map.SetTile(3, 5, 0x112233);
        map.SetTile(1023, 2047, 0x445566);

        AssertThat(WorldMapPicture.Width).IsEqual(2048);
        AssertThat(WorldMapPicture.Height).IsEqual(1024);
        AssertThat(Pixel(Block(map, 0, 0), 5, 3)).IsEqual(new byte[] { 0x11, 0x22, 0x33, 255 });
        AssertThat(Pixel(Block(map, 0, 0), 0, 0)).IsEqual(new byte[] { 0, 0, 0, 255 });
        AssertThat(Pixel(Block(map, 7, 3), 255, 255)).IsEqual(new byte[] { 0x44, 0x55, 0x66, 255 });
    }

    [TestCase]
    public void RenderBlock_StartsAtItsOwnCorner()
    {
        var map = new ExploredMap();
        map.SetTile(300, 600, 0x112233);

        AssertThat(Pixel(Block(map, 2, 1), 600 - 512, 300 - 256)).IsEqual(new byte[] { 0x11, 0x22, 0x33, 255 });
    }

    [TestCase]
    public void TheBlocks_CoverThePicture()
    {
        AssertThat(WorldMapPicture.BlocksAcross * WorldMapPicture.BlockSize).IsEqual(WorldMapPicture.Width);
        AssertThat(WorldMapPicture.BlocksDown * WorldMapPicture.BlockSize).IsEqual(WorldMapPicture.Height);
    }

    private static byte[] Block(ExploredMap map, int across, int down)
    {
        var rgba = new byte[WorldMapPicture.BlockSize * WorldMapPicture.BlockSize * BytesPerPixel];
        WorldMapPicture.RenderBlock(map, across, down, rgba);
        return rgba;
    }

    private static byte[] Pixel(byte[] rgba, int across, int down) =>
        rgba.AsSpan((down * WorldMapPicture.BlockSize + across) * BytesPerPixel, BytesPerPixel).ToArray();
}
