using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Windows;

// DisplayIndex is the row's place in its parent's list, where the subcategories come first and then the notes.
public readonly record struct CategoryRow(int Id, bool IsCategory, int Depth, int ParentId, int DisplayIndex);

public static class CategoryRows
{
    private const float LineBand = 0.25f;
    private const int Last = -1;

    // The top and bottom of a row are the lines above and below it; the middle of a category drops into it.
    public static CategoryDropSlot? Slot(
        IReadOnlyList<CategoryRow> rows,
        CategoryDrag drag,
        int rowIndex,
        float fractionDown,
        int cursorLevel)
    {
        if (rowIndex < rows.Count && rows[rowIndex].IsCategory && fractionDown is >= LineBand and <= 1 - LineBand)
        {
            return new CategoryDropSlot(rows[rowIndex].Id, Last, true);
        }

        var lineAt = rowIndex < rows.Count && fractionDown > 0.5f ? rowIndex + 1 : rowIndex;
        CategoryRow? above = lineAt > 0 ? rows[lineAt - 1] : null;
        CategoryRow? below = lineAt < rows.Count ? rows[lineAt] : null;
        return LineSlots(rows, above, below)
            .Select(slot => (slot.Level, Slot: Fit(rows, drag, slot.ParentId, slot.DisplayIndex)))
            .Where(candidate => candidate.Slot != null)
            .OrderBy(candidate => Math.Abs(candidate.Level - cursorLevel))
            .ThenByDescending(candidate => candidate.Level)
            .Select(candidate => candidate.Slot)
            .FirstOrDefault();
    }

    // Under the last row of an expanded category the line could end that list or any list it sits in, so each depth
    // from the row below's up to the row above's is a slot; the first child of an expanded category has only one.
    private static IEnumerable<(int Level, int ParentId, int DisplayIndex)> LineSlots(
        IReadOnlyList<CategoryRow> rows,
        CategoryRow? above,
        CategoryRow? below)
    {
        if (below is { } next && (above == null || next.Depth > above.Value.Depth))
        {
            yield return (next.Depth, next.ParentId, next.DisplayIndex);
            yield break;
        }

        if (above is not { } last)
        {
            yield break;
        }

        var shallowest = below?.Depth ?? 0;
        if (below is { } lower)
        {
            yield return (lower.Depth, lower.ParentId, lower.DisplayIndex);
            shallowest++;
        }

        for (var level = last.Depth; level >= shallowest; level--)
        {
            var ancestor = AncestorAt(rows, last, level);
            yield return (level, ancestor.ParentId, ancestor.DisplayIndex + 1);
        }
    }

    private static CategoryRow AncestorAt(IReadOnlyList<CategoryRow> rows, CategoryRow row, int level)
    {
        var current = row;
        while (current.Depth > level)
        {
            var parentId = current.ParentId;
            current = rows.First(candidate => candidate.IsCategory && candidate.Id == parentId);
        }

        return current;
    }

    // Categories only land among the subcategories and notes only among the notes; an item moving down its own list
    // is counted without itself, as the move takes it out first.
    private static CategoryDropSlot? Fit(
        IReadOnlyList<CategoryRow> rows,
        CategoryDrag drag,
        int parentId,
        int displayIndex)
    {
        var subcategories = rows.Count(row => row.IsCategory && row.ParentId == parentId);
        var own = rows.Where(row => row.Id == drag.Id && row.IsCategory == drag.IsCategory && row.ParentId == parentId)
            .Select(row => (CategoryRow?)row)
            .FirstOrDefault();
        var index = drag.IsCategory ? displayIndex : displayIndex - subcategories;
        if (drag.IsCategory ? displayIndex > subcategories : parentId == 0 || index < 0)
        {
            return null;
        }

        if (own is { } itself && itself.DisplayIndex < displayIndex)
        {
            index--;
        }

        return new CategoryDropSlot(parentId, index, false);
    }
}
