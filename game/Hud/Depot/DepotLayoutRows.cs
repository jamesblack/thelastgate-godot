using System.Collections.Generic;
using System.Linq;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Depot;

public sealed partial class DepotLayout
{
    public IReadOnlyList<CategoryRow> Rows(IReadOnlyCollection<int> expanded)
    {
        var rows = new List<CategoryRow>();
        AddList(0, 0, expanded, rows);
        return rows;
    }

    // A hidden character's category gives its place to what it holds, so the rest of its contents never vanish.
    private IEnumerable<(SavedCategory Shown, SavedCategory Holder)> ShownCategories(int parentId, int steps = 0)
    {
        foreach (var child in categories.Where(category => category.ParentId == parentId).ToList())
        {
            if (!IsHidden(child))
            {
                yield return (child, child);
            }
            else if (steps < MaxDepth)
            {
                foreach (var (inner, _) in ShownCategories(child.Id, steps + 1))
                {
                    yield return (inner, child);
                }
            }
        }
    }

    private IEnumerable<SavedPage> ShownPages(int parentId, int steps = 0)
    {
        var hiddenChildren = categories.Where(category => category.ParentId == parentId && IsHidden(category));
        if (steps < MaxDepth)
        {
            foreach (var hidden in hiddenChildren.ToList())
            {
                foreach (var lifted in ShownPages(hidden.Id, steps + 1))
                {
                    yield return lifted;
                }
            }
        }

        foreach (var page in pages.Where(page => page.ParentId == parentId && !IsHidden(page)).ToList())
        {
            yield return page;
        }
    }

    private void AddList(int parentId, int depth, IReadOnlyCollection<int> expanded, List<CategoryRow> rows)
    {
        var shownCategories = ShownCategories(parentId).Select(shown => shown.Shown).ToList();
        for (var index = 0; index < shownCategories.Count; index++)
        {
            var category = shownCategories[index];
            rows.Add(new CategoryRow(category.Id, true, depth, parentId, index));
            if (expanded.Contains(category.Id) && depth < MaxDepth)
            {
                AddList(category.Id, depth + 1, expanded, rows);
            }
        }

        var shownPages = ShownPages(parentId).ToList();
        for (var index = 0; index < shownPages.Count; index++)
        {
            rows.Add(new CategoryRow(PageId(shownPages[index]), false, depth, parentId, shownCategories.Count + index));
        }
    }
}
