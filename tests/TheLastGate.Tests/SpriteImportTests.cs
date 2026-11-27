using System.Globalization;
using TheLastGate.Assets;

namespace TheLastGate.Tests;

// Sprites are pixel art drawn 1:1, so every one must import lossless, without mipmaps and without its transparent
// edges being recoloured. The project's import defaults give new sprites these settings; this catches one changed.
public class SpriteImportTests
{
    private static readonly string[] RequiredSettings =
    [
        "compress/mode=0",
        "mipmaps/generate=false",
        "process/fix_alpha_border=false",
    ];

    private static string SpriteFolder => Path.Combine(RepositoryFolder.Find(), "game", "sprites");

    [Fact]
    public void EverySprite_HasAnImportFile()
    {
        var missing = Directory.EnumerateFiles(SpriteFolder, "*.png", SearchOption.AllDirectories)
            .Where(png => !File.Exists(png + ".import"));

        Assert.Empty(missing);
    }

    [Fact]
    public void EverySprite_ImportsPixelExact()
    {
        var wrong = Directory.EnumerateFiles(SpriteFolder, "*.png.import", SearchOption.AllDirectories)
            .Where(import => !RequiredSettings.All(File.ReadAllLines(import).Contains))
            .Select(Path.GetFileName);

        Assert.Empty(wrong);
    }

    [Fact]
    public void NoLooseSprite_FallsInASheetRange()
    {
        var sheets = Directory.EnumerateFiles(Path.Combine(SpriteFolder, "characters"), "*.png")
            .Select(sheet => CharacterSheet.FromFileName(Path.GetFileName(sheet)))
            .OfType<CharacterSheet>()
            .ToList();

        var misplaced = Directory.EnumerateFiles(SpriteFolder, "*.png")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => int.Parse(name!, CultureInfo.InvariantCulture))
            .Where(spriteNumber => sheets.Any(sheet => sheet.TryGetOffset(spriteNumber, out _)));

        Assert.NotEmpty(sheets);
        Assert.Empty(misplaced);
    }
}
