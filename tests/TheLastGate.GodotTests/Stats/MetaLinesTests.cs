using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class MetaLinesTests
{
    [TestCase(0, TextColour.Red)]
    [TestCase(1, TextColour.Yellow)]
    [TestCase(7, TextColour.Teal)]
    [TestCase(9, TextColour.White)]
    [TestCase(12, TextColour.Yellow)]
    [TestCase(-1, TextColour.Yellow)]
    public void Colour_FollowsTheServersFontNumbers(int font, TextColour colour)
    {
        AssertThat(MetaLines.Colour(font)).IsEqual(colour);
    }

    [TestCase]
    public void Description_PrintsTheNameThenTheText()
    {
        AssertThat(MetaLines.Description("Hit Score", "How often you hit."))
            .ContainsExactly(
                new LogLine("Hit Score", (int)TextColour.Orange),
                new LogLine("How often you hit.", (int)TextColour.Yellow));
    }

    [TestCase("  Passive Stats:")]
    [TestCase("  Active Stats:")]
    public void Description_OfAHeading_IsEmpty(string heading)
    {
        AssertThat(MetaLines.Description(heading, "")).IsEmpty();
    }
}
