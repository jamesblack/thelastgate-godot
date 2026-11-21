using System.Diagnostics;
using TheLastGate.Items;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Items;

public static class TooltipParts
{
    public static void AssertEqual(IEnumerable<TooltipPart> actual, params TooltipPart[] expected) =>
        AssertThat(Describe(actual)).IsEqual(Describe(expected));

    public static void AssertEqual(TooltipPart actual, TooltipPart expected) =>
        AssertThat(Describe(actual)).IsEqual(Describe(expected));

    public static string Describe(IEnumerable<TooltipPart> parts) => string.Join("\n", parts.Select(Describe));

    public static string Describe(TooltipPart part) => part switch
    {
        TooltipLine line => $"{line.Text} wraps: {line.Wraps}",
        TooltipSeparator => "separator",
        TooltipTable table => $"table {Joined(table.Header)} rows {string.Join(" / ", table.Rows.Select(Joined))}",
        TooltipBadges badges => $"badges {badges.Label} {Joined(badges.Badges)}",
        _ => throw new UnreachableException(),
    };

    private static string Joined(IEnumerable<TooltipText> texts) => string.Join(" | ", texts);
}
