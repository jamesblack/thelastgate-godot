using System.Globalization;
using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Tests;

// Stat rows are built the way the server's printf formats write them.
public class ItemDetailsParserTests
{
    private const string TableHeader = "Stat              Passiv   Active   Requi";

    [Fact]
    public void Parse_ReadsAWeapon()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Steel Sword:"),
            Line(LogFont.Yellow, "A finely crafted steel sword."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, PlainWithRequired("Strength", 2, 4, 30)),
            Line(LogFont.Yellow, PlainWithRequired("Sword", 0, 0, 25)),
            Line(LogFont.Yellow, Plain("Weapon Value", 24, 24)),
            Line(LogFont.Yellow, WholePercent("Crit Bonus", 5, 5)),
            Line(LogFont.Yellow, TenthsPercent("Dmg Bonus", 1, 5, 2, 5)),
            Line(LogFont.Yellow, Rank("Min. Rank", 5, "Sergeant")),
            Line(LogFont.Orange, "Can be Whetstoned."),
            Line(LogFont.Red, "Cannot be Soulstoned."),
            Line(LogFont.Purple, "Has been Enchanted."),
            Line(LogFont.Yellow, "It's fairly old."),
            Line(LogFont.Lime, "You estimate this could sell for 12G 34S."),
        ]);

        Assert.Equal("Steel Sword", details.Name);
        Assert.Equal(1, details.DescriptionLine);
        Assert.False(details.SoulstoneTable);
        Assert.Equal(
            [
                new ItemStat("Strength", ItemStatKind.Plain, 2, 4, 30),
                new ItemStat("Sword", ItemStatKind.Plain, 0, 0, 25),
                new ItemStat("Weapon Value", ItemStatKind.Plain, 24, 24),
                new ItemStat("Crit Bonus", ItemStatKind.Percent, 50, 50),
                new ItemStat("Dmg Bonus", ItemStatKind.Percent, 15, 25),
                new ItemStat("Min. Rank", ItemStatKind.Rank, 5, RankName: "Sergeant"),
            ],
            details.Stats);
        Assert.Equal(ItemModsPossible.CanWhetstone | ItemModsPossible.CannotSoulstone, details.ModsPossible);
        Assert.Equal(ItemModsApplied.Enchanted, details.ModsApplied);
        Assert.Equal(2, details.Condition);
        Assert.Equal(1234, details.SellSilver);
        Assert.Empty(details.EffectLines);
        Assert.Empty(details.OtherLines);
    }

    [Fact]
    public void Parse_ReadsASoulstoneTable()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Soulstone:"),
            Line(LogFont.Yellow, "A glowing stone."),
            Line(LogFont.Orange, "Stat               Adds"),
            Line(LogFont.Yellow, Adds("Dagger", 3)),
            Line(LogFont.Yellow, Plain("Armor Value", 2, 2)),
        ]);

        Assert.True(details.SoulstoneTable);
        Assert.Equal(
            [new ItemStat("Dagger", ItemStatKind.Soulstone, 3), new ItemStat("Armor Value", ItemStatKind.Plain, 2, 2)],
            details.Stats);
    }

    [Fact]
    public void Parse_ReadsRegenDurationAndSingleValues()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Healing Potion:"),
            Line(LogFont.Yellow, "Restores health over time."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, Regen("HP Regen", 120, 4)),
            Line(LogFont.Yellow, Seconds("Duration", 30)),
            Line(LogFont.Yellow, Single("Complexity", 12)),
        ]);

        Assert.Equal(
            [
                new ItemStat("HP Regen", ItemStatKind.Regen, 120, 4),
                new ItemStat("Duration", ItemStatKind.Duration, 30),
                new ItemStat("Complexity", ItemStatKind.Single, 12),
            ],
            details.Stats);
    }

    [Fact]
    public void Parse_KeepsEffectsBeforeTheTableAndUnrecognisedLinesAfterIt()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Ring of Warmth:"),
            Line(LogFont.Yellow, "A warm ring."),
            Line(LogFont.Purple, "20% more maximum Endurance."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, Plain("Armor Value", 1, 1)),
            Line(LogFont.Green, "Level 3. Next level in 120 kills."),
            Line(LogFont.Pink, "Something the parser has never seen."),
        ]);

        Assert.Equal([2], details.EffectLines);
        Assert.Single(details.Stats);
        Assert.Equal(3, details.Level);
        Assert.Equal(120, details.KillsToLevel);
        Assert.Equal([6], details.OtherLines);
    }

    [Fact]
    public void Parse_LeavesOutTheComparisonWithTheHeldItem()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Steel Sword:"),
            Line(LogFont.Yellow, "A sword."),
            Line(LogFont.Yellow, " "),
            Line(LogFont.Yellow, "You compare it with a Bronze Sword:"),
            Line(LogFont.Yellow, "A Steel Sword is the better weapon."),
            Line(LogFont.Yellow, "No difference as armor."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, Plain("Weapon Value", 24, 24)),
        ]);

        Assert.Empty(details.EffectLines);
        Assert.Empty(details.OtherLines);
        Assert.Single(details.Stats);
    }

    [Fact]
    public void Parse_LeavesValuesThatWereNotShownEmpty()
    {
        var details = ItemDetailsParser.Parse([Line(LogFont.Lime, "Talisman:")]);

        Assert.Equal("Talisman", details.Name);
        Assert.Null(details.DescriptionLine);
        Assert.Null(details.Condition);
        Assert.Null(details.Level);
        Assert.Null(details.Blessings);
        Assert.Null(details.SellSilver);
    }

    [Fact]
    public void Parse_NamesAContractAndKeepsItsText()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Yellow, "A contract. It reads:"),
            Line(LogFont.Yellow, " "),
            Line(LogFont.Green, "Clear the area of all enemies."),
            Line(LogFont.Yellow, " "),
            Line(LogFont.Green, "  + Enemies have more gold"),
            Line(LogFont.Green, "A contract for a mercenary."),
        ]);

        Assert.Equal("Contract", details.Name);
        Assert.Empty(details.Stats);
        Assert.Equal([2, 4, 5], details.EffectLines);
    }

    [Theory]
    [InlineData("3 more blessings. Next in 40 kills.", 3, 40)]
    [InlineData("1 more blessing. Next in 15 kills.", 1, 15)]
    public void Parse_ReadsBlessings(string text, int blessings, int kills)
    {
        var details = ItemDetailsParser.Parse(
            [Line(LogFont.Lime, "Godly Blade:"), Line(LogFont.Yellow, "Blessed."), Line(LogFont.Green, text)]);

        Assert.Equal(blessings, details.Blessings);
        Assert.Equal(kills, details.KillsToBlessing);
    }

    [Fact]
    public void Parse_ReadsEveryAppliedModAndTheWorstCondition()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Old Axe:"),
            Line(LogFont.Yellow, "Heavy."),
            Line(LogFont.Purple, "Has been Whetstoned."),
            Line(LogFont.Purple, "Has been Soulstoned."),
            Line(LogFont.Purple, "Has been Catalyzed."),
            Line(LogFont.Purple, "Has been Enchanted."),
            Line(LogFont.Purple, "Has been Focused."),
            Line(LogFont.Purple, "Has been Augmented."),
            Line(LogFont.Purple, "Has been made easier to use."),
            Line(LogFont.Purple, "Has been Corrupted."),
            Line(LogFont.Purple, "Legacy item."),
            Line(LogFont.Orange, "Can be Enchanted."),
            Line(LogFont.Orange, "Can be Soulstoned."),
            Line(LogFont.Orange, "May be further enhanced by Osiris."),
            Line(LogFont.Red, "Cannot be Enchanted."),
            Line(LogFont.Yellow, "It is very old and battered."),
        ]);

        Assert.Equal(
            ItemModsApplied.Whetstoned | ItemModsApplied.Soulstoned | ItemModsApplied.Catalyzed
            | ItemModsApplied.Enchanted | ItemModsApplied.Focused | ItemModsApplied.Augmented | ItemModsApplied.Easier
            | ItemModsApplied.Corrupted | ItemModsApplied.Legacy,
            details.ModsApplied);
        Assert.Equal(
            ItemModsPossible.CanEnchant | ItemModsPossible.CanSoulstone | ItemModsPossible.CanOsiris
            | ItemModsPossible.CannotEnchant,
            details.ModsPossible);
        Assert.Equal(4, details.Condition);
    }

    [Fact]
    public void Parse_ReadsNegativeValues()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Cursed Ring:"),
            Line(LogFont.Yellow, "Cold."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, PlainWithRequired("Agility", -3, -5, 10)),
            Line(LogFont.Yellow, WholePercent("Crit Bonus", -2, -4)),
        ]);

        Assert.Equal(
            [
                new ItemStat("Agility", ItemStatKind.Plain, -3, -5, 10),
                new ItemStat("Crit Bonus", ItemStatKind.Percent, -20, -40),
            ],
            details.Stats);
    }

    [Fact]
    public void Parse_EndsTheTableAtTheFirstRowItCannotRead()
    {
        var details = ItemDetailsParser.Parse(
        [
            Line(LogFont.Lime, "Strange Gem:"),
            Line(LogFont.Yellow, "Odd."),
            Line(LogFont.Orange, TableHeader),
            Line(LogFont.Yellow, Plain("Armor Value", 1, 1)),
            Line(LogFont.Yellow, "Hums quietly."),
            Line(LogFont.Yellow, Plain("Weapon Value", 2, 2)),
        ]);

        Assert.Single(details.Stats);
        Assert.Equal([4, 5], details.OtherLines);
    }

    [Theory]
    [InlineData("Steel Sword:", LogFont.Lime, true)]
    [InlineData("A contract. It reads:", LogFont.Yellow, true)]
    [InlineData("A finely crafted sword:", LogFont.Yellow, false)]
    [InlineData("Steel Sword", LogFont.Lime, false)]
    [InlineData(":", LogFont.Lime, false)]
    public void IsReplyStart_SpotsTheFirstLineOfALookReply(string text, LogFont font, bool start)
    {
        Assert.Equal(start, ItemDetailsParser.IsReplyStart(text, (int)font));
    }

    private static LogLine Line(LogFont font, string text) => new(text, (int)font);

    private static string Name(string name) => name.PadRight(16)[..16];

    private static string Signed(int value, int width) =>
        (value >= 0 ? "+" + Whole(value) : Whole(value)).PadLeft(width);

    private static string Whole(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Adds(string name, int adds) => $"{Name(name)}    {Signed(adds, 2)}";

    private static string PlainWithRequired(string name, int passive, int active, int required) =>
        $"{Name(name)}   {Signed(passive, 4)}     {Signed(active, 4)}     {Whole(required).PadLeft(3)}";

    private static string Plain(string name, int passive, int active) =>
        $"{Name(name)}   {Signed(passive, 4)}     {Signed(active, 4)}";

    private static string WholePercent(string name, int passive, int active) =>
        $"{Name(name)}   {Signed(passive, 4)}%    {Signed(active, 4)}%";

    private static string TenthsPercent(string name, int passive, int passiveTenth, int active, int activeTenth) =>
        $"{Name(name)}  {Signed(passive, 2)}.{passiveTenth}%   {Signed(active, 2)}.{activeTenth}%";

    private static string Regen(string name, int total, int perSecond) =>
        $"{Name(name)}  {Signed(total, 4)} ({Signed(perSecond, 4)}/s)";

    private static string Seconds(string name, int seconds) => $"{Name(name)}   {Whole(seconds).PadLeft(4)} seconds";

    private static string Single(string name, int value) => $"{Name(name)}   {Whole(value).PadLeft(4)}";

    private static string Rank(string name, int rank, string rankName) =>
        $"{Name(name)}   {Whole(rank).PadLeft(4)} ({rankName})";
}
