using TheLastGate.Assets;

namespace TheLastGate.Stats;

public readonly record struct StatLine(
    string Name,
    TextColour Colour,
    int? Base,
    int? Current,
    bool CanRaise,
    int? Cost,
    bool CanLower = false)
{
    public static StatLine Unknown { get; } = new("-", TextColour.Yellow, null, null, false, null);
}
