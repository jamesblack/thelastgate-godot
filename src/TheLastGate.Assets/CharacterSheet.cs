using System.Globalization;
using System.Text.RegularExpressions;

namespace TheLastGate.Assets;

public sealed partial record CharacterSheet(int BaseSprite, string Name)
{
    public const int Columns = 32;
    public const int Rows = 14;
    public const int Slots = Columns * Rows;

    public string FileName => BaseSprite.ToString("D5", CultureInfo.InvariantCulture) + "-" + Name + ".png";

    public static CharacterSheet? FromFileName(string fileName)
    {
        var match = SheetFileName().Match(fileName);
        return match.Success
            ? new CharacterSheet(
                int.Parse(match.Groups["base"].Value, CultureInfo.InvariantCulture),
                match.Groups["name"].Value)
            : null;
    }

    public bool TryGetOffset(int spriteNumber, out int offset)
    {
        offset = spriteNumber - BaseSprite;
        return offset is >= 0 and < Slots;
    }

    [GeneratedRegex(@"^(?<base>\d{5})-(?<name>[a-z0-9]+(?:-[a-z0-9]+)*)\.[pP][nN][gG]$")]
    private static partial Regex SheetFileName();
}
