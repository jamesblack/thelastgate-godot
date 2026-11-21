using System;
using TheLastGate.State;

namespace TheLastGate.Stats;

public sealed class SkillScroll
{
    public const int VisibleRows = 10;

    public int Row { get; private set; }

    public static int LastRow => Terminology.SkillCount - VisibleRows;

    public void ScrollBy(int rows) => ScrollTo(Row + rows);

    public void ScrollTo(int row) => Row = Math.Clamp(row, 0, LastRow);

    public int PositionShownIn(int visibleRow) => visibleRow + Row;
}
