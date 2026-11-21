using TheLastGate.State;

namespace TheLastGate.Tests;

public class MetaStatTests
{
    [Theory]
    [InlineData(1234, false, "1234")]
    [InlineData(0, false, "0")]
    [InlineData(1234, true, "12.34")]
    [InlineData(1205, true, "12.05")]
    [InlineData(7, true, "0.07")]
    public void ValueText_PrintsWholeNumbersOrHundredths(int value, bool hundredths, string text)
    {
        Assert.Equal(text, MetaStat.ValueText(value, hundredths));
    }

    [Fact]
    public void ValueText_LeavesTheUnsetValueBlank()
    {
        Assert.Equal("", MetaStat.ValueText(65535, false));
    }
}
