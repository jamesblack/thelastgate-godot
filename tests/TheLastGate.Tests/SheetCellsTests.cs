using TheLastGate.Assets;

namespace TheLastGate.Tests;

public class SheetCellsTests
{
    private const int BytesPerPixel = 4;
    private const int SmallCell = 2;
    private const int SmallSheetWidth = CharacterSheet.Columns * SmallCell;
    private const int SmallSheetHeight = CharacterSheet.Rows * SmallCell;

    [Theory]
    [InlineData(0, 64, 64, 0, 0)]
    [InlineData(33, 64, 64, 64, 64)]
    [InlineData(447, 32, 64, 31 * 32, 13 * 64)]
    public void CellOrigin_IsRowMajorOver32Columns(
        int offset,
        int cellWidth,
        int cellHeight,
        int expectedX,
        int expectedY)
    {
        Assert.Equal((expectedX, expectedY), SheetCells.CellOrigin(offset, cellWidth, cellHeight));
    }

    [Fact]
    public void UsedCells_MarksCellsWithAnyVisiblePixel()
    {
        var sheet = new byte[SmallSheetWidth * SmallSheetHeight * BytesPerPixel];
        var (cellX, cellY) = SheetCells.CellOrigin(33, SmallCell, SmallCell);
        sheet[((cellY + 1) * SmallSheetWidth + cellX + 1) * BytesPerPixel + 3] = 1;

        var used = SheetCells.UsedCells(sheet, SmallSheetWidth, SmallSheetHeight);

        Assert.Equal(CharacterSheet.Slots, used.Length);
        Assert.Equal([33], Enumerable.Range(0, used.Length).Where(offset => used[offset]));
    }

    [Fact]
    public void BlitThenExtract_RoundTripsTheFrame()
    {
        var sheet = new byte[SmallSheetWidth * SmallSheetHeight * BytesPerPixel];
        var frame = Enumerable.Range(1, SmallCell * SmallCell * BytesPerPixel).Select(value => (byte)value).ToArray();

        SheetCells.Blit(sheet, SmallSheetWidth, 40, SmallCell, SmallCell, frame);

        Assert.Equal(frame, SheetCells.Extract(sheet, SmallSheetWidth, 40, SmallCell, SmallCell));
        Assert.All(
            SheetCells.Extract(sheet, SmallSheetWidth, 39, SmallCell, SmallCell),
            value => Assert.Equal(0, value));
        Assert.All(
            SheetCells.Extract(sheet, SmallSheetWidth, 41, SmallCell, SmallCell),
            value => Assert.Equal(0, value));
        Assert.Equal(
            [40],
            Enumerable.Range(0, CharacterSheet.Slots)
                .Where(offset => SheetCells.UsedCells(sheet, SmallSheetWidth, SmallSheetHeight)[offset]));
    }
}
