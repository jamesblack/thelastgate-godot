using GdUnit4;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class SpriteLightingTests
{
    [TestCase]
    public void Brightness_IsFullAtLightZero()
    {
        AssertThat(SpriteLighting.Brightness(0)).IsEqual(1f);
    }

    [TestCase(1, 120f / 121f)]
    [TestCase(5, 120f / 145f)]
    [TestCase(15, 120f / 345f)]
    public void Brightness_FollowsTheCurrentClientsCurve(int light, float brightness)
    {
        AssertThat(SpriteLighting.Brightness(light)).IsEqual(brightness);
    }

    [TestCase(5, 1.1f, 620f / 645f)]
    [TestCase(15, 1.2f, 1120f / 1345f)]
    [TestCase(15, 1f, 120f / 345f)]
    public void Brightness_UsesTheWorldGammaAsTheCurrentClientUsedItsGamma(
        int light,
        float worldGamma,
        float brightness)
    {
        AssertThat(MathF.Round(SpriteLighting.Brightness(light, worldGamma), 5)).IsEqual(MathF.Round(brightness, 5));
    }

    [TestCase]
    public void Brightness_LightsDarkTilesMoreWithAHigherWorldGamma()
    {
        AssertThat(SpriteLighting.Brightness(10, Gamma.Highest) > SpriteLighting.Brightness(10, Gamma.Normal)).IsTrue();
        AssertThat(SpriteLighting.Brightness(0, Gamma.Highest)).IsEqual(1f);
    }

    [TestCase]
    public void Brightness_DarkensWithEveryLevel()
    {
        for (var light = 1; light <= SpriteLighting.DarkestLight; light++)
        {
            AssertThat(SpriteLighting.Brightness(light) < SpriteLighting.Brightness(light - 1)).IsTrue();
        }
    }
}
