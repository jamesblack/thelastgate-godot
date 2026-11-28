using System.Collections.Generic;
using Godot;

namespace TheLastGate.Windows;

public readonly record struct CategoryRowLook(
    string Text,
    Color? Swatch,
    CategoryRowState State,
    string HoverText = "");

public interface ICategoryListSource
{
    // Only a change to which rows exist, or where, counts: the list rebuilds its rows when this changes, and asks
    // for every row's look each frame anyway.
    int Revision { get; }

    IReadOnlyCollection<int> Expanded { get; }

    bool CanDrag { get; }

    IReadOnlyList<CategoryRow> BuildRows();

    CategoryRowLook Look(CategoryRow row);

    void Expand(int categoryId, bool expanded);

    bool CanDrop(CategoryDrag drag, CategoryDropSlot slot);

    bool Drop(CategoryDrag drag, CategoryDropSlot slot);
}
