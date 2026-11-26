using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class ShippedFontsTests
{
    private const string LicenceSuffix = "-ofl.txt";
    private const string RegularSuffix = "-regular.ttf";

    private static readonly string FontsFolder = Path.Combine(RepositoryFolder.Find(), "data", "fonts");

    private static FontCatalogue Catalogue() =>
        FontCatalogue.Parse(File.ReadAllText(Path.Combine(FontsFolder, "fonts.json")));

    [Fact]
    public void Catalogue_ParsesWithNoProblems() => Assert.Empty(Catalogue().Problems);

    [Fact]
    public void Catalogue_ListsDogicaPixel() => Assert.Equal("dogicapixel", Assert.Single(Catalogue().Fonts).Id);

    [Fact]
    public void EveryListedFile_Exists()
    {
        foreach (var font in Catalogue().Fonts)
        {
            Assert.True(File.Exists(Path.Combine(FontsFolder, font.File)), $"{font.File} is listed but missing.");
        }
    }

    [Fact]
    public void EveryListedFile_HasItsLicenceBesideIt()
    {
        foreach (var font in Catalogue().Fonts)
        {
            var licence = font.File.Replace(RegularSuffix, LicenceSuffix, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(FontsFolder, licence)), $"{font.File} has no {licence}.");
        }
    }

    [Fact]
    public void EveryFileInTheFolder_IsLowerCase()
    {
        foreach (var file in Directory.GetFiles(FontsFolder)
            .Select(Path.GetFileName)
            .Where(file => file != "README.md"))
        {
            Assert.Equal(file!.ToLowerInvariant(), file);
        }
    }

    [Fact]
    public void EveryTtfInTheFolder_IsListed()
    {
        var listed = Catalogue().Fonts.Select(font => font.File).ToHashSet();

        Assert.All(
            Directory.GetFiles(FontsFolder, "*.ttf").Select(Path.GetFileName),
            file => Assert.Contains(file!, listed));
    }
}
