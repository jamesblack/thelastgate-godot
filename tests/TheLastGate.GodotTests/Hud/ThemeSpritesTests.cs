using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class ThemeSpritesTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;
    private const string ThemePath = "res://themes/OptionsTheme.tres";

    private SpriteLoader sprites = null!;

    [BeforeTest]
    public void OpenTheSprites()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
    }

    [AfterTest]
    public void CloseTheSprites() => sprites.Dispose();

    [TestCase]
    public void Textured_GivesEveryTaggedStyleboxItsSprite()
    {
        var textured = ThemeSprites.Textured(LoadTheme(), sprites);

        var boxes = TaggedBoxes(textured).ToList();
        AssertThat(boxes).IsNotEmpty();
        foreach (var box in boxes)
        {
            var expected = sprites.GetTexture(box.GetMeta("sprite").AsInt32())!;
            AssertThat(box.Texture).IsNotNull();
            AssertThat(box.Texture.GetSize()).IsEqual(expected.GetSize());
        }
    }

    [TestCase]
    public void Textured_LeavesTheOriginalThemeUntextured()
    {
        var original = (Theme)LoadTheme().Duplicate(true);

        ThemeSprites.Textured(original, sprites);

        AssertThat(TaggedBoxes(original).All(box => box.Texture == null)).IsTrue();
    }

    private static Theme LoadTheme() =>
        ResourceLoader.Load<Theme>(ThemePath, cacheMode: ResourceLoader.CacheMode.Ignore);

    private static IEnumerable<StyleBoxTexture> TaggedBoxes(Theme theme) =>
        theme.GetStyleboxTypeList()
            .SelectMany(type => theme.GetStyleboxList(type).Select(name => theme.GetStylebox(name, type)))
            .OfType<StyleBoxTexture>()
            .Where(box => box.HasMeta("sprite"));
}
