using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class HudLabelTests
{
    private HudLabel label = null!;
    private Control themed = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowALabel()
    {
        themed = new Control { Theme = HudTextTheme.Load() };
        label = new HudLabel();
        themed.AddChild(label);
        Tree.Root.AddChild(themed);
    }

    [AfterTest]
    public void FreeTheLabel() => themed.Free();

    [TestCase]
    public void NewLabel_IsYellow() => AssertThat(label.ThemeTypeVariation.ToString()).IsEqual("HudYellow");

    [TestCase]
    public void Colour_PicksItsVariation()
    {
        label.Colour = TextColour.Engraved;

        AssertThat(label.ThemeTypeVariation.ToString()).IsEqual("HudEngraved");
        AssertThat(label.GetThemeColor("font_color")).IsEqual(Color.Color8(86, 87, 76));
    }

    [TestCase]
    public void InsideAWindowWithItsOwnLabelColour_TheVariationStillWins()
    {
        var window = new Control { Theme = GD.Load<Theme>("res://themes/OptionsTheme.tres") };
        var engraved = new HudLabel { Colour = TextColour.Engraved };
        window.AddChild(engraved);
        Tree.Root.AddChild(window);

        AssertThat(engraved.GetThemeColor("font_color")).IsEqual(Color.Color8(86, 87, 76));
        AssertThat(engraved.GetThemeFont("font").ResourcePath).IsEqual("res://fonts/classic/white.tres");
        window.Free();
    }

    [TestCase]
    public void TheThemeAndVariation_AreNotSavedInTheScene()
    {
        var saved = label.GetPropertyList()
            .Where(property => ((PropertyUsageFlags)property["usage"].AsInt32()).HasFlag(PropertyUsageFlags.Storage))
            .Select(property => property["name"].AsString())
            .ToList();

        AssertThat(saved).NotContains("theme", "theme_type_variation");
        AssertThat(saved).Contains("Colour");
    }

    [TestCase]
    public void Label_TakesItsFontFromTheThemeNotAnOverride()
    {
        label.Colour = TextColour.White;

        AssertThat(label.HasThemeFontOverride("font")).IsFalse();
        AssertThat(label.GetThemeFont("font").ResourcePath).IsEqual("res://fonts/classic/white.tres");
    }
}
