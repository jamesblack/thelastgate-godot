using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class SpriteLoaderArtSheetTests
{
    private const long CacheBytes = 64L * 1024 * 1024;
    private const string FloorSheet = "res://sprites/floors.png";
    private const int FloorSprite = 116;
    private const int FontStrip = 18101;
    private const int TransparentSprite = 10;

    private static readonly Rect2 FloorRegion = new(1, 1, 32, 32);
    private static readonly Rect2I FloorDrawnArea = new(0, 17, 32, 15);

    private TempFolder dataFolder = null!;

    [BeforeTest]
    public void MakeTheDataFolder() => dataFolder = new TempFolder();

    [AfterTest]
    public void RemoveTheDataFolder() => dataFolder.Dispose();

    [TestCase]
    public void GetTexture_ArtSprite_IsItsManifestRegion()
    {
        using var loader = NewLoader();

        var sprite = loader.GetTexture(FloorSprite) as AtlasTexture;

        AssertThat(sprite).IsNotNull();
        AssertThat(sprite!.Atlas.ResourcePath).IsEqual(FloorSheet);
        AssertThat(sprite.Region).IsEqual(FloorRegion);
        AssertThat(loader.FindSource(FloorSprite)).IsEqual(SpriteSource.Resource);
    }

    [TestCase]
    public void GetTexture_WideSprite_KeepsItsSize()
    {
        using var loader = NewLoader();

        var strip = loader.GetTexture(FontStrip);

        AssertThat(strip is AtlasTexture).IsTrue();
        AssertThat(strip!.GetWidth()).IsEqual(576);
        AssertThat(strip.GetHeight()).IsEqual(11);
    }

    [TestCase]
    public void GetTexture_OverrideOfAnArtSprite_IsItsOwnTexture()
    {
        WriteOverride(FloorSprite, 32, 32, Colors.Red);
        using var loader = NewLoader();

        var sprite = loader.GetTexture(FloorSprite);

        AssertThat(sprite).IsNotNull();
        AssertThat(sprite is AtlasTexture).IsFalse();
        AssertThat(sprite!.GetImage().GetPixel(5, 5)).IsEqual(Colors.Red);
        AssertThat(loader.FindSource(FloorSprite)).IsEqual(SpriteSource.Override);
    }

    [TestCase]
    public void HasFrame_ArtSprites()
    {
        using var loader = NewLoader();

        AssertThat(loader.HasFrame(FloorSprite)).IsTrue();
        AssertThat(loader.HasFrame(TransparentSprite)).IsTrue();
        AssertThat(loader.HasFrame(SpriteNumbers.MaxSprite - 1)).IsFalse();
    }

    [TestCase]
    public void AverageColour_ArtSprite_MatchesItsSource()
    {
        using var loader = NewLoader();

        AssertThat(loader.AverageColour(FloorSprite))
            .IsEqual(SpriteColours.Average(SourcePixels("floors", FloorSprite)));
    }

    [TestCase]
    public void AverageColour_EverySpriteOnASheet_MatchesItsSource()
    {
        using var loader = NewLoader();
        var folder = Path.Combine(RepositoryFolder.Find(), "art", "floors");
        var spriteNumbers = Directory.GetFiles(folder, "*.png")
            .Select(path => int.Parse(Path.GetFileNameWithoutExtension(path)))
            .Order()
            .Take(40)
            .ToList();

        foreach (var spriteNumber in spriteNumbers)
        {
            AssertThat(loader.AverageColour(spriteNumber))
                .OverrideFailureMessage($"sprite {spriteNumber}")
                .IsEqual(SpriteColours.Average(SourcePixels("floors", spriteNumber)));
        }
    }

    [TestCase]
    public void GetTexture_ArtSprites_StayCachedHoweverSmallTheBudget()
    {
        using var loader = new SpriteLoader(dataFolder.FolderPath, 1);

        var first = loader.GetTexture(FloorSprite);
        loader.GetTexture(FontStrip);

        AssertThat(loader.GetTexture(FloorSprite)).IsSame(first);
    }

    [TestCase]
    public void AverageColour_Override_UsesTheOverride()
    {
        WriteOverride(FloorSprite, 32, 32, Colors.Red);
        using var loader = NewLoader();

        AssertThat(loader.AverageColour(FloorSprite)).IsEqual(SpriteColours.Average(RedPixels(32, 32)));
    }

    [TestCase]
    public void DrawnArea_ArtSprite_IsItsUsedRect()
    {
        using var loader = NewLoader();

        AssertThat(loader.DrawnArea(FloorSprite)).IsEqual(FloorDrawnArea);
    }

    [TestCase]
    public void DrawnArea_Override_IsNull()
    {
        WriteOverride(FloorSprite, 32, 32, Colors.Red);
        using var loader = NewLoader();

        AssertThat(loader.DrawnArea(FloorSprite)).IsNull();
    }

    [TestCase]
    public void DrawnArea_TransparentSprite_IsNull()
    {
        using var loader = NewLoader();

        AssertThat(loader.DrawnArea(TransparentSprite)).IsNull();
    }

    [TestCase]
    public void Swap_OntoAnArtSprite_FollowsIt()
    {
        using var loader = NewLoader();
        loader.Swaps = new Dictionary<int, int> { [1] = FloorSprite };

        var sprite = loader.GetTexture(1) as AtlasTexture;

        AssertThat(sprite).IsNotNull();
        AssertThat(sprite!.Region).IsEqual(FloorRegion);
        AssertThat(loader.AverageColour(1)).IsEqual(loader.AverageColour(FloorSprite));
        AssertThat(loader.DrawnArea(1)).IsEqual(FloorDrawnArea);
    }

    private SpriteLoader NewLoader() => new(dataFolder.FolderPath, CacheBytes);

    private void WriteOverride(int spriteNumber, int width, int height, Color colour)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(colour);
        dataFolder.Write(Path.Combine(DataLayout.Overrides, $"{spriteNumber:D5}.png"), image.SavePngToBuffer());
    }

    private static byte[] SourcePixels(string group, int spriteNumber)
    {
        var path = Path.Combine(RepositoryFolder.Find(), "art", group, $"{spriteNumber:D5}.png");
        var image = Image.LoadFromFile(path);
        image.Convert(Image.Format.Rgba8);
        return image.GetData();
    }

    private static byte[] RedPixels(int width, int height)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Red);
        return image.GetData();
    }
}
