using TheLastGate.State;

namespace TheLastGate.World;

public static class MapPicking
{
    private const int TileSize = IsometricProjection.TileSize;
    private const int MouseShiftX = 176 - 16;
    private const int MouseShiftY = 8;
    private const int ViewSize = 48;
    private const int EdgeMargin = 3;
    private const int FirstTile = (WorldMap.Size - ViewSize) / 2 + EdgeMargin;
    private const int LastTile = WorldMap.Size - FirstTile;
    private const int LowestPosition = FirstTile * TileSize + 12;
    private const int HighestPosition = LastTile * TileSize + 20;
    private const float TileCentre = 0.5f;

    private static readonly (int Columns, int Rows)[][] SearchRings =
    [
        [(0, 0)],
        [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)],
        [
            (-2, -2), (-1, -2), (0, -2), (1, -2), (2, -2), (-2, -1), (2, -1), (-2, 0), (2, 0), (-2, 1), (2, 1),
            (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2),
        ],
    ];

    public static (float Column, float Row)? PointAt(
        int screenX,
        int screenY,
        int walkOffsetX = 0,
        int walkOffsetY = 0)
    {
        var x = screenX + walkOffsetX + MouseShiftX;
        var y = screenY + walkOffsetY + MouseShiftY;
        var columnPosition = 2 * y + x - 2 * IsometricProjection.ScreenOriginY - IsometricProjection.ScreenOriginX
            + IsometricProjection.MapCentringShift;
        var rowPosition = x - 2 * y + 2 * IsometricProjection.ScreenOriginY - IsometricProjection.ScreenOriginX
            + IsometricProjection.MapCentringShift;
        if (columnPosition is < LowestPosition or > HighestPosition
            || rowPosition is < LowestPosition or > HighestPosition)
        {
            return null;
        }

        var point = (Column: columnPosition / (float)TileSize, Row: rowPosition / (float)TileSize);
        var (column, row) = ((int)point.Column, (int)point.Row);
        return column is >= FirstTile and <= LastTile && row is >= FirstTile and <= LastTile ? point : null;
    }

    public static (int Column, int Row)? TileAt(int screenX, int screenY, int walkOffsetX = 0, int walkOffsetY = 0) =>
        PointAt(screenX, screenY, walkOffsetX, walkOffsetY) is var (column, row) ? ((int)column, (int)row) : null;

    // Searches outwards a ring at a time and stops at the first ring with a match, taking its tile closest to the
    // mouse; a tie goes to the tile searched first.
    public static (int Column, int Row)? NearestTile(WorldMap map, MapFlags wanted, (float Column, float Row) point)
    {
        var (centreColumn, centreRow) = ((int)point.Column, (int)point.Row);
        foreach (var ring in SearchRings)
        {
            (int Column, int Row)? nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var (columns, rows) in ring)
            {
                var (column, row) = (centreColumn + columns, centreRow + rows);
                if (!((MapFlags)map.Tiles[column + row * WorldMap.Size].Flags).HasFlag(wanted))
                {
                    continue;
                }

                var acrossDistance = column + TileCentre - point.Column;
                var downDistance = row + TileCentre - point.Row;
                var distance = acrossDistance * acrossDistance + downDistance * downDistance;
                if (nearest == null || distance < nearestDistance)
                {
                    nearest = (column, row);
                    nearestDistance = distance;
                }
            }

            if (nearest != null)
            {
                return nearest;
            }
        }

        return null;
    }
}
