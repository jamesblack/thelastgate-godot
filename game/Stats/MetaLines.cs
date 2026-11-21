using System.Collections.Generic;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Stats;

public readonly record struct MetaLine(string Name, TextColour Colour, string Value, string Affix);

public static class MetaLines
{
    private const int NameWidth = 20;
    private const int AffixWidth = 7;

    private static readonly HashSet<string> Headings = ["  Passive Stats:", "  Active Stats:"];

    public static MetaLine? For(MetaStat stat) =>
        stat.Show == 0
            ? null
            : new MetaLine(
                Clip(stat.Name, NameWidth),
                Colour(stat.Font),
                MetaStat.ValueText(stat.Value, stat.Flag != 0),
                Clip(stat.Affix, AffixWidth));

    public static TextColour Colour(int font) =>
        font is >= (int)TextColour.Red and <= (int)TextColour.White ? (TextColour)font : TextColour.Yellow;

    public static IReadOnlyList<LogLine> Description(string name, string description) =>
        Headings.Contains(name)
            ? []
            : [new LogLine(name, (int)TextColour.Orange), new LogLine(description, (int)TextColour.Yellow)];

    private static string Clip(string text, int width) => text.Length > width ? text[..width] : text;
}
