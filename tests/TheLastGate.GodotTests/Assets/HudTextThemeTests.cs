using GdUnit4;
using Godot;
using TheLastGate.Assets;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class HudTextThemeTests
{
    private const string ThemePath = "res://themes/HudTextTheme.tres";
    private const string WhiteFontPath = "res://fonts/classic/white.tres";
    private const int FontSize = 9;
    private const int CaptionSize = 8;

    private static readonly Vector2I CacheSize = new(FontSize, 0);

    private static Theme Theme => GD.Load<Theme>(ThemePath);

    [TestCase]
    public void EveryColour_HasAVariationBasedOnLabel()
    {
        foreach (var colour in Enum.GetValues<TextColour>())
        {
            AssertThat(Theme.GetTypeVariationBase("Hud" + colour).ToString()).IsEqual("Label");
        }
    }

    [TestCase]
    public void EveryColour_HasACaptionVariationInSilkscreen()
    {
        foreach (var colour in Enum.GetValues<TextColour>())
        {
            var variation = HudTextTheme.CaptionVariation(colour);
            AssertThat(Theme.GetTypeVariationBase(variation).ToString()).IsEqual("Label");
            AssertThat(Theme.GetFont("font", variation).ResourcePath).IsEqual("res://fonts/caption.tres");
            AssertThat(Theme.GetFontSize("font_size", variation)).IsEqual(CaptionSize);
        }
    }

    [TestCase]
    public void TheCaptionFont_IsSilkscreenWithItsLinesTightened()
    {
        var caption = GD.Load<FontVariation>("res://fonts/caption.tres");
        var silkscreen = (FontFile)caption.BaseFont;

        AssertThat(silkscreen.ResourcePath).IsEqual("res://fonts/silkscreen.ttf");
        AssertThat(silkscreen.Antialiasing).IsEqual(TextServer.FontAntialiasing.None);
        AssertThat(caption.SpacingTop).IsEqual(-3);
        AssertThat(caption.SpacingBottom).IsEqual(-3);
    }

    [TestCase]
    public void CaptionColours_MatchTheClassicLetters()
    {
        AssertThat(Theme.GetColor("font_color", "HudCaptionYellow")).IsEqual(Color.Color8(243, 219, 106));
        AssertThat(Theme.GetColor("font_color", "HudCaptionEngraved")).IsEqual(Color.Color8(76, 76, 67));
    }

    [TestCase]
    public void SheetColours_UseTheirOwnSheet()
    {
        var font = (FontFile)Theme.GetFont("font", "HudYellow");

        AssertThat(font.ResourcePath).IsEqual("res://fonts/classic/yellow.tres");
        AssertThat(Theme.GetFontSize("font_size", "HudYellow")).IsEqual(FontSize);
        AssertThat(Theme.GetColor("font_color", "HudYellow")).IsEqual(Colors.White);
        AssertThat(Rgba(font.GetTextureImage(0, CacheSize, 0)))
            .IsEqual(Rgba(GD.Load<Texture2D>("res://sprites/18101.png").GetImage()));
    }

    [TestCase]
    public void TintedColours_UseTheWhiteFont()
    {
        AssertTinted("HudGrey", Color.Color8(72, 81, 81));
        AssertTinted("HudDim", Color.Color8(144, 154, 154));
        AssertTinted("HudEngraved", Color.Color8(86, 87, 76));
    }

    [TestCase]
    public void GlyphA_ComesFromItsCell()
    {
        var font = GD.Load<FontFile>("res://fonts/classic/yellow.tres");
        const int letterA = 65;

        AssertThat(font.GetGlyphUVRect(0, CacheSize, letterA)).IsEqual(new Rect2(198, 1, 6, 9));
        AssertThat(font.GetGlyphSize(0, CacheSize, letterA)).IsEqual(new Vector2(6, 9));
        AssertThat(font.GetGlyphOffset(0, CacheSize, letterA)).IsEqual(new Vector2(0, -9));
        AssertThat(font.GetGlyphAdvance(0, FontSize, letterA)).IsEqual(new Vector2(6, 0));
    }

    [TestCase]
    public void DefaultFont_IsWhiteAtNine()
    {
        AssertThat(Theme.DefaultFont.ResourcePath).IsEqual(WhiteFontPath);
        AssertThat(Theme.DefaultFontSize).IsEqual(FontSize);
    }

    private static void AssertTinted(string variation, Color tint)
    {
        AssertThat(Theme.GetFont("font", variation).ResourcePath).IsEqual(WhiteFontPath);
        AssertThat(Theme.GetColor("font_color", variation)).IsEqual(tint);
    }

    private static byte[] Rgba(Image image)
    {
        var copy = (Image)image.Duplicate();
        copy.Convert(Image.Format.Rgba8);
        return copy.GetData();
    }
}
