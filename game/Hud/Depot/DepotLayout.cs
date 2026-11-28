using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Net;
using TheLastGate.Notes;
using TheLastGate.Windows;

namespace TheLastGate.Hud.Depot;

public sealed partial class DepotLayout
{
    public const int MaxDepth = NoteLimits.Depth;

    private readonly List<SavedCategory> categories = [];
    private readonly List<SavedPage> pages = [];
    private Dictionary<int, string> listedNames = [];

    public int Revision { get; private set; }

    public bool Changed { get; set; }

    public IReadOnlyList<SavedCategory> Categories => categories;

    public IReadOnlyList<SavedPage> Pages => pages;

    public int NextCategoryId { get; private set; } = 1;

    public static int PageId(int characterNumber, int page) => characterNumber * DepotPackets.Pages + page + 1;

    public static (int CharacterNumber, int Page) PageOf(int pageId) =>
        ((pageId - 1) / DepotPackets.Pages, (pageId - 1) % DepotPackets.Pages);

    public static DepotLayout FromSaved(
        IReadOnlyList<SavedCategory> categories,
        IReadOnlyList<SavedPage> pages,
        int nextCategoryId)
    {
        var layout = new DepotLayout();
        layout.categories.AddRange(categories);
        layout.pages.AddRange(pages);
        layout.NextCategoryId = Math.Max(
            nextCategoryId,
            categories.Select(category => category.Id + 1).DefaultIfEmpty(1).Max());
        return layout;
    }

    public void Sync(IReadOnlyList<DepotCharacter> listed)
    {
        var names = new Dictionary<int, string>();
        foreach (var character in listed)
        {
            names[character.Number] = character.Name;
        }

        var shownChanged = !names.Keys.ToHashSet().SetEquals(listedNames.Keys);
        listedNames = names;
        var savedChanged = false;
        foreach (var (number, name) in names)
        {
            savedChanged |= KeepCharacter(number, name);
        }

        if (savedChanged)
        {
            Touch();
        }
        else if (shownChanged)
        {
            Revision++;
        }
    }

    public int CategoryOf(int characterNumber) =>
        categories.FirstOrDefault(category => IsCharacter(category) && category.CharacterNumber == characterNumber)?.Id
        ?? 0;

    public bool IsCharacterCategory(int categoryId) =>
        FindCategory(categoryId) is { } category && IsCharacter(category);

    public int CharacterOf(int categoryId) => FindCategory(categoryId)?.CharacterNumber ?? 0;

    public string CategoryText(int categoryId) => FindCategory(categoryId)?.Name ?? "";

    public string PageText(int pageId)
    {
        if (FindPage(pageId) is not { } page)
        {
            return "";
        }

        return page.Name.Length > 0 ? page.Name : $"Page {page.Page + 1}";
    }

    public string PageOwner(int pageId) =>
        FindPage(pageId) is { } page && page.ParentId != CategoryOf(page.CharacterNumber)
            ? OwnerName(page.CharacterNumber)
            : "";

    public string PageName(int pageId) => FindPage(pageId)?.Name ?? "";

    public int Depth(int categoryId)
    {
        var depth = 0;
        for (var current = FindCategory(categoryId);
            current != null && current.ParentId != 0 && depth <= MaxDepth;
            current = FindCategory(current.ParentId))
        {
            depth++;
        }

        return depth;
    }

    public bool CanDrop(CategoryDrag drag, CategoryDropSlot slot)
    {
        var target = slot.ParentId;
        var targetShown = target == 0 || (FindCategory(target) is { } parent && !IsHidden(parent));
        if (drag.IsCategory)
        {
            return FindCategory(drag.Id) is { } category
                && !IsHidden(category)
                && targetShown
                && !(slot.Onto && category.ParentId == target)
                && !IsWithin(target, category.Id)
                && ChildDepth(target) + Height(category.Id) <= MaxDepth;
        }

        return FindPage(drag.Id) is { } page
            && !IsHidden(page)
            && target != 0
            && targetShown
            && !(slot.Onto && page.ParentId == target);
    }

    public bool Drop(CategoryDrag drag, CategoryDropSlot slot)
    {
        if (!CanDrop(drag, slot))
        {
            return false;
        }

        var index = slot.Onto ? -1 : slot.Index;
        if (drag.IsCategory)
        {
            MoveCategory(FindCategory(drag.Id)!, slot.ParentId, index);
        }
        else
        {
            MovePage(FindPage(drag.Id)!, slot.ParentId, index);
        }

        Touch();
        return true;
    }

    public int CreateCategory(string name, int parentId)
    {
        if (parentId != 0 && (FindCategory(parentId) == null || ChildDepth(parentId) + 1 > MaxDepth))
        {
            return 0;
        }

        var category = new SavedCategory(NextCategoryId++, parentId, 0, name.Trim());
        categories.Add(category);
        Touch();
        return category.Id;
    }

    public bool RenameCategory(int categoryId, string name)
    {
        var trimmed = name.Trim();
        if (FindCategory(categoryId) is not { } category || IsCharacter(category) || trimmed.Length == 0)
        {
            return false;
        }

        Replace(category, category with { Name = trimmed });
        return true;
    }

    public void RenamePage(int pageId, string name)
    {
        var trimmed = name.Trim();
        if (FindPage(pageId) is { } page && page.Name != trimmed)
        {
            Replace(page, page with { Name = trimmed });
        }
    }

    public void ResetPage(int pageId)
    {
        if (FindPage(pageId) is not { } page)
        {
            return;
        }

        pages.Remove(page);
        pages.Add(page with { ParentId = HomeOf(page), Name = "" });
        Touch();
    }

    public (int Pages, int Categories) DeleteSummary(int categoryId)
    {
        if (FindCategory(categoryId) is not { } category || IsCharacter(category))
        {
            return (0, 0);
        }

        var beneath = CategoriesWithin(categoryId);
        return (PagesWithin(beneath).Count(page => !IsUntouched(page)),
            beneath.Count(inner => inner.Id != categoryId && !IsCharacter(inner)));
    }

    public void DeleteCategory(int categoryId)
    {
        if (FindCategory(categoryId) is not { } deleted || IsCharacter(deleted))
        {
            return;
        }

        var beneath = CategoriesWithin(categoryId);
        var pagesBeneath = PagesWithin(beneath);
        var lifted = beneath.Where(IsCharacter).ToList();
        categories.RemoveAll(category => category.Id != categoryId && beneath.Any(inner => inner.Id == category.Id));
        categories.InsertRange(
            categories.IndexOf(deleted),
            lifted.Select(character => character with { ParentId = deleted.ParentId }));
        categories.Remove(deleted);
        foreach (var page in pagesBeneath)
        {
            var home = HomeOf(page);
            if (page.ParentId == home)
            {
                Replace(page, page with { Name = "" });
                continue;
            }

            pages.Remove(page);
            pages.Add(page with { ParentId = home, Name = "" });
        }

        Touch();
    }

    private static bool IsCharacter(SavedCategory category) => category.CharacterNumber != 0;

    private static int PageId(SavedPage page) => PageId(page.CharacterNumber, page.Page);

    private bool KeepCharacter(int number, string name)
    {
        var changed = false;
        var categoryId = CategoryOf(number);
        if (categoryId == 0)
        {
            categoryId = NextCategoryId++;
            categories.Add(new SavedCategory(categoryId, 0, number, name));
            changed = true;
        }
        else if (FindCategory(categoryId) is { } category && category.Name != name)
        {
            categories[categories.IndexOf(category)] = category with { Name = name };
            changed = true;
        }

        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            if (FindPage(PageId(number, page)) == null)
            {
                pages.Add(new SavedPage(number, page, categoryId, ""));
                changed = true;
            }
        }

        return changed;
    }

    private void Touch()
    {
        Revision++;
        Changed = true;
    }

    private SavedCategory? FindCategory(int categoryId) =>
        categoryId == 0 ? null : categories.FirstOrDefault(category => category.Id == categoryId);

    private SavedPage? FindPage(int pageId) => pages.FirstOrDefault(page => PageId(page) == pageId);

    private bool IsHidden(SavedCategory category) =>
        IsCharacter(category) && !listedNames.ContainsKey(category.CharacterNumber);

    private bool IsHidden(SavedPage page) => !listedNames.ContainsKey(page.CharacterNumber);

    private string OwnerName(int characterNumber) =>
        listedNames.TryGetValue(characterNumber, out var name) ? name : CategoryText(CategoryOf(characterNumber));

    private bool IsUntouched(SavedPage page) => page.ParentId == HomeOf(page) && page.Name.Length == 0;

    private int HomeOf(SavedPage page) => CategoryOf(page.CharacterNumber) is var home and not 0 ? home : page.ParentId;

    private int ChildDepth(int parentId) => parentId == 0 ? 0 : Depth(parentId) + 1;

    private int Height(int categoryId, int steps = 0) =>
        steps > MaxDepth
            ? 0
            : 1 + categories.Where(child => child.ParentId == categoryId)
                .Select(child => Height(child.Id, steps + 1))
                .DefaultIfEmpty(0)
                .Max();

    private bool IsWithin(int categoryId, int ancestorId)
    {
        var current = categoryId;
        for (var step = 0; step <= MaxDepth && current != 0; step++)
        {
            if (current == ancestorId)
            {
                return true;
            }

            current = FindCategory(current)?.ParentId ?? 0;
        }

        return false;
    }

    private List<SavedCategory> CategoriesWithin(int ancestorId) =>
        categories.Where(category => IsWithin(category.Id, ancestorId)).ToList();

    private List<SavedPage> PagesWithin(List<SavedCategory> within) =>
        pages.Where(page => within.Any(category => category.Id == page.ParentId)).ToList();

    private void Replace(SavedCategory category, SavedCategory replacement)
    {
        categories[categories.IndexOf(category)] = replacement;
        Touch();
    }

    private void Replace(SavedPage page, SavedPage replacement)
    {
        pages[pages.IndexOf(page)] = replacement;
        Touch();
    }

    private void MoveCategory(SavedCategory category, int parentId, int index)
    {
        categories.Remove(category);
        var moved = category with { ParentId = parentId };
        var shown = ShownCategories(parentId).ToList();
        if (index >= 0 && index < shown.Count)
        {
            categories.Insert(categories.IndexOf(shown[index].Holder), moved);
        }
        else
        {
            categories.Add(moved);
        }
    }

    // Pages lifted out of a hidden category show before the list's own, so a slot among them lands first in its own.
    private void MovePage(SavedPage page, int parentId, int index)
    {
        pages.Remove(page);
        var moved = page with { ParentId = parentId };
        var before = index < 0
            ? null
            : ShownPages(parentId).Skip(index).FirstOrDefault(shown => shown.ParentId == parentId);
        if (before != null)
        {
            pages.Insert(pages.IndexOf(before), moved);
        }
        else
        {
            pages.Add(moved);
        }
    }

    public sealed record SavedCategory(int Id, int ParentId, int CharacterNumber, string Name);

    public sealed record SavedPage(int CharacterNumber, int Page, int ParentId, string Name);
}
