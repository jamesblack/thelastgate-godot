namespace TheLastGate.World;

public static class NameplateText
{
    private const int CentringWidthPerLetter = 5;

    public static bool IsShown(bool showNames, bool showPercent) => showNames || showPercent;

    public static string For(string name, int characterId, int healthPercent, bool showNames, bool showPercent)
    {
        if (characterId == 0)
        {
            return name;
        }

        return (showNames, showPercent) switch
        {
            (true, true) => healthPercent != 0 ? $"{name} {healthPercent}%" : name,
            (true, false) => name,
            (false, true) => healthPercent != 0 ? $"{healthPercent}%" : "",
            _ => "",
        };
    }

    public static int FontFor(int textColour) => textColour switch
    {
        0 => 1,
        1 => 0,
        _ => textColour,
    };

    // The current client centres at five pixels a letter, although letters are six apart.
    public static int LeftOffset(string text) => -(text.Length * CentringWidthPerLetter / 2);
}
