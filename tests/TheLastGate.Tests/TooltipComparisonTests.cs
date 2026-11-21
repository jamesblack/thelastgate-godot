using TheLastGate.Assets;
using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class TooltipComparisonTests
{
    private const string TableHeader = "Stat              Passiv   Active   Requi";

    [Fact]
    public void Comparing_StartsWithEquippedAndAddsTheChangeAfterAValue()
    {
        var parts = Compare(Item(Row("Armor Value", "   +2      +2")), Item(Row("Armor Value", "   +5      +5")));

        Assert.Equivalent(new TooltipLine(new TooltipText("Equipped", TextColour.Dim)), parts[0], true);
        var table = Table(parts);
        Assert.Equal(Cells(TextColour.Orange, "Stat", "Value"), table.Header);
        Assert.Equal(
            [Text("Armor Value", TextColour.Yellow), Gain("+2", TextColour.White, "+3")],
            table.Rows[0]);
    }

    [Fact]
    public void Comparing_ShowsALossInRed()
    {
        var table = Table(
            Compare(Item(Row("Armor Value", "   +2      +2")), Item(Row("Armor Value", "   +1      +1"))));

        Assert.Equal(Loss("+2", TextColour.White, "-1"), table.Rows[0][1]);
    }

    [Fact]
    public void Comparing_AddsNothingWhereTheValuesMatch()
    {
        var table = Table(
            Compare(Item(Row("Armor Value", "   +2      +2")), Item(Row("Armor Value", "   +2      +2"))));

        Assert.Equal(Text("+2", TextColour.White), table.Rows[0][1]);
    }

    [Fact]
    public void Comparing_WritesPercentChangesAsPercents()
    {
        var table = Table(
            Compare(Item(Row("Crit Bonus", "   +5%     +5%")), Item(Row("Crit Bonus", "   +8%     +8%"))));

        Assert.Equal(Gain("+5%", TextColour.White, "+3%"), table.Rows[0][1]);
    }

    [Fact]
    public void Comparing_ChangesTheActiveValueToo()
    {
        var table = Table(
            Compare(Item(Row("Strength", "   +2     +4      30")), Item(Row("Strength", "   +3     +6      30"))));

        Assert.Equal(Gain("+2", TextColour.White, "+1"), table.Rows[0][1]);
        Assert.Equal(Gain("+4", TextColour.Teal, "+2"), table.Rows[0][2]);
        Assert.Equal(Text("30", TextColour.Dim), table.Rows[0][3]);
    }

    [Fact]
    public void Comparing_CountsAStatTheHeldItemLacksAsALoss()
    {
        var table = Table(
            Compare(
                Item(Row("Armor Value", "   +2      +2"), Row("Hit Bonus", "   +1      +1")),
                Item(Row("Hit Bonus", "   +1      +1"))));

        Assert.Equal(Loss("+2", TextColour.White, "-2"), table.Rows[0][1]);
    }

    [Fact]
    public void Comparing_AddsDimRowsForStatsOnlyTheHeldItemHas()
    {
        var held = Item(Row("Armor Value", "   +2      +2"), Row("Agility", "   +4      +4"));

        var table = Table(Compare(Item(Row("Armor Value", "   +2      +2")), held));

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal([Text("Agility", TextColour.Dim), Gain("-", TextColour.Dim, "+4")], table.Rows[1]);
    }

    [Fact]
    public void Comparing_ChangesSingleValuesBelowTheTable()
    {
        var parts = Compare(Item(Row("Complexity", "     12")), Item(Row("Complexity", "     20")));

        Assert.Equivalent(
            new TooltipLine(Gain("Complexity: 12", TextColour.Yellow, "+8")),
            parts.OfType<TooltipLine>().Single(line => line.Text.Text.StartsWith("Complexity")),
            true);
    }

    [Fact]
    public void Comparing_ListsSingleValuesOnlyTheHeldItemHas()
    {
        var held = Item(
            Row("Armor Value", "   +2      +2"),
            Row("Complexity", "     20"),
            Row("Min. Rank", "      5 (Sergeant)"));

        var parts = Compare(Item(Row("Armor Value", "   +2      +2")), held);

        var lines = parts.OfType<TooltipLine>().Select(line => line.Text).ToList();
        Assert.Contains(Gain("Complexity: -", TextColour.Dim, "+20"), lines);
        Assert.Contains(Text("Min. Rank: -", TextColour.Dim), lines);
    }

    [Fact]
    public void Comparing_LeavesRanksUncompared()
    {
        var parts = Compare(Item(Row("Min. Rank", "      5 (Sergeant)")), Item(Row("Min. Rank", "      9 (Captain)")));

        Assert.Contains(
            Text("Min. Rank: 5 (Sergeant)", TextColour.Yellow),
            parts.OfType<TooltipLine>().Select(line => line.Text));
    }

    [Fact]
    public void Comparing_DoesNotCompareWithASoulstone()
    {
        LogLine[] soulstone =
        [
            Line(TextColour.Lime, "Soulstone:"),
            Line(TextColour.Yellow, "A glowing stone."),
            Line(TextColour.Orange, "Stat               Adds"),
            Line(TextColour.Yellow, Row("Armor Value", "    +3")),
        ];

        var table = Table(Compare(Item(Row("Armor Value", "   +2      +2")), ItemDetailsParser.Parse(soulstone)));

        Assert.Single(table.Rows);
        Assert.Equal(Text("+2", TextColour.White), table.Rows[0][1]);
    }

    [Fact]
    public void Comparing_SaysSoWhenTheHeldItemIsUnknown()
    {
        var parts = TooltipContent.Comparing(Item(Row("Armor Value", "   +2      +2")), null);

        Assert.Equivalent(new TooltipLine(new TooltipText("Equipped", TextColour.Dim)), parts[0], true);
        Assert.Equivalent(
            new TooltipLine(new TooltipText("No details for the held item.", TextColour.Dim)),
            parts[^1],
            true);
        Assert.Equal(Text("+2", TextColour.White), Table(parts).Rows[0][1]);
    }

    [Fact]
    public void ComparingEmptySlot_ShowsEverythingTheHeldItemHasAsAGain()
    {
        var held = Details(
            Row("Armor Value", "   +3      +3"),
            Row("Strength", "   +2     +4"),
            Row("Complexity", "     20"));

        var parts = TooltipContent.ComparingEmptySlot(held);

        Assert.Equivalent(new TooltipLine(new TooltipText("Empty slot", TextColour.Dim)), parts[0], true);
        Assert.IsType<TooltipSeparator>(parts[1]);
        var table = Table(parts);
        Assert.Equal(Cells(TextColour.Orange, "Stat", "Passive", "Active"), table.Header);
        Assert.Equal(
            [Text("Armor Value", TextColour.Dim), Gain("-", TextColour.Dim, "+3"), Gain("-", TextColour.Dim, "+3")],
            table.Rows[0]);
        Assert.Equal(
            [Text("Strength", TextColour.Dim), Gain("-", TextColour.Dim, "+2"), Gain("-", TextColour.Dim, "+4")],
            table.Rows[1]);
        Assert.Equivalent(new TooltipLine(Gain("Complexity: -", TextColour.Dim, "+20")), parts[^1], true);
    }

    [Fact]
    public void ComparingEmptySlot_ShowsJustTheHeadingForAnItemWithoutStats()
    {
        var parts = TooltipContent.ComparingEmptySlot(Details());

        var heading = new TooltipLine(new TooltipText("Empty slot", TextColour.Dim));
        Assert.Equivalent(new TooltipPart[] { heading }, parts, true);
    }

    private static IReadOnlyList<TooltipPart> Compare(ItemLook equipped, ItemLook held) =>
        TooltipContent.Comparing(equipped, held.Details);

    private static IReadOnlyList<TooltipPart> Compare(ItemLook equipped, ItemDetails held) =>
        TooltipContent.Comparing(equipped, held);

    private static ItemLook Item(params string[] rows)
    {
        var lines = Lines(rows);
        return new ItemLook(lines, ItemDetailsParser.Parse(lines));
    }

    private static ItemDetails Details(params string[] rows) => ItemDetailsParser.Parse(Lines(rows));

    private static LogLine[] Lines(string[] rows) =>
    [
        Line(TextColour.Lime, "Steel Sword:"),
        Line(TextColour.Yellow, "A sword."),
        .. rows.Length > 0 ? [Line(TextColour.Orange, TableHeader)] : Array.Empty<LogLine>(),
        .. rows.Select(row => Line(TextColour.Yellow, row)),
    ];

    private static TooltipTable Table(IReadOnlyList<TooltipPart> parts) => Assert.Single(parts.OfType<TooltipTable>());

    private static LogLine Line(TextColour font, string text) => new(text, (int)font);

    private static string Row(string name, string values) => name.PadRight(16) + values;

    private static TooltipText Text(string text, TextColour colour) => new(text, colour);

    private static TooltipText Gain(string text, TextColour colour, string change) =>
        new(text, colour, new TooltipChange(change, TextColour.Green));

    private static TooltipText Loss(string text, TextColour colour, string change) =>
        new(text, colour, new TooltipChange(change, TextColour.Red));

    private static TooltipText[] Cells(TextColour colour, params string[] texts) =>
        texts.Select(text => new TooltipText(text, colour)).ToArray();
}
