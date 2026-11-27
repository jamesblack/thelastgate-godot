using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class CharacterSheetTests
{
    [Theory]
    [InlineData("02000-knight.png", 2000, "knight")]
    [InlineData("22480-flame.PNG", 22480, "flame")]
    [InlineData("27088-braver-male.png", 27088, "braver-male")]
    public void FromFileName_ReadsBaseAndName(string fileName, int baseSprite, string name)
    {
        Assert.Equal(new CharacterSheet(baseSprite, name), CharacterSheet.FromFileName(fileName));
    }

    [Theory]
    [InlineData("02000.png")]
    [InlineData("2000-knight.png")]
    [InlineData("02000-Knight.png")]
    [InlineData("02000-knight.bmp")]
    [InlineData("02000-.png")]
    [InlineData("02000-knight-.png")]
    public void FromFileName_RejectsOtherNames(string fileName)
    {
        Assert.Null(CharacterSheet.FromFileName(fileName));
    }

    [Fact]
    public void FileName_PadsTheBase()
    {
        Assert.Equal("02000-knight.png", new CharacterSheet(2000, "knight").FileName);
    }

    [Theory]
    [InlineData(2000, true, 0)]
    [InlineData(2447, true, 447)]
    [InlineData(1999, false, 0)]
    [InlineData(2448, false, 0)]
    public void TryGetOffset_CoversBaseToBasePlus447(int spriteNumber, bool expected, int expectedOffset)
    {
        var found = new CharacterSheet(2000, "knight").TryGetOffset(spriteNumber, out var offset);

        Assert.Equal(expected, found);
        if (expected)
        {
            Assert.Equal(expectedOffset, offset);
        }
    }
}
