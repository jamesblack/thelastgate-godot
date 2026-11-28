using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class ArchiveSpriteTests
{
    private const long CacheBytes = 64L * 1024 * 1024;

    private TempFolder dataFolder = null!;

    [BeforeTest]
    public void MakeTheDataFolder() => dataFolder = new TempFolder();

    [AfterTest]
    public void RemoveTheDataFolder() => dataFolder.Dispose();

    [TestCase]
    public void TrimToDrawnArea_ArtSprite_IsItsDrawnAreaOnTheSheet()
    {
        using var loader = new SpriteLoader(dataFolder.FolderPath, CacheBytes);
        var sprite = new ArchiveSprite { TrimToDrawnArea = true, SpriteNumber = 116 };

        sprite.ShowContext(new HudContext(null, loader, null));

        var trimmed = sprite.Texture as AtlasTexture;
        AssertThat(trimmed).IsNotNull();
        AssertThat(trimmed!.Atlas.ResourcePath).IsEqual("res://sprites/floors.png");
        AssertThat(trimmed.Region).IsEqual(new Rect2(1, 18, 32, 15));
        sprite.Free();
    }
}
