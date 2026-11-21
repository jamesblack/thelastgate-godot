using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Items;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Items;

[TestSuite]
public class TooltipContentTests
{
    private const string TableHeader = "Stat              Passiv   Active   Requi";

    private static readonly LogLine Name = Line(TextColour.Lime, "Steel Sword:");
    private static readonly LogLine Description = Line(TextColour.Yellow, "A finely crafted steel sword.");

    [TestCase]
    public void For_ShowsTheNameInItsColourAndTheDescriptionWrapped()
    {
        TooltipParts.AssertEqual(
            Content(Name, Description),
            new TooltipLine(new TooltipText("Steel Sword", TextColour.Lime)),
            new TooltipLine(new TooltipText("A finely crafted steel sword.", TextColour.Yellow), true));
    }

    [TestCase]
    public void For_ShowsJustTheNameWhenThereIsNothingElse()
    {
        TooltipParts.AssertEqual(
            Content(Line(TextColour.Lime, "Talisman:")),
            new TooltipLine(new TooltipText("Talisman", TextColour.Lime)));
    }

    [TestCase]
    public void For_ShowsEffectsWrappedInTheirOwnColours()
    {
        var parts = Content(Name, Description, Line(TextColour.Purple, "20% more maximum Endurance."));

        TooltipParts.AssertEqual(
            parts.Skip(2),
            new TooltipSeparator(),
            new TooltipLine(new TooltipText("20% more maximum Endurance.", TextColour.Purple), true));
    }

    [TestCase]
    public void For_ShowsActiveAndRequiredColumnsWhenAStatNeedsThem()
    {
        var table = Table(
            Content(
                Name,
                Description,
                Line(TextColour.Orange, TableHeader),
                Line(TextColour.Yellow, Row("Strength", "   +2     +4      30")),
                Line(TextColour.Yellow, Row("Weapon Value", "  +24     +24")),
                Line(TextColour.Yellow, Row("Crit Bonus", "   +5%     +5%")),
                Line(TextColour.Yellow, Row("Dmg Bonus", " +1.5%   +2.5%"))));

        AssertThat(table.Header).ContainsExactly(Cells(TextColour.Orange, "Stat", "Passive", "Active", "Req"));
        AssertThat(table.Rows[0])
            .ContainsExactly(
                Text("Strength", TextColour.Yellow),
                Text("+2", TextColour.White),
                Text("+4", TextColour.Teal),
                Text("30", TextColour.Dim));
        AssertThat(table.Rows[1])
            .ContainsExactly(
                Text("Weapon Value", TextColour.Yellow),
                Text("+24", TextColour.White),
                Text("+24", TextColour.Teal),
                Text("", TextColour.Dim));
        AssertThat(table.Rows[2][1]).IsEqual(Text("+5%", TextColour.White));
        AssertThat(table.Rows[3][1]).IsEqual(Text("+1.5%", TextColour.White));
        AssertThat(table.Rows[3][2]).IsEqual(Text("+2.5%", TextColour.Teal));
    }

    [TestCase]
    public void For_ShowsOnlyAValueColumnWhenNoStatNeedsMore()
    {
        var table = Table(
            Content(
                Name,
                Description,
                Line(TextColour.Orange, TableHeader),
                Line(TextColour.Yellow, Row("Armor Value", "   +2      +2"))));

        AssertThat(table.Header).ContainsExactly(Cells(TextColour.Orange, "Stat", "Value"));
        AssertThat(table.Rows[0]).ContainsExactly(Text("Armor Value", TextColour.Yellow), Text("+2", TextColour.White));
    }

    [TestCase]
    public void For_ShowsNegativeValuesInRed()
    {
        var table = Table(
            Content(
                Name,
                Description,
                Line(TextColour.Orange, TableHeader),
                Line(TextColour.Yellow, Row("Agility", "   -3     -5      10"))));

        AssertThat(table.Rows[0][1]).IsEqual(Text("-3", TextColour.Red));
        AssertThat(table.Rows[0][2]).IsEqual(Text("-5", TextColour.Red));
    }

    [TestCase]
    public void For_ShowsASoulstoneTableWithAnAddsColumnOnly()
    {
        var table = Table(
            Content(
                Line(TextColour.Lime, "Soulstone:"),
                Line(TextColour.Yellow, "A glowing stone."),
                Line(TextColour.Orange, "Stat               Adds"),
                Line(TextColour.Yellow, Row("Dagger", "    +3")),
                Line(TextColour.Yellow, Row("Armor Value", "   +2      +5"))));

        AssertThat(table.Header).ContainsExactly(Cells(TextColour.Orange, "Stat", "Adds"));
        AssertThat(table.Rows[0]).ContainsExactly(Text("Dagger", TextColour.Yellow), Text("+3", TextColour.White));
        AssertThat(table.Rows[1]).ContainsExactly(Text("Armor Value", TextColour.Yellow), Text("+2", TextColour.White));
    }

    [TestCase]
    public void For_WritesRegenAndDurationInTheTableAndSingleValuesAsLinesBelowIt()
    {
        var parts = Content(
            Line(TextColour.Lime, "Healing Potion:"),
            Line(TextColour.Yellow, "Restores health over time."),
            Line(TextColour.Orange, TableHeader),
            Line(TextColour.Yellow, Row("HP Regen", "  +120 (  +4/s)")),
            Line(TextColour.Yellow, Row("Duration", "     30 seconds")),
            Line(TextColour.Yellow, Row("Complexity", "     12")),
            Line(TextColour.Yellow, Row("Min. Rank", "      5 (Sergeant)")));

        var table = Table(parts);
        AssertThat(table.Header).ContainsExactly(Cells(TextColour.Orange, "Stat", "Value"));
        AssertThat(table.Rows[0])
            .ContainsExactly(Text("HP Regen", TextColour.Yellow), Text("+120 (+4/s)", TextColour.White));
        AssertThat(table.Rows[1])
            .ContainsExactly(Text("Duration", TextColour.Yellow), Text("30 seconds", TextColour.White));
        TooltipParts.AssertEqual(
            parts.SkipWhile(part => part is not TooltipTable).Skip(1),
            new TooltipLine(new TooltipText("Complexity: 12", TextColour.Yellow)),
            new TooltipLine(new TooltipText("Min. Rank: 5 (Sergeant)", TextColour.Yellow)));
    }

    [TestCase]
    public void For_ShowsSingleValuesWithoutATableWhenTheyAreTheOnlyStats()
    {
        var parts = Content(
            Name,
            Description,
            Line(TextColour.Orange, TableHeader),
            Line(TextColour.Yellow, Row("Complexity", "     12")));

        AssertThat(parts.OfType<TooltipTable>().ToArray()).IsEmpty();
        TooltipParts.AssertEqual(parts[^1], new TooltipLine(new TooltipText("Complexity: 12", TextColour.Yellow)));
    }

    [TestCase]
    public void For_ShowsWhatHasBeenAndCanBeDoneToTheItemAsBadges()
    {
        var parts = Content(
            Name,
            Description,
            Line(TextColour.Purple, "Has been Enchanted."),
            Line(TextColour.Purple, "Has been Corrupted."),
            Line(TextColour.Orange, "Can be Whetstoned."),
            Line(TextColour.Red, "Cannot be Soulstoned."));

        TooltipParts.AssertEqual(
            parts.Skip(2),
            new TooltipSeparator(),
            new TooltipBadges("Has:", [Text("Enchanted", TextColour.Purple), Text("Corrupted", TextColour.Pink)]),
            new TooltipBadges("Can:", [Text("Whetstone", TextColour.Orange)]),
            new TooltipBadges("Cannot:", [Text("Soulstone", TextColour.Red)]));
    }

    [TestCase]
    public void For_SumsUpLevelBlessingsConditionAndPriceAtTheFoot()
    {
        var parts = Content(
            Name,
            Description,
            Line(TextColour.Green, "Level 3. Next level in 120 kills."),
            Line(TextColour.Green, "1 more blessing. Next in 40 kills."),
            Line(TextColour.Yellow, "It's fairly old."),
            Line(TextColour.Lime, "You estimate this could sell for 12G 34S."));

        TooltipParts.AssertEqual(
            parts.Skip(2),
            new TooltipSeparator(),
            new TooltipLine(new TooltipText("Level 3 - 120 kills to next", TextColour.Green)),
            new TooltipLine(new TooltipText("1 blessing left - next in 40 kills", TextColour.Green)),
            new TooltipLine(new TooltipText("Fairly old", TextColour.Yellow)),
            new TooltipLine(new TooltipText("Sells for about 12G 34S", TextColour.Lime)));
    }

    [TestCase]
    public void For_ShowsTheWorstConditionInRedAndCountsBlessingsInThePlural()
    {
        var parts = Content(
            Name,
            Description,
            Line(TextColour.Green, "3 more blessings. Next in 40 kills."),
            Line(TextColour.Yellow, "It is very old and battered."));

        TooltipParts.AssertEqual(
            parts.Skip(3),
            new TooltipLine(new TooltipText("3 blessings left - next in 40 kills", TextColour.Green)),
            new TooltipLine(new TooltipText("Very old and battered", TextColour.Red)));
    }

    [TestCase]
    public void For_PutsTheSectionsInOrderWithSeparatorsBetween()
    {
        var parts = Content(
            Name,
            Description,
            Line(TextColour.Purple, "Glows faintly."),
            Line(TextColour.Orange, TableHeader),
            Line(TextColour.Yellow, Row("Armor Value", "   +2      +2")),
            Line(TextColour.Purple, "Has been Enchanted."),
            Line(TextColour.Yellow, "It's fairly old."),
            Line(TextColour.Pink, "Something the parser has never seen."));

        AssertThat(parts.Select(part => part.GetType()))
            .ContainsExactly(
                typeof(TooltipLine),
                typeof(TooltipLine),
                typeof(TooltipSeparator),
                typeof(TooltipLine),
                typeof(TooltipSeparator),
                typeof(TooltipTable),
                typeof(TooltipSeparator),
                typeof(TooltipBadges),
                typeof(TooltipSeparator),
                typeof(TooltipLine),
                typeof(TooltipSeparator),
                typeof(TooltipLine));
        TooltipParts.AssertEqual(
            parts[^1],
            new TooltipLine(new TooltipText("Something the parser has never seen.", TextColour.Pink), true));
    }

    private static IReadOnlyList<TooltipPart> Content(params LogLine[] lines) =>
        TooltipContent.For(new ItemLook(lines, ItemDetailsParser.Parse(lines)));

    private static TooltipTable Table(IReadOnlyList<TooltipPart> parts) => parts.OfType<TooltipTable>().Single();

    private static LogLine Line(TextColour font, string text) => new(text, (int)font);

    private static string Row(string name, string values) => name.PadRight(16) + values;

    private static TooltipText Text(string text, TextColour colour) => new(text, colour);

    private static TooltipText[] Cells(TextColour colour, params string[] texts) =>
        texts.Select(text => new TooltipText(text, colour)).ToArray();
}
