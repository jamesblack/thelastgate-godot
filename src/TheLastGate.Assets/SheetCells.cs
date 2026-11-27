using System;

namespace TheLastGate.Assets;

public static class SheetCells
{
    private const int AlphaChannel = 3;

    public static (int X, int Y) CellOrigin(int offset, int cellWidth, int cellHeight) =>
        (offset % CharacterSheet.Columns * cellWidth, offset / CharacterSheet.Columns * cellHeight);

    public static bool[] UsedCells(ReadOnlySpan<byte> sheet, int sheetWidth, int sheetHeight)
    {
        var cellWidth = sheetWidth / CharacterSheet.Columns;
        var cellHeight = sheetHeight / CharacterSheet.Rows;
        var used = new bool[CharacterSheet.Slots];
        for (var offset = 0; offset < used.Length; offset++)
        {
            var (cellX, cellY) = CellOrigin(offset, cellWidth, cellHeight);
            for (var row = 0; row < cellHeight && !used[offset]; row++)
            {
                var rowPixels = RowOf(sheet, sheetWidth, cellX, cellY + row, cellWidth);
                for (var alpha = AlphaChannel; alpha < rowPixels.Length; alpha += SpritePixels.Rgba8BytesPerPixel)
                {
                    if (rowPixels[alpha] != 0)
                    {
                        used[offset] = true;
                        break;
                    }
                }
            }
        }

        return used;
    }

    public static void Blit(
        Span<byte> sheet,
        int sheetWidth,
        int offset,
        int cellWidth,
        int cellHeight,
        ReadOnlySpan<byte> frame)
    {
        var (cellX, cellY) = CellOrigin(offset, cellWidth, cellHeight);
        var rowBytes = cellWidth * SpritePixels.Rgba8BytesPerPixel;
        for (var row = 0; row < cellHeight; row++)
        {
            frame.Slice(row * rowBytes, rowBytes)
                .CopyTo(sheet.Slice(PixelIndex(sheetWidth, cellX, cellY + row), rowBytes));
        }
    }

    public static byte[] Extract(ReadOnlySpan<byte> sheet, int sheetWidth, int offset, int cellWidth, int cellHeight)
    {
        var (cellX, cellY) = CellOrigin(offset, cellWidth, cellHeight);
        var rowBytes = cellWidth * SpritePixels.Rgba8BytesPerPixel;
        var frame = new byte[rowBytes * cellHeight];
        for (var row = 0; row < cellHeight; row++)
        {
            RowOf(sheet, sheetWidth, cellX, cellY + row, cellWidth).CopyTo(frame.AsSpan(row * rowBytes, rowBytes));
        }

        return frame;
    }

    private static ReadOnlySpan<byte> RowOf(ReadOnlySpan<byte> sheet, int sheetWidth, int x, int y, int width) =>
        sheet.Slice(PixelIndex(sheetWidth, x, y), width * SpritePixels.Rgba8BytesPerPixel);

    private static int PixelIndex(int sheetWidth, int x, int y) =>
        (y * sheetWidth + x) * SpritePixels.Rgba8BytesPerPixel;
}
