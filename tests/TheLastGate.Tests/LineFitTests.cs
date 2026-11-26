using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class LineFitTests
{
    [Fact]
    public void Fit_TrimsTheExtraEvenlyAboveAndBelow() => Assert.Equal(new LineSpacing(-2, -2), LineFit.Fit(13, 9, 0));

    [Fact]
    public void Fit_OddExtra_TrimsMoreFromBelow() => Assert.Equal(new LineSpacing(-1, -2), LineFit.Fit(11.5f, 9, 0));

    [Fact]
    public void Fit_SmallFont_PadsUpToTheLine_TheOddPixelAbove() =>
        Assert.Equal(new LineSpacing(2, 1), LineFit.Fit(6, 9, 0));

    [Fact]
    public void Fit_PositiveOffset_MovesTheLettersDown() =>
        Assert.Equal(new LineSpacing(-1, -3), LineFit.Fit(13, 9, 1));

    [Theory]
    [InlineData(13f, 0)]
    [InlineData(18.8f, -2)]
    [InlineData(6f, 3)]
    public void Fit_AlwaysGivesTheLineHeight(float fontHeight, int offset)
    {
        var spacing = LineFit.Fit(fontHeight, 9, offset);

        Assert.Equal(9, (int)Math.Ceiling(fontHeight) + spacing.Top + spacing.Bottom);
    }
}
