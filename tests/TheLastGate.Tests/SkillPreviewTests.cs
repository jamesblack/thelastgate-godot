using TheLastGate.Rules;

namespace TheLastGate.Tests;

public class SkillPreviewTests
{
    private static readonly int[] Values = [60, 61, 62];

    [Fact]
    public void AttributeDivisor_IsFiveOrSixInHardMode()
    {
        Assert.Equal(5, SkillPreview.AttributeDivisor(false));
        Assert.Equal(6, SkillPreview.AttributeDivisor(true));
    }

    [Fact]
    public void AttributeGain_IsNothingUntilTheSumCrossesTheDivisor()
    {
        Assert.Equal(0, SkillPreview.AttributeGain(Values, [1, 0, 0], 5));
    }

    [Fact]
    public void AttributeGain_CountsOnceTheSumCrossesTheDivisor()
    {
        Assert.Equal(1, SkillPreview.AttributeGain(Values, [2, 0, 0], 5));
    }

    [Fact]
    public void AttributeGain_InHardModeNeedsMore()
    {
        Assert.Equal(1, SkillPreview.AttributeGain(Values, [3, 0, 0], 6));
        Assert.Equal(0, SkillPreview.AttributeGain(Values, [2, 0, 0], 6));
    }

    [Fact]
    public void AttributeGain_AddsUpEveryAttribute()
    {
        Assert.Equal(3, SkillPreview.AttributeGain([50, 50, 50], [5, 5, 5], 5));
    }

    [Fact]
    public void NoRaises_GainNothingAndUseNoRaisedAttribute()
    {
        Assert.Equal(0, SkillPreview.AttributeGain(Values, [0, 0, 0], 5));
        Assert.False(SkillPreview.UsesRaisedAttribute([0, 0, 0]));
        Assert.True(SkillPreview.UsesRaisedAttribute([0, 0, 4]));
    }
}
