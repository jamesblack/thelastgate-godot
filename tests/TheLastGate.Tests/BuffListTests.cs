using TheLastGate.Buffs;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class BuffListTests
{
    private readonly BuffListParser parser = new();

    private BuffReading Bless => parser.Readings[(int)BuffKind.Bless];

    private BuffReading Enhance => parser.Readings[(int)BuffKind.Enhance];

    private BuffReading Protect => parser.Readings[(int)BuffKind.Protect];

    [Fact]
    public void IsHeader_IsOnlyThePlayersOwnList()
    {
        Assert.True(BuffList.IsHeader("Your active buffs and debuffs:"));
        Assert.False(BuffList.IsHeader("Your companion's active buffs and debuffs:"));
    }

    [Fact]
    public void APowerLine_ReadsTheBuffsPower()
    {
        StartReply();
        parser.Feed("Bless power of 142 for 4m 10s");

        Assert.True(Bless.Known);
        Assert.Equal(142, Bless.Power);
        Assert.False(Bless.HasExact);
        Assert.False(Enhance.Known);
    }

    [Fact]
    public void BonusLines_GiveTheExactAttributeBonuses()
    {
        StartReply();
        parser.Feed("Bless power of 142 for 4m 10s");
        parser.Feed(" : +31 Braveness");
        parser.Feed(" : +30 Willpower");
        parser.Feed(" : +30 Intuition");
        parser.Feed(" : +30 Agility");
        parser.Feed(" : +29 Strength");

        Assert.True(Bless.HasExact);
        Assert.Equal(AttributeAmounts.Of(31, 30, 30, 30, 29), Bless.AttributeBonus);
    }

    [Fact]
    public void EnhanceAndProtect_GiveWeaponAndArmorValue()
    {
        StartReply();
        parser.Feed("Enhance power of 80 for 9m 0s");
        parser.Feed(" : +24 Weapon Value");
        parser.Feed("Protect power of 60 for 9m 0s");
        parser.Feed(" : +19 Armor Value");

        Assert.Equal(80, Enhance.Power);
        Assert.Equal(24, Enhance.WeaponBonus);
        Assert.Equal(60, Protect.Power);
        Assert.Equal(19, Protect.ArmorBonus);
        Assert.Equal(0, Enhance.ArmorBonus);
    }

    [Fact]
    public void OtherBuffs_AreIgnored()
    {
        StartReply();
        parser.Feed("Bless power of 50 for 1m 0s");
        parser.Feed("Curse power of 70 for 1m 0s");
        parser.Feed(" : -17 Braveness");
        parser.Feed("Magic Shield power of 50 / 12");

        Assert.Equal(50, Bless.Power);
        Assert.False(Bless.HasExact);
        Assert.Equal(0, Bless.AttributeBonus[AttributeSlot.Braveness]);
    }

    [Fact]
    public void ANoneReply_KnowsNoBuff()
    {
        Assert.True(parser.Feed(" "));
        Assert.True(parser.Feed("None."));
        Assert.False(parser.Feed(" "));
        Assert.All(parser.Readings, reading => Assert.False(reading.Known));
    }

    [Fact]
    public void TheReply_EndsAtTheSecondBlankLine()
    {
        Assert.True(parser.Feed(" "));
        Assert.True(parser.Feed("Bless power of 9 for 0m 5s"));
        Assert.False(parser.Feed(" "));
    }

    [Theory]
    [InlineData(0, 0, 88, 16, true)]
    [InlineData(88, 16, 0, 0, true)]
    [InlineData(88, 10, 88, 16, true)]
    [InlineData(88, 16, 88, 16, true)]
    [InlineData(88, 10, 87, 16, true)]
    [InlineData(88, 16, 88, 15, false)]
    [InlineData(500, 4, 500, 4, false)]
    [InlineData(0, 0, 500, 16, false)]
    [InlineData(0, 0, 0, 0, false)]
    public void SlotNeedsFetch_WhenOneOfTheThreeBuffsAppearsGoesOrIsRecast(
        int oldSprite,
        int oldActive,
        int newSprite,
        int newActive,
        bool needsFetch)
    {
        Assert.Equal(needsFetch, BuffList.SlotNeedsFetch(oldSprite, oldActive, newSprite, newActive));
    }

    private void StartReply() => parser.Feed(" ");
}
