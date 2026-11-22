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

    [Fact]
    public void EverySprite_HasAnImportFile()
    {
        var sprites = Path.Combine(RepositoryFolder.Find(), "game", "sprites");

        var missing = Directory.EnumerateFiles(sprites, "*.png").Where(png => !File.Exists(png + ".import"));

        Assert.Empty(missing);
    }

    [Fact]
    public void EverySprite_ImportsPixelExact()
    {
        var sprites = Path.Combine(RepositoryFolder.Find(), "game", "sprites");

        var wrong = Directory.EnumerateFiles(sprites, "*.png.import")
            .Where(import => !RequiredSettings.All(File.ReadAllLines(import).Contains))
            .Select(Path.GetFileName);

        Assert.Empty(wrong);
    }
}
