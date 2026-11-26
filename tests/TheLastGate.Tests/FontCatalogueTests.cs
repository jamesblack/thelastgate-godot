using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class FontCatalogueTests
{
    private const string Vt323 =
        """{ "id": "vt323", "name": "VT323", "file": "vt323-regular.ttf", "size": 12, "offset": -1, "smooth": false }""";

    [Fact]
    public void Parse_ReadsEveryField()
    {
        var catalogue = FontCatalogue.Parse($"[{Vt323}]");

        Assert.Equal([new FontEntry("vt323", "VT323", "vt323-regular.ttf", 12, -1, false)], catalogue.Fonts);
        Assert.Empty(catalogue.Problems);
    }

    [Fact]
    public void Parse_SkipsEntryMissingAField_AndNamesItsId()
    {
        var catalogue = FontCatalogue.Parse(
            """[{ "id": "syne", "name": "Syne", "size": 9, "offset": 0, "smooth": true }]""");

        Assert.Empty(catalogue.Fonts);
        Assert.Contains("syne", Assert.Single(catalogue.Problems));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Parse_SkipsNonPositiveSizes(int size)
    {
        var catalogue = FontCatalogue.Parse(
            $$"""[{ "id": "syne", "name": "Syne", "file": "syne-regular.ttf", "size": {{size}}, "offset": 0, "smooth": true }]""");

        Assert.Empty(catalogue.Fonts);
        Assert.Single(catalogue.Problems);
    }

    [Fact]
    public void Parse_SkipsDuplicateIds_KeepsTheFirst()
    {
        var second = Vt323.Replace("\"VT323\"", "\"Second\"");

        var catalogue = FontCatalogue.Parse($"[{Vt323}, {second}]");

        Assert.Equal("VT323", Assert.Single(catalogue.Fonts).Name);
        Assert.Single(catalogue.Problems);
    }

    [Fact]
    public void Parse_SkipsTheReservedClassicId()
    {
        var catalogue = FontCatalogue.Parse($"[{Vt323.Replace("\"vt323\"", "\"classic\"")}]");

        Assert.Empty(catalogue.Fonts);
        Assert.Single(catalogue.Problems);
    }

    [Theory]
    [InlineData("../x.ttf")]
    [InlineData("sub/x.ttf")]
    public void Parse_RejectsFileWithAPath(string file)
    {
        var catalogue = FontCatalogue.Parse($"[{Vt323.Replace("vt323-regular.ttf", file)}]");

        Assert.Empty(catalogue.Fonts);
        Assert.Single(catalogue.Problems);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    public void Parse_MalformedJson_GivesNoFontsAndAProblem(string json)
    {
        var catalogue = FontCatalogue.Parse(json);

        Assert.Empty(catalogue.Fonts);
        Assert.Single(catalogue.Problems);
    }

    [Fact]
    public void Parse_KeepsFileOrder()
    {
        var entries = new[] { "tiny5", "aaa", "silkscreen" }.Select(id => Vt323.Replace("\"vt323\"", $"\"{id}\""));

        var catalogue = FontCatalogue.Parse($"[{string.Join(", ", entries)}]");

        Assert.Equal(["tiny5", "aaa", "silkscreen"], catalogue.Fonts.Select(font => font.Id));
    }
}
