using System.Collections.Generic;
using System.Linq;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Depot;

public sealed class DepotListSource(DepotLayout layout) : ICategoryListSource
{
    private readonly HashSet<int> expandedIds = [];
    private int openPageId;
    private int pathShownPageId;

    public int Revision => layout.Revision;

    public IReadOnlyCollection<int> Expanded => expandedIds;

    public bool CanDrag => true;

    public IReadOnlyList<CategoryRow> BuildRows() => layout.Rows(expandedIds);

    public CategoryRowLook Look(CategoryRow row) =>
        row.IsCategory
            ? new CategoryRowLook(layout.CategoryText(row.Id), null, CategoryRowState.Normal)
            : new CategoryRowLook(
                layout.PageText(row.Id),
                null,
                row.Id == openPageId ? CategoryRowState.Current : CategoryRowState.Normal,
                layout.PageOwner(row.Id));

    public void Expand(int categoryId, bool expanded)
    {
        if (expanded)
        {
            expandedIds.Add(categoryId);
        }
        else
        {
            expandedIds.Remove(categoryId);
        }
    }

    public bool CanDrop(CategoryDrag drag, CategoryDropSlot slot) => layout.CanDrop(drag, slot);

    public bool Drop(CategoryDrag drag, CategoryDropSlot slot) => layout.Drop(drag, slot);

    public void ShowOpen(int characterNumber, int page)
    {
        openPageId = DepotLayout.PageId(characterNumber, page);
        var home = layout.CategoryOf(characterNumber);
        if (openPageId == pathShownPageId || home == 0)
        {
            return;
        }

        pathShownPageId = openPageId;
        ExpandWithAncestors(home);
        var saved = layout.Pages.FirstOrDefault(candidate =>
            candidate.CharacterNumber == characterNumber && candidate.Page == page);
        if (saved != null)
        {
            ExpandWithAncestors(saved.ParentId);
        }
    }

    private void ExpandWithAncestors(int categoryId)
    {
        var current = categoryId;
        for (var step = 0; step <= DepotLayout.MaxDepth && current != 0; step++)
        {
            expandedIds.Add(current);
            current = layout.Categories.FirstOrDefault(category => category.Id == current)?.ParentId ?? 0;
        }
    }
}
