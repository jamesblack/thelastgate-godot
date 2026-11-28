using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class ArtManifestTests
{
    private const string OneSprite = """
        {
          "sheet_sha256": "ab",
          "sprites": {
            "00116": {
              "h": 32,
              "source_sha256": "cd",
              "used": { "h": 16, "w": 32, "x": 0, "y": 16 },
              "w": 32,
              "x": 3,
              "y": 4
            }
          }
        }
        """;

    [Theory]
    [InlineData("floors.json", "floors")]
    [InlineData("raised-blocks.json", "raised-blocks")]
    [InlineData("unsorted-32x32.json", "unsorted-32x32")]
    public void SheetName_GroupFile_IsItsName(string fileName, string sheet)
    {
        Assert.Equal(sheet, ArtManifest.SheetName(fileName));
    }

    [Theory]
    [InlineData("Floors.json")]
    [InlineData("floors.png")]
    [InlineData("characters.json")]
    [InlineData("a_b.json")]
    [InlineData("-x.json")]
    [InlineData("00116.json")]
    public void SheetName_BadNames_AreNull(string fileName)
    {
        Assert.Null(ArtManifest.SheetName(fileName));
    }

    [Fact]
    public void Parse_ReadsRegionAndUsedArea()
    {
        var sprites = ArtManifest.Parse("floors", OneSprite);

        Assert.Equal(new ArtSprite("floors", new PixelRect(3, 4, 32, 32), new PixelRect(0, 16, 32, 16)), sprites[116]);
        Assert.Single(sprites);
    }

    [Fact]
    public void Parse_BadJson_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ArtManifest.Parse("floors", "{ \"sprites\": "));
    }

    [Fact]
    public void Parse_NonNumericKey_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ArtManifest.Parse("floors", OneSprite.Replace("00116", "tree")));
    }

    [Fact]
    public void Parse_MissingField_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ArtManifest.Parse("floors", OneSprite.Replace("\"w\": 32,", "")));
    }
}
