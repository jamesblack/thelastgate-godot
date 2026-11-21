using TheLastGate.Cursors;

namespace TheLastGate.World;

public readonly record struct MapAction(
    int HighlightedTile,
    MapLayer HighlightedLayer,
    MouseCursor Cursor,
    byte[]? Command,
    int? NewSelection)
{
    public const int NoTile = -1;

    // The world tile a click queues a use of.
    public (int X, int Y)? QueuedUse { get; init; }

    // The give picks up another item like the one given once the hand is empty.
    public bool GivesMore { get; init; }

    public static MapAction Nothing { get; } = new(NoTile, MapLayer.Floor, MouseCursor.None, null, null);
}
