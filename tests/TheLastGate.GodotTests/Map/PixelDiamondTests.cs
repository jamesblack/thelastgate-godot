using GdUnit4;
using Godot;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class PixelDiamondTests
{
    [TestCase]
    public void Solid_FillsEachRowOutFromTheMiddle()
    {
        var diamond = PixelDiamond.Solid(3);

        AssertThat(diamond.GetSize()).IsEqual(new Vector2I(7, 7));
        AssertThat(Row(diamond, 0)).IsEqual("...#...");
        AssertThat(Row(diamond, 2)).IsEqual(".#####.");
        AssertThat(Row(diamond, 3)).IsEqual("#######");
        AssertThat(Row(diamond, 6)).IsEqual("...#...");
    }

    [TestCase]
    public void Dotted_MarksBothEdgesOfEveryOtherRow()
    {
        var diamond = PixelDiamond.Dotted(4);

        AssertThat(Row(diamond, 0)).IsEqual("....#....");
        AssertThat(Row(diamond, 1)).IsEqual(".........");
        AssertThat(Row(diamond, 2)).IsEqual("..#...#..");
        AssertThat(Row(diamond, 4)).IsEqual("#.......#");
        AssertThat(Row(diamond, 8)).IsEqual("....#....");
    }

    private static string Row(Image image, int row) =>
        string.Concat(
            Enumerable.Range(0, image.GetWidth()).Select(column => image.GetPixel(column, row).A > 0 ? '#' : '.'));
}
