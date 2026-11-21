using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Items;

// Change is what equipping the held item would do to this value.
public readonly record struct TooltipText(string Text, TextColour Colour, TooltipChange? Change = null);

public readonly record struct TooltipChange(string Text, TextColour Colour);

public abstract record TooltipPart;

public sealed record TooltipLine(TooltipText Text, bool Wraps = false) : TooltipPart;

public sealed record TooltipSeparator : TooltipPart;

public sealed record TooltipTable(IReadOnlyList<TooltipText> Header, IReadOnlyList<IReadOnlyList<TooltipText>> Rows)
    : TooltipPart;

public sealed record TooltipBadges(string Label, IReadOnlyList<TooltipText> Badges) : TooltipPart;

public static class TooltipContent
{
    private const int WorstCondition = 4;
    private const int SilverPerGold = 100;
    private const int TenthsPerPercent = 10;

    private static readonly (ItemModsApplied Mod, TooltipText Badge)[] AppliedBadges =
    [
        (ItemModsApplied.Whetstoned, new TooltipText("Whetstoned", TextColour.Orange)),
        (ItemModsApplied.Soulstoned, new TooltipText("Soulstoned", TextColour.Blue)),
        (ItemModsApplied.Catalyzed, new TooltipText("Catalyzed", TextColour.Blue)),
        (ItemModsApplied.Enchanted, new TooltipText("Enchanted", TextColour.Purple)),
        (ItemModsApplied.Focused, new TooltipText("Focused", TextColour.Purple)),
        (ItemModsApplied.Augmented, new TooltipText("Augmented", TextColour.Teal)),
        (ItemModsApplied.Easier, new TooltipText("Easier to use", TextColour.Lime)),
        (ItemModsApplied.Corrupted, new TooltipText("Corrupted", TextColour.Pink)),
        (ItemModsApplied.Legacy, new TooltipText("Legacy", TextColour.White)),
    ];

    private static readonly (ItemModsPossible Mod, TooltipText Badge)[] PossibleBadges =
    [
        (ItemModsPossible.CanWhetstone, new TooltipText("Whetstone", TextColour.Orange)),
        (ItemModsPossible.CanSoulstone, new TooltipText("Soulstone", TextColour.Blue)),
        (ItemModsPossible.CanOsiris, new TooltipText("Osiris", TextColour.Blue)),
        (ItemModsPossible.CanEnchant, new TooltipText("Enchant", TextColour.Purple)),
    ];

    private static readonly (ItemModsPossible Mod, TooltipText Badge)[] ImpossibleBadges =
    [
        (ItemModsPossible.CannotSoulstone, new TooltipText("Soulstone", TextColour.Red)),
        (ItemModsPossible.CannotEnchant, new TooltipText("Enchant", TextColour.Red)),
    ];

    private static readonly string[] ConditionLabels =
        ["Perfect condition", "Showing signs of age", "Fairly old", "Old", "Very old and battered"];

    public static IReadOnlyList<TooltipPart> For(ItemLook look) => Body(look, null);

    public static IReadOnlyList<TooltipPart> Comparing(ItemLook equipped, ItemDetails? held)
    {
        var parts = new List<TooltipPart> { Line("Equipped", TextColour.Dim) };
        parts.AddRange(Body(equipped, held));
        if (held == null)
        {
            parts.Add(Line("No details for the held item.", TextColour.Dim));
        }

        return parts;
    }

    public static IReadOnlyList<TooltipPart> ComparingEmptySlot(ItemDetails held)
    {
        var parts = new List<TooltipPart> { Line("Empty slot", TextColour.Dim) };
        if (held.Stats.Count > 0)
        {
            AddSection(parts, Stats(new ItemDetails(), held));
        }

        return parts;
    }

    // `held` is the item that would replace this one, when comparing.
    private static List<TooltipPart> Body(ItemLook look, ItemDetails? held)
    {
        var lines = look.Lines;
        var details = look.Details;
        var parts = new List<TooltipPart> { new TooltipLine(new TooltipText(details.Name, Colour(lines[0]))) };
        if (details.DescriptionLine is { } description && lines[description].Text.Length > 0)
        {
            parts.Add(Wrapped(lines[description]));
        }

        AddSection(parts, details.EffectLines.Select(index => Wrapped(lines[index])));
        if (details.Stats.Count > 0 || held is { Stats.Count: > 0 })
        {
            AddSection(parts, Stats(details, held));
        }

        AddSection(parts, Badges(details));
        AddSection(parts, Footer(details));
        AddSection(parts, details.OtherLines.Select(index => Wrapped(lines[index])));
        return parts;
    }

    private static void AddSection(List<TooltipPart> parts, IEnumerable<TooltipPart> section)
    {
        var sectionParts = section.ToList();
        if (sectionParts.Count == 0)
        {
            return;
        }

        parts.Add(new TooltipSeparator());
        parts.AddRange(sectionParts);
    }

    private static TooltipLine Wrapped(LogLine line) => new(new TooltipText(line.Text, Colour(line)), true);

    private static TextColour Colour(LogLine line) =>
        line.Font is >= (int)TextColour.Red and <= (int)TextColour.White ? (TextColour)line.Font : TextColour.Yellow;

    private static IEnumerable<TooltipPart> Stats(ItemDetails shown, ItemDetails? held)
    {
        if (shown.SoulstoneTable || held is { SoulstoneTable: true })
        {
            held = null;
        }

        if (shown.Stats.Any(InTable) || (held != null && held.Stats.Any(InTable)))
        {
            yield return Table(shown, held);
        }

        foreach (var stat in shown.Stats.Where(stat => !InTable(stat)))
        {
            var change = held != null && Comparable(stat)
                ? Change(stat, (Find(held, stat.Name)?.Passive ?? 0) - stat.Passive)
                : null;
            yield return new TooltipLine(new TooltipText($"{stat.Name}: {Special(stat)}", TextColour.Yellow, change));
        }

        if (held == null)
        {
            yield break;
        }

        foreach (var gain in held.Stats.Where(stat => !InTable(stat) && Find(shown, stat.Name) == null))
        {
            var change = Comparable(gain) ? Change(gain, gain.Passive) : null;
            yield return new TooltipLine(new TooltipText($"{gain.Name}: -", TextColour.Dim, change));
        }
    }

    // Complexity and Min. Rank read better as lines under the table than as a column of mostly empty cells.
    private static bool InTable(ItemStat stat) => stat.Kind is not (ItemStatKind.Single or ItemStatKind.Rank);

    private static bool Comparable(ItemStat stat) =>
        stat.Kind is ItemStatKind.Plain or ItemStatKind.Percent or ItemStatKind.Single or ItemStatKind.Regen;

    private static ItemStat? Find(ItemDetails details, string name) =>
        details.Stats.FirstOrDefault(stat => stat.Name == name);

    private static TooltipTable Table(ItemDetails shown, ItemDetails? held)
    {
        var anyActive = shown.Stats.Any(ShowsActive) || (held != null && held.Stats.Any(ShowsActive));
        var showActive = !shown.SoulstoneTable && anyActive;
        var showRequired = !shown.SoulstoneTable && shown.Stats.Any(ShowsRequired);
        var valueLabel = shown.SoulstoneTable ? "Adds" : showActive ? "Passive" : "Value";
        var header = new List<string> { "Stat", valueLabel };
        if (showActive)
        {
            header.Add("Active");
        }

        if (showRequired)
        {
            header.Add("Req");
        }

        var rows = shown.Stats.Where(InTable).Select(stat => Row(stat, held, showActive, showRequired)).ToList();
        if (held != null)
        {
            var gains = held.Stats.Where(stat => InTable(stat) && Comparable(stat) && Find(shown, stat.Name) == null);
            rows.AddRange(gains.Select(gain => GainedRow(gain, showActive, showRequired)));
        }

        return new TooltipTable(header.Select(label => new TooltipText(label, TextColour.Orange)).ToList(), rows);
    }

    // Each value is followed by what swapping the held item in would change: held minus shown.
    private static IReadOnlyList<TooltipText> Row(ItemStat stat, ItemDetails? held, bool showActive, bool showRequired)
    {
        var compare = held != null && Comparable(stat);
        var heldStat = compare ? Find(held!, stat.Name) : null;
        var passiveChange = compare ? Change(stat, (heldStat?.Passive ?? 0) - stat.Passive) : null;
        var passive = Special(stat) ?? Value(stat, stat.Passive);
        var row = new List<TooltipText>
        {
            new(stat.Name, TextColour.Yellow),
            new(passive, stat.Passive < 0 ? TextColour.Red : TextColour.White, passiveChange),
        };
        if (showActive)
        {
            row.Add(HasActive(stat) ? ActiveCell(stat, heldStat, compare) : new TooltipText("", TextColour.Teal));
        }

        if (showRequired)
        {
            row.Add(new TooltipText(ShowsRequired(stat) ? Whole(stat.Required!.Value) : "", TextColour.Dim));
        }

        return row;
    }

    private static TooltipText ActiveCell(ItemStat stat, ItemStat? heldStat, bool compare)
    {
        var heldActive = heldStat != null && HasActive(heldStat) ? heldStat.Active : 0;
        var change = compare ? Change(stat, heldActive - stat.Active) : null;
        return new TooltipText(Value(stat, stat.Active), stat.Active < 0 ? TextColour.Red : TextColour.Teal, change);
    }

    // A stat only the held item has: equipping it would add it outright.
    private static IReadOnlyList<TooltipText> GainedRow(ItemStat gain, bool showActive, bool showRequired)
    {
        var row = new List<TooltipText>
        {
            new(gain.Name, TextColour.Dim),
            new("-", TextColour.Dim, Change(gain, gain.Passive)),
        };
        if (showActive)
        {
            row.Add(
                HasActive(gain)
                    ? new TooltipText("-", TextColour.Dim, Change(gain, gain.Active))
                    : new TooltipText("", TextColour.Dim));
        }

        if (showRequired)
        {
            row.Add(new TooltipText("", TextColour.Dim));
        }

        return row;
    }

    private static TooltipChange? Change(ItemStat stat, int difference) =>
        difference == 0
            ? null
            : new TooltipChange(Value(stat, difference), difference > 0 ? TextColour.Green : TextColour.Red);

    private static bool HasActive(ItemStat stat) => stat.Kind is ItemStatKind.Plain or ItemStatKind.Percent;

    private static bool ShowsActive(ItemStat stat) => HasActive(stat) && stat.Active != stat.Passive;

    private static bool ShowsRequired(ItemStat stat) => stat.Required is not (null or 0);

    private static string Value(ItemStat stat, int value)
    {
        if (stat.Kind != ItemStatKind.Percent)
        {
            return Signed(value);
        }

        return value % TenthsPerPercent == 0
            ? Signed(value / TenthsPerPercent) + "%"
            : (value / (double)TenthsPerPercent).ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + "%";
    }

    private static string? Special(ItemStat stat) => stat.Kind switch
    {
        ItemStatKind.Regen => $"{Signed(stat.Passive)} ({Signed(stat.Active)}/s)",
        ItemStatKind.Duration => $"{Whole(stat.Passive)} seconds",
        ItemStatKind.Single => Whole(stat.Passive),
        ItemStatKind.Rank => $"{Whole(stat.Passive)} ({stat.RankName})",
        _ => null,
    };

    private static IEnumerable<TooltipPart> Badges(ItemDetails details)
    {
        var has = AppliedBadges.Where(entry => details.ModsApplied.HasFlag(entry.Mod)).Select(entry => entry.Badge);
        var can = PossibleBadges.Where(entry => details.ModsPossible.HasFlag(entry.Mod)).Select(entry => entry.Badge);
        var cannot = ImpossibleBadges.Where(entry => details.ModsPossible.HasFlag(entry.Mod))
            .Select(entry => entry.Badge);
        TooltipBadges[] rows =
        [
            new("Has:", has.ToList()),
            new("Can:", can.ToList()),
            new("Cannot:", cannot.ToList()),
        ];
        return rows.Where(row => row.Badges.Count > 0);
    }

    private static IEnumerable<TooltipPart> Footer(ItemDetails details)
    {
        if (details.Level is { } level)
        {
            yield return Line($"Level {Whole(level)} - {Whole(details.KillsToLevel)} kills to next", TextColour.Green);
        }

        if (details.Blessings is { } blessings)
        {
            var plural = blessings == 1 ? "" : "s";
            var text = $"{Whole(blessings)} blessing{plural} left - next in {Whole(details.KillsToBlessing)} kills";
            yield return Line(text, TextColour.Green);
        }

        if (details.Condition is { } condition)
        {
            var colour = condition >= WorstCondition ? TextColour.Red : TextColour.Yellow;
            yield return Line(ConditionLabels[condition], colour);
        }

        if (details.SellSilver is { } silver)
        {
            var price = $"Sells for about {Whole(silver / SilverPerGold)}G {Whole(silver % SilverPerGold)}S";
            yield return Line(price, TextColour.Lime);
        }
    }

    private static TooltipLine Line(string text, TextColour colour) => new(new TooltipText(text, colour));

    private static string Signed(int value) => value.ToString("+0;-0", CultureInfo.InvariantCulture);

    private static string Whole(int value) => value.ToString(CultureInfo.InvariantCulture);
}
