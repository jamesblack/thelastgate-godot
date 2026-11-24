using GdUnit4;
using Godot;
using TheLastGate.Assets;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class ClassicFontsTests
{
    [TestCase]
    public void Engraved_IsTheWhiteLettersTintedLikeTheWindowArt()
    {
        var fonts = new ClassicFonts();

        var engraved = fonts.For(TextColour.Engraved);

        AssertThat(engraved.Font).IsSame(fonts.For(TextColour.White).Font);
        AssertThat(engraved.Tint).IsEqual(Color.Color8(86, 87, 76));
    }

    [TestCase]
    public void Yellow_IsTheYellowFontResource()
    {
        var yellow = new ClassicFonts().For(TextColour.Yellow);

        AssertThat(yellow.Font.ResourcePath).IsEqual("res://fonts/classic/yellow.tres");
        AssertThat(yellow.Size).IsEqual(9);
        AssertThat(yellow.Tint).IsEqual(Colors.White);
    }

    [TestCase]
    public void EveryColour_HasItsVariationName()
    {
        AssertThat(HudTextTheme.Variation(TextColour.Engraved)).IsEqual("HudEngraved");
    }
}
