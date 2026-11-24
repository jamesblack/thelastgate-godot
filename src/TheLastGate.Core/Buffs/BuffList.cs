using System;
using TheLastGate.State;

namespace TheLastGate.Buffs;

// The server's reply to "#buffs": a header line, a blank line, a "<Name> power of N ..." line for each buff (characters
// with spell knowledge also get " : +N <stat>" lines under each), and a closing blank line.
public static class BuffList
{
    public const string Header = "Your active buffs and debuffs:";
    public const int ProtectSprite = 86;
    public const int EnhanceSprite = 87;
    public const int BlessSprite = 88;

    // In BuffKind order. The sensed messages follow the caster's name.
    private static readonly string[] SelfCastMessages =
        ["You have been blessed.", "Your weapon feels stronger.", "You feel protected."];

    private static readonly string[] SensedCastMessages =
        [" cast bless on you.", " cast enhance weapon on you.", " cast protect on you."];

    public static BuffKind? SpriteKind(int sprite) => sprite switch
    {
        BlessSprite => BuffKind.Bless,
        EnhanceSprite => BuffKind.Enhance,
        ProtectSprite => BuffKind.Protect,
        _ => null,
    };

    public static bool IsHeader(string text) => text == Header;

    public static BuffKind? CastKind(string text)
    {
        foreach (var kind in Enum.GetValues<BuffKind>())
        {
            if (text == SelfCastMessages[(int)kind]
                || text.EndsWith(SensedCastMessages[(int)kind], StringComparison.Ordinal))
            {
                return kind;
            }
        }

        return null;
    }

    // An equal slot isn't a recast: the server resends slots unchanged, so a recast still at 15/16 shows only in the chat.
    // A buff going needs nothing, as its reading is only shown while its icon is.
    public static bool SlotNeedsFetch(int oldSprite, int oldActive, int newSprite, int newActive) =>
        SpriteKind(newSprite) != null && (oldSprite != newSprite || newActive > oldActive);
}

// Reads one reply line at a time; only Bless, Enhance and Protect are kept.
public sealed class BuffListParser
{
    private const string PowerMarker = " power of ";
    private const string BonusPrefix = " : ";
    private int blankLines;

    private BuffKind? currentKind;

    public BuffReading[] Readings { get; } = [new(), new(), new()];

    // True while the reply goes on; false once its closing blank line has been read.
    public bool Feed(string text)
    {
        if (text.Trim(' ').Length == 0)
        {
            blankLines++;
            return blankLines < 2;
        }

        if (!ReadPowerLine(text))
        {
            ReadBonusLine(text);
        }

        return true;
    }

    // The value whose name the text is exactly; a number isn't a name.
    private static T? Named<T>(string text)
        where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
        {
            if (value.ToString() == text)
            {
                return value;
            }
        }

        return null;
    }

    // Reads an optional sign and digits after any spaces, as C's strtol does.
    private static bool ReadNumber(string text, ref int position, out int value)
    {
        var start = position;
        while (start < text.Length && text[start] == ' ')
        {
            start++;
        }

        var end = start;
        if (end < text.Length && text[end] is '+' or '-')
        {
            end++;
        }

        var digitsStart = end;
        while (end < text.Length && char.IsAsciiDigit(text[end]))
        {
            end++;
        }

        if (end == digitsStart || !int.TryParse(text.AsSpan(start, end - start), out value))
        {
            value = 0;
            return false;
        }

        position = end;
        return true;
    }

    // A power line sets the buff the bonus lines under it belong to, or none for a buff that isn't kept.
    private bool ReadPowerLine(string text)
    {
        var marker = text.IndexOf(PowerMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        currentKind = Named<BuffKind>(text[..marker]);
        if (currentKind is { } kept)
        {
            var reading = new BuffReading { Known = true };
            var position = marker + PowerMarker.Length;
            ReadNumber(text, ref position, out var power);
            reading.Power = power;
            Readings[(int)kept] = reading;
        }

        return true;
    }

    private void ReadBonusLine(string text)
    {
        if (currentKind is not { } kind || !text.StartsWith(BonusPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var position = BonusPrefix.Length;
        if (!ReadNumber(text, ref position, out var amount) || position >= text.Length || text[position] != ' ')
        {
            return;
        }

        var statName = text[(position + 1)..];
        var reading = Readings[(int)kind];
        if (Named<AttributeSlot>(statName) is { } attribute)
        {
            reading.AttributeBonus[attribute] = amount;
            reading.HasExact = true;
        }
        else if (statName == "Weapon Value")
        {
            reading.WeaponBonus = amount;
            reading.HasExact = true;
        }
        else if (statName == "Armor Value")
        {
            reading.ArmorBonus = amount;
            reading.HasExact = true;
        }
    }
}
