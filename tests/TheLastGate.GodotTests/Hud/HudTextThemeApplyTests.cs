using GdUnit4;
using Godot;
using TheLastGate.Assets;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class HudTextThemeApplyTests
{
    private const int OtherSize = 12;
    private static readonly FontVariation OtherFont = new();
    private static readonly Color OtherColour = new(0.2f, 0.4f, 0.6f);

    [TestCase]
    public void Apply_WritesEveryVariationAndTheDefault()
    {
        var theme = (Theme)HudTextTheme.Load().Duplicate();

        HudTextTheme.Apply(theme, new OtherFonts());

        foreach (var colour in Enum.GetValues<TextColour>())
        {
            var variation = HudTextTheme.Variation(colour);
            AssertThat(theme.GetFont("font", variation)).IsSame(OtherFont);
            AssertThat(theme.GetFontSize("font_size", variation)).IsEqual(OtherSize);
            AssertThat(theme.GetColor("font_color", variation)).IsEqual(OtherColour);
        }

        AssertThat(theme.DefaultFont).IsSame(OtherFont);
        AssertThat(theme.DefaultFontSize).IsEqual(OtherSize);
    }

    [TestCase]
    public void Apply_WritesEachSetsCaptionFont()
    {
        var theme = (Theme)HudTextTheme.Load().Duplicate();

        HudTextTheme.Apply(theme, new OtherFonts());

        var variation = HudTextTheme.CaptionVariation(TextColour.Engraved);
        AssertThat(theme.GetFont("font", variation)).IsSame(ClassicFonts.SavedCaption(TextColour.Engraved).Font);
        AssertThat(theme.GetColor("font_color", variation))
            .IsEqual(ClassicFonts.SavedCaption(TextColour.Engraved).Tint);
    }

    [TestCase]
    public void ClassicSet_ChangesNothing()
    {
        var original = HudTextTheme.Load();
        var theme = (Theme)original.Duplicate();

        HudTextTheme.Apply(theme, new ClassicFonts());

        foreach (var colour in Enum.GetValues<TextColour>())
        {
            var variation = HudTextTheme.Variation(colour);
            AssertThat(theme.GetFont("font", variation)).IsSame(original.GetFont("font", variation));
            AssertThat(theme.GetColor("font_color", variation)).IsEqual(original.GetColor("font_color", variation));
        }

        AssertThat(theme.DefaultFont).IsSame(original.DefaultFont);
    }

    [TestCase]
    public void AfterASwap_TheClassicSetIsStillTheClassicFontsAndPutsThemBack()
    {
        var shared = HudTextTheme.Load();
        HudTextTheme.Apply(shared, new OtherFonts());

        var yellow = new ClassicFonts().For(TextColour.Yellow);
        HudTextTheme.Apply(shared, new ClassicFonts());

        AssertThat(yellow.Font.ResourcePath).IsEqual("res://fonts/classic/yellow.tres");
        AssertThat(shared.GetFont("font", "HudYellow").ResourcePath).IsEqual("res://fonts/classic/yellow.tres");
        AssertThat(shared.DefaultFont.ResourcePath).IsEqual("res://fonts/classic/white.tres");
    }

    [TestCase]
    public void ApplyingTheSameSet_DoesNotChangeTheTheme()
    {
        var theme = (Theme)HudTextTheme.Load().Duplicate();
        var changes = 0;
        theme.Changed += () => changes++;

        HudTextTheme.Apply(theme, new ClassicFonts());

        AssertThat(changes).IsEqual(0);
    }

    [TestCase]
    public void ASwap_AnnouncesItselfOnce()
    {
        var theme = (Theme)HudTextTheme.Load().Duplicate();
        var changes = 0;
        theme.Changed += () => changes++;

        HudTextTheme.Apply(theme, new OtherFonts());

        AssertThat(changes).IsEqual(1);
        AssertThat(theme.IsBlockingSignals()).IsFalse();
    }

    private sealed class OtherFonts : IFontSet
    {
        public bool CentresLikeTheCurrentClient => false;

        public GameFont For(TextColour colour) => new(OtherFont, OtherSize, OtherColour);

        public GameFont Caption(TextColour colour) => ClassicFonts.SavedCaption(colour);
    }
}
