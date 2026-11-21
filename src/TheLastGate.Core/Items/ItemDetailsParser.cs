using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.State;

namespace TheLastGate.Items;

public static class ItemDetailsParser
{
    private const string ContractOpening = "A contract. It reads:";
    private const string ContractName = "Contract";
    private const string ComparisonOpening = "You compare it with ";
    private const string TableOpening = "Stat ";
    private const string SoulstoneColumn = "Adds";
    private const int FirstBodyLine = 2;
    private const int ComparisonLines = 2;
    private const int StatNameWidth = 16;
    private const int MaxStats = 32;
    private const int MaxLineReferences = 32;
    private const int RankNameSize = 24;
    private const int MaxPlainValues = 3;
    private const int SilverPerGold = 100;

    private static readonly (string Text, ItemModsApplied Mod)[] AppliedLines =
    [
        ("Has been Whetstoned.", ItemModsApplied.Whetstoned),
        ("Has been Soulstoned.", ItemModsApplied.Soulstoned),
        ("Has been Catalyzed.", ItemModsApplied.Catalyzed),
        ("Has been Enchanted.", ItemModsApplied.Enchanted),
        ("Has been Focused.", ItemModsApplied.Focused),
        ("Has been Augmented.", ItemModsApplied.Augmented),
        ("Has been made easier to use.", ItemModsApplied.Easier),
        ("Has been Corrupted.", ItemModsApplied.Corrupted),
        ("Legacy item.", ItemModsApplied.Legacy),
    ];

    private static readonly (string Text, ItemModsPossible Mod)[] PossibleLines =
    [
        ("Can be Whetstoned.", ItemModsPossible.CanWhetstone),
        ("Can be Soulstoned.", ItemModsPossible.CanSoulstone),
        ("Cannot be Soulstoned.", ItemModsPossible.CannotSoulstone),
        ("Can be Enchanted.", ItemModsPossible.CanEnchant),
        ("Cannot be Enchanted.", ItemModsPossible.CannotEnchant),
        ("May be further enhanced by Osiris.", ItemModsPossible.CanOsiris),
    ];

    private static readonly string[] ConditionLines =
    [
        "It's in perfect condition.", "It's showing signs of age.", "It's fairly old.", "It is old.",
        "It is very old and battered.",
    ];

    public static bool IsReplyStart(string text, int font) =>
        text == ContractOpening || (font == (int)LogFont.Lime && text.Length >= 2 && text[^1] == ':');

    public static ItemDetails Parse(IReadOnlyList<LogLine> reply)
    {
        if (reply.Count == 0)
        {
            return new ItemDetails();
        }

        var reading = new Reading();
        for (var index = FirstBodyLine; index < reply.Count; index++)
        {
            var line = reply[index];
            if (reading.SkipsComparison(line.Text) || new TextCursor(line.Text).IsBlank())
            {
                continue;
            }

            if (!reading.ReadTableLine(line) && !reading.ReadKnownLine(line.Text))
            {
                reading.KeepUnrecognised(index);
            }
        }

        var header = reply[0].Text == ContractOpening ? ContractName : reply[0].Text;
        return reading.Details(header.EndsWith(':') ? header[..^1] : header, reply.Count > 1 ? 1 : null);
    }

    private static ItemStat? ReadStat(string text, bool soulstoneTable)
    {
        if (text.Length <= StatNameWidth)
        {
            return null;
        }

        var name = text[..StatNameWidth].TrimEnd(' ');
        return name.Length > 0 && ReadStatValues(text[StatNameWidth..], soulstoneTable) is { } values
            ? values with { Name = name }
            : null;
    }

    private static ItemStat? ReadStatValues(string values, bool soulstoneTable)
    {
        var cursor = new TextCursor(values);
        if (values.Contains("seconds", StringComparison.Ordinal))
        {
            return cursor.ReadInt(out var seconds) && cursor.Expect("seconds") && cursor.IsBlank()
                ? new ItemStat("", ItemStatKind.Duration, seconds)
                : null;
        }

        if (values.Contains("/s)", StringComparison.Ordinal))
        {
            return cursor.ReadInt(out var total) && cursor.Expect("(") && cursor.ReadInt(out var perSecond)
                && cursor.Expect("/s)") && cursor.IsBlank()
                    ? new ItemStat("", ItemStatKind.Regen, total, perSecond)
                    : null;
        }

        if (values.Contains('('))
        {
            return cursor.ReadInt(out var rank) && cursor.Expect("(")
                && cursor.ReadUntil(')', RankNameSize, out var rankName) && cursor.IsBlank()
                    ? new ItemStat("", ItemStatKind.Rank, rank, RankName: rankName)
                    : null;
        }

        if (values.Contains('%'))
        {
            return cursor.ReadPercent(out var passive) && cursor.ReadPercent(out var active) && cursor.IsBlank()
                ? new ItemStat("", ItemStatKind.Percent, passive, active)
                : null;
        }

        var numbers = new List<int>();
        while (numbers.Count < MaxPlainValues && cursor.ReadInt(out var number))
        {
            numbers.Add(number);
        }

        if (!cursor.IsBlank())
        {
            return null;
        }

        return numbers.Count switch
        {
            0 => null,
            1 => new ItemStat("", soulstoneTable ? ItemStatKind.Soulstone : ItemStatKind.Single, numbers[0]),
            2 => new ItemStat("", ItemStatKind.Plain, numbers[0], numbers[1]),
            _ => new ItemStat("", ItemStatKind.Plain, numbers[0], numbers[1], numbers[2]),
        };
    }

    private sealed class Reading
    {
        private readonly List<int> effectLines = [];
        private readonly List<int> otherLines = [];
        private readonly List<ItemStat> stats = [];
        private int? blessings;
        private int? condition;
        private bool inTable;
        private int killsToBlessing;
        private int killsToLevel;
        private int? level;
        private ItemModsApplied modsApplied;
        private ItemModsPossible modsPossible;
        private bool seenTable;
        private int? sellSilver;
        private int skipComparison;
        private bool soulstoneTable;

        public bool SkipsComparison(string text)
        {
            if (text.StartsWith(ComparisonOpening, StringComparison.Ordinal))
            {
                skipComparison = ComparisonLines;
                return true;
            }

            if (skipComparison == 0)
            {
                return false;
            }

            skipComparison--;
            return true;
        }

        public bool ReadTableLine(LogLine line)
        {
            if (line.Font == (int)LogFont.Orange && line.Text.StartsWith(TableOpening, StringComparison.Ordinal))
            {
                inTable = true;
                seenTable = true;
                soulstoneTable = line.Text.Contains(SoulstoneColumn, StringComparison.Ordinal);
                return true;
            }

            if (!inTable)
            {
                return false;
            }

            if (stats.Count < MaxStats && ReadStat(line.Text, soulstoneTable) is { } stat)
            {
                stats.Add(stat);
                return true;
            }

            inTable = false;
            return false;
        }

        public bool ReadKnownLine(string text)
        {
            var applied = AppliedLines.FirstOrDefault(entry => entry.Text == text);
            if (applied.Text != null)
            {
                modsApplied |= applied.Mod;
                return true;
            }

            var possible = PossibleLines.FirstOrDefault(entry => entry.Text == text);
            if (possible.Text != null)
            {
                modsPossible |= possible.Mod;
                return true;
            }

            var conditionIndex = Array.IndexOf(ConditionLines, text);
            if (conditionIndex >= 0)
            {
                condition = conditionIndex;
                return true;
            }

            return ReadLevel(text) || ReadBlessings(text) || ReadSellPrice(text);
        }

        public void KeepUnrecognised(int index)
        {
            var lines = seenTable ? otherLines : effectLines;
            if (lines.Count < MaxLineReferences)
            {
                lines.Add(index);
            }
        }

        public ItemDetails Details(string name, int? descriptionLine) => new()
        {
            Name = name,
            DescriptionLine = descriptionLine,
            SoulstoneTable = soulstoneTable,
            Stats = stats,
            ModsApplied = modsApplied,
            ModsPossible = modsPossible,
            Condition = condition,
            Level = level,
            KillsToLevel = killsToLevel,
            Blessings = blessings,
            KillsToBlessing = killsToBlessing,
            SellSilver = sellSilver,
            EffectLines = effectLines,
            OtherLines = otherLines,
        };

        // "Level 3. Next level in 120 kills."
        private bool ReadLevel(string text)
        {
            var cursor = new TextCursor(text);
            if (!cursor.Expect("Level ") || !cursor.ReadInt(out var readLevel) || !cursor.Expect(". Next level in ")
                || !cursor.ReadInt(out var kills) || !cursor.Expect(" kills.") || !cursor.AtEnd)
            {
                return false;
            }

            level = readLevel;
            killsToLevel = kills;
            return true;
        }

        // "2 more blessings. Next in 40 kills." or "1 more blessing. Next in 40 kills."
        private bool ReadBlessings(string text)
        {
            var cursor = new TextCursor(text);
            if (!cursor.ReadInt(out var count) || !cursor.Expect(" more blessing"))
            {
                return false;
            }

            cursor.Skip('s');
            if (!cursor.Expect(". Next in ") || !cursor.ReadInt(out var kills) || !cursor.Expect(" kills.")
                || !cursor.AtEnd)
            {
                return false;
            }

            blessings = count;
            killsToBlessing = kills;
            return true;
        }

        // "You estimate this could sell for 12G 34S."
        private bool ReadSellPrice(string text)
        {
            var cursor = new TextCursor(text);
            if (!cursor.Expect("You estimate this could sell for ") || !cursor.ReadInt(out var gold)
                || !cursor.Expect("G") || !cursor.ReadInt(out var silver) || !cursor.Expect("S.") || !cursor.AtEnd)
            {
                return false;
            }

            sellSilver = gold * SilverPerGold + silver;
            return true;
        }
    }
}
