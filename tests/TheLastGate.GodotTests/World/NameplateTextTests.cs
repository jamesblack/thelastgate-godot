using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class NameplateTextTests
{
    private const int Id = 41;

    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void IsShown_WhenEitherNamesOrPercentagesAreOn(bool showNames, bool showPercent, bool shown)
    {
        AssertThat(NameplateText.IsShown(showNames, showPercent)).IsEqual(shown);
    }

    [TestCase(64, true, true, "Guard 64%")]
    [TestCase(0, true, true, "Guard")]
    [TestCase(64, true, false, "Guard")]
    [TestCase(64, false, true, "64%")]
    [TestCase(0, false, true, "")]
    [TestCase(64, false, false, "")]
    public void For_CombinesTheNameAndPercentageAsTheOptionsSay(
        int healthPercent,
        bool showNames,
        bool showPercent,
        string text)
    {
        AssertThat(NameplateText.For("Guard", Id, healthPercent, showNames, showPercent)).IsEqual(text);
    }

    [TestCase]
    public void For_ShowsAPercentageAfterAnEmptyNameLikeTheCurrentClient()
    {
        AssertThat(NameplateText.For("", Id, 64, true, true)).IsEqual(" 64%");
    }

    [TestCase]
    public void For_ShowsOnlyTheNameForACharacterWithoutAnId()
    {
        AssertThat(NameplateText.For("Guard", 0, 64, false, true)).IsEqual("Guard");
    }

    [TestCase(0, 1)]
    [TestCase(1, 0)]
    [TestCase(5, 5)]
    [TestCase(9, 9)]
    public void FontFor_SwapsTheFirstTwoColours(int textColour, int font)
    {
        AssertThat(NameplateText.FontFor(textColour)).IsEqual(font);
    }

    [TestCase("Guard 64%", -22)]
    [TestCase("Guard", -12)]
    [TestCase("", 0)]
    public void LeftOffset_CentresAtFivePixelsALetterLikeTheCurrentClient(string text, int offset)
    {
        AssertThat(NameplateText.LeftOffset(text)).IsEqual(offset);
    }
}
