using System.Collections.Generic;

namespace TheLastGate.Procedural;

public enum CellTone
{
    Normal,
    Dim,
    Positive,
    Negative,
    Zero,
}

public sealed record TableColumn(string Header, float Width = 1, string? Group = null);

public sealed record TableCell(string Text, CellTone Tone = CellTone.Normal);

public sealed record TableRow(IReadOnlyList<TableCell> Cells);
