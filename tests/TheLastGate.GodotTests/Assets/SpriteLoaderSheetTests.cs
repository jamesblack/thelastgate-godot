using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class SpriteLoaderSheetTests
{
    private const long CacheBytes = 64L * 1024 * 1024;
    private const string KnightSheet = "res://sprites/characters/02000-knight.png";
    private const int KnightWalkFrame = 2064;
    private const int KnightEmptyIdleSlot = 2005;
    private const int FlameFrame = 22481;
    private const int LooseSprite = 2;

    private TempFolder dataFolder = null!;

    [BeforeTest]
    public void MakeTheDataFolder() => dataFolder = new TempFolder();

    [AfterTest]
    public void RemoveTheDataFolder() => dataFolder.Dispose();

    [TestCase]
    public void GetTexture_KnightFrame_IsACellOfTheKnightSheet()
    {
        using var loader = NewLoader();

        var frame = loader.GetTexture(KnightWalkFrame) as AtlasTexture;

        AssertThat(frame).IsNotNull();
        AssertThat(frame!.Atlas.ResourcePath).IsEqual(KnightSheet);
        AssertThat(frame.Region).IsEqual(new Rect2(0, 2 * 64, 64, 64));
        AssertThat(loader.FindSource(KnightWalkFrame)).IsEqual(SpriteSource.Resource);
    }

    [TestCase]
    public void GetTexture_FlameFrame_Is32By64()
    {
        using var loader = NewLoader();

        var frame = loader.GetTexture(FlameFrame);

        AssertThat(frame).IsNotNull();
        AssertThat(frame!.GetWidth()).IsEqual(32);
        AssertThat(frame.GetHeight()).IsEqual(64);
    }

    [TestCase]
    public void GetTexture_EmptyCellOnASheet_IsThatTransparentCell()
    {
        using var loader = NewLoader();

        var frame = loader.GetTexture(KnightEmptyIdleSlot) as AtlasTexture;

        AssertThat(frame).IsNotNull();
        AssertThat(frame!.Atlas.ResourcePath).IsEqual(KnightSheet);
        AssertThat(frame.Region).IsEqual(new Rect2(5 * 64, 0, 64, 64));
        AssertThat(loader.FindSource(KnightEmptyIdleSlot)).IsEqual(SpriteSource.Resource);
    }

    [TestCase]
    public void HasFrame_TellsEmptyCellsFromFrames()
    {
        using var loader = NewLoader();

        AssertThat(loader.HasFrame(KnightEmptyIdleSlot)).IsFalse();
        AssertThat(loader.HasFrame(KnightWalkFrame)).IsTrue();
        AssertThat(loader.HasFrame(FlameFrame)).IsTrue();
        AssertThat(loader.HasFrame(LooseSprite)).IsTrue();
        AssertThat(loader.HasFrame(SpriteNumbers.MaxSprite - 1)).IsFalse();
    }

    [TestCase]
    public void HasFrame_OverrideInAnEmptyCell_IsAFrame()
    {
        WriteOverride(KnightEmptyIdleSlot, 64, 64, Colors.Red);
        using var loader = NewLoader();

        AssertThat(loader.HasFrame(KnightEmptyIdleSlot)).IsTrue();
    }

    [TestCase]
    public void GetTexture_OverrideOfTheCellSize_IsDrawnIntoTheSheet()
    {
        WriteOverride(KnightWalkFrame, 64, 64, Colors.Red);
        using var loader = NewLoader();

        var frame = (AtlasTexture)loader.GetTexture(KnightWalkFrame)!;
        var neighbour = (AtlasTexture)loader.GetTexture(KnightWalkFrame + 1)!;

        var sheet = frame.Atlas.GetImage();
        sheet.Convert(Image.Format.Rgba8);
        var cell = sheet.GetRegion((Rect2I)frame.Region);
        AssertThat(cell.GetPixel(0, 0)).IsEqual(Colors.Red);
        AssertThat(cell.GetPixel(63, 63)).IsEqual(Colors.Red);
        var imported = GD.Load<Texture2D>(KnightSheet).GetImage();
        imported.Convert(Image.Format.Rgba8);
        AssertThat(sheet.GetRegion((Rect2I)neighbour.Region).GetData())
            .IsEqual(imported.GetRegion((Rect2I)neighbour.Region).GetData());
        AssertThat(loader.FindSource(KnightWalkFrame)).IsEqual(SpriteSource.Override);
    }

    [TestCase]
    public void GetTexture_OverrideOfAnotherSize_IsItsOwnTexture()
    {
        WriteOverride(KnightWalkFrame, 40, 40, Colors.Red);
        using var loader = NewLoader();

        var frame = loader.GetTexture(KnightWalkFrame);

        AssertThat(frame).IsNotNull();
        AssertThat(frame is AtlasTexture).IsFalse();
        AssertThat(frame!.GetWidth()).IsEqual(40);
        AssertThat(frame.GetHeight()).IsEqual(40);
    }

    [TestCase]
    public void GetTexture_OverrideInAnEmptyCell_IsShown()
    {
        WriteOverride(KnightEmptyIdleSlot, 64, 64, Colors.Red);
        using var loader = NewLoader();

        var frame = loader.GetTexture(KnightEmptyIdleSlot) as AtlasTexture;

        AssertThat(frame).IsNotNull();
        AssertThat(frame!.Region).IsEqual(new Rect2(5 * 64, 0, 64, 64));
    }

    [TestCase]
    public void GetTexture_SwapOntoASheetFrame_ReturnsThatCell()
    {
        using var loader = NewLoader();
        loader.Swaps = new Dictionary<int, int> { [1] = KnightWalkFrame };

        var frame = loader.GetTexture(1) as AtlasTexture;

        AssertThat(frame).IsNotNull();
        AssertThat(frame!.Region).IsEqual(new Rect2(0, 2 * 64, 64, 64));
    }

    [TestCase]
    public void GetTexture_SpriteOffTheCharacterSheets_IsOnItsArtSheet()
    {
        using var loader = NewLoader();

        var sprite = loader.GetTexture(LooseSprite) as AtlasTexture;

        AssertThat(sprite).IsNotNull();
        AssertThat(sprite!.Atlas.ResourcePath).IsEqual("res://sprites/items-and-objects.png");
    }

    [TestCase]
    public void AverageColour_SheetFrame_MatchesTheCell()
    {
        WriteOverride(KnightWalkFrame, 64, 64, Colors.Red);
        using var loader = NewLoader();

        AssertThat(loader.AverageColour(KnightWalkFrame)).IsEqual(SpriteColours.Average(RedPixels(64, 64)));
    }

    private SpriteLoader NewLoader() => new(dataFolder.FolderPath, CacheBytes);

    private void WriteOverride(int spriteNumber, int width, int height, Color colour)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(colour);
        dataFolder.Write(Path.Combine(DataLayout.Overrides, $"{spriteNumber:D5}.png"), image.SavePngToBuffer());
    }

    private static byte[] RedPixels(int width, int height)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Red);
        return image.GetData();
    }
}
