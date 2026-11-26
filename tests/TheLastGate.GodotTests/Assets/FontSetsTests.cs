using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Assets;

[TestSuite]
[RequireGodotRuntime]
public class FontSetsTests
{
    private const string Dogica = "dogicapixel";
    private const int ClassicLineHeight = 9;

    private static readonly string DataRoot = Path.Combine(RepositoryFolder.Find(), "data");

    private static FontSets Shipped() => new(FontSets.ReadCatalogue(DataRoot), DataRoot);

    [TestCase]
    public void TheShippedFolder_OffersDogicaPixel()
    {
        var entry = Shipped().Available.Single();

        AssertThat(entry.Id).IsEqual(Dogica);
        AssertThat(entry.Name).IsEqual("Dogica Pixel");
    }

    [TestCase]
    public void ClassicOrAnUnknownChoice_IsTheClassicSet()
    {
        var fonts = Shipped();

        AssertThat(fonts.For(FontChoice.ClassicId)).IsInstanceOf<ClassicFonts>();
        AssertThat(fonts.For("comic-sans")).IsInstanceOf<ClassicFonts>();
        AssertThat(fonts.For(null)).IsInstanceOf<ClassicFonts>();
    }

    [TestCase]
    public void DogicaPixel_IsCrispAndFillsTheClassicLine()
    {
        var yellow = Shipped().For(Dogica).For(TextColour.Yellow);
        var file = (FontFile)((FontVariation)yellow.Font).BaseFont;

        AssertThat(file.Antialiasing).IsEqual(TextServer.FontAntialiasing.None);
        AssertThat(yellow.Font.GetHeight(yellow.Size)).IsEqual(ClassicLineHeight);
    }

    [TestCase]
    public void DogicaPixel_TintsEachColourLikeTheClassicLetters()
    {
        var fonts = Shipped().For(Dogica);

        AssertThat(fonts.For(TextColour.Yellow).Tint).IsEqual(Color.Color8(243, 219, 106));
        AssertThat(fonts.For(TextColour.Engraved).Tint).IsEqual(ClassicFonts.SavedCaption(TextColour.Engraved).Tint);
        AssertThat(fonts.CentresLikeTheCurrentClient).IsFalse();
    }

    [TestCase]
    public void DogicaPixel_KeepsTheSilkscreenCaptions()
    {
        var caption = Shipped().For(Dogica).Caption(TextColour.Engraved);

        AssertThat(caption.Font.ResourcePath).IsEqual("res://fonts/caption.tres");
    }

    [TestCase]
    public void ResettingTheDisplayTab_GoesBackToTheClassicFont()
    {
        var settings = ClientSettings.Load();
        settings.FontId = Dogica;

        settings.Reset(OptionsTab.Display);

        AssertThat(settings.FontId).IsEqual(FontChoice.ClassicId);
    }
}
