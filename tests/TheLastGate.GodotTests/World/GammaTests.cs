using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class GammaTests
{
    [TestCase]
    public void Range_RunsFromTheCurrentClientsNormalToItsBrightestInHundredths()
    {
        AssertThat(Gamma.Normal).IsEqual(1f);
        AssertThat(Gamma.Lowest).IsEqual(1f);
        AssertThat(Gamma.Highest).IsEqual(1.2f);
        AssertThat(Gamma.Step).IsEqual(0.01f);
    }

    [TestCase(0.5f, 1f)]
    [TestCase(1.07f, 1.07f)]
    [TestCase(3f, 1.2f)]
    [TestCase(float.NaN, 1f)]
    public void Clamp_KeepsASavedGammaInsideTheRange(float saved, float gamma)
    {
        AssertThat(Gamma.Clamp(saved)).IsEqual(gamma);
    }

    [TestCase(1f, "1.00")]
    [TestCase(1.05f, "1.05")]
    [TestCase(1.2f, "1.20")]
    public void Text_ShowsTwoDecimalPlaces(float gamma, string text)
    {
        AssertThat(Gamma.Text(gamma)).IsEqual(text);
    }

    [TestCase(1f, 1.05f)]
    [TestCase(1.05f, 1.1f)]
    [TestCase(1.1f, 1.15f)]
    [TestCase(1.15f, 1.2f)]
    [TestCase(1.2f, 1f)]
    public void NextStep_GoesUpInTheCurrentClientsStepsAndWrapsAfterTheBrightest(float gamma, float next)
    {
        AssertThat(Gamma.NextStep(gamma)).IsEqual(next);
    }

    [TestCase(1.01f, 1.05f)]
    [TestCase(1.04f, 1.05f)]
    [TestCase(1.13f, 1.15f)]
    [TestCase(1.19f, 1.2f)]
    public void NextStep_MovesASliderValueUpToTheNextStep(float gamma, float next)
    {
        AssertThat(Gamma.NextStep(gamma)).IsEqual(next);
    }

    [TestCase]
    public void WorldGammaLine_SaysTheNewWorldGammaInGreen()
    {
        AssertThat(Gamma.WorldGammaLine(1.05f)).IsEqual(new LogLine("Set world gamma to 1.05", (int)TextColour.Green));
    }
}
