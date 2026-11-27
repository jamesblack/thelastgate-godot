using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class LauncherPartFontsTests
{
    private const string PixelOperatorPath = "res://fonts/pixeloperator.ttf";
    private const string ClassicWhitePath = "res://fonts/classic/white.tres";

    private readonly PixelOperatorFonts launcherFonts = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [AfterTest]
    public void GiveTheHudBackItsFonts() => HudTextTheme.Apply(HudTextTheme.Load(), new ClassicFonts());

    [TestCase]
    public void ForEveryColour_UsesTheRegularFontAtTheLauncherSize()
    {
        var parts = new LauncherPartFonts(launcherFonts);

        foreach (var colour in Enum.GetValues<TextColour>())
        {
            AssertThat(parts.For(colour).Font).IsEqual(launcherFonts.Regular);
            AssertThat(parts.For(colour).Size).IsEqual(launcherFonts.Size);
        }
    }

    [TestCase]
    public void EveryColour_KeepsTheHudTint()
    {
        var parts = new LauncherPartFonts(launcherFonts);

        AssertThat(parts.For(TextColour.Yellow).Tint).IsEqual(ClassicFonts.SavedCaption(TextColour.Yellow).Tint);
        AssertThat(parts.For(TextColour.Grey).Tint).IsEqual(ClassicFonts.SavedCaption(TextColour.Grey).Tint);
    }

    [TestCase]
    public void Captions_UseTheBoldFont()
    {
        var parts = new LauncherPartFonts(launcherFonts);

        AssertThat(parts.Caption(TextColour.Grey).Font).IsEqual(launcherFonts.Bold);
    }

    [TestCase]
    public void AHudLabelUnderTheLauncherSet_DrawsInPixelOperator()
    {
        HudTextTheme.Apply(HudTextTheme.Load(), new LauncherPartFonts(launcherFonts));
        var label = AutoFree(new HudLabel())!;
        Tree.Root.AddChild(label);

        AssertThat(label.GetThemeFont("font").ResourcePath).IsEqual(PixelOperatorPath);
    }

    [TestCase]
    public void TheHud_TakesBackItsOwnFonts()
    {
        HudTextTheme.Apply(HudTextTheme.Load(), new LauncherPartFonts(launcherFonts));
        HudTextTheme.Apply(HudTextTheme.Load(), new ClassicFonts());
        var label = AutoFree(new HudLabel { Colour = TextColour.White })!;
        Tree.Root.AddChild(label);

        AssertThat(label.GetThemeFont("font").ResourcePath).IsEqual(ClassicWhitePath);
    }
}
