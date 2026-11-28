using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using TheLastGate.Notes;
using TheLastGate.Windows;

namespace TheLastGate.Map;

public sealed class NoteListSource(NoteSet notes, WorldMapEditor editor) : ICategoryListSource
{
    public int Revision => HashCode.Combine(RuntimeHelpers.GetHashCode(notes), notes.Revision);

    public IReadOnlyCollection<int> Expanded => editor.ExpandedCategories;

    public bool CanDrag => editor.CanDrag;

    public IReadOnlyList<CategoryRow> BuildRows() => Build(notes, editor.ExpandedCategories);

    public CategoryRowLook Look(CategoryRow row)
    {
        if (row.IsCategory)
        {
            return notes.FindCategory(row.Id) is { } category
                ? new CategoryRowLook(category.Name, Colour(NotePalette.Rgb(category.ColourIndex)), StateOf(row))
                : new CategoryRowLook("", null, CategoryRowState.Normal);
        }

        if (notes.OwnerOf(row.Id) is not { } owner || notes.FindNote(row.Id) is not { } note)
        {
            return new CategoryRowLook("", null, CategoryRowState.Normal);
        }

        var swatch = note.ColourIndex == Note.CategoryColour ? (Color?)null : Colour(NoteSet.Rgb(owner, note));
        return new CategoryRowLook(WorldMapWording.KindPrefix(note.Kind) + note.Label, swatch, StateOf(row));
    }

    public void Expand(int categoryId, bool expanded) => editor.Expand(categoryId, expanded);

    public bool CanDrop(CategoryDrag drag, CategoryDropSlot slot) =>
        editor.CheckDrop(notes, drag, slot) == NoteMoveRefusal.None;

    public bool Drop(CategoryDrag drag, CategoryDropSlot slot)
    {
        var result = editor.Drop(notes, drag, slot);
        switch (result.Outcome)
        {
            case NoteDropOutcome.Moved:
                return true;
            case NoteDropOutcome.Clash:
                ClashFound?.Invoke(drag, slot, result.ClashId);
                return false;
            case NoteDropOutcome.Refused:
                return false;
            default:
                throw new UnreachableException();
        }
    }

    public event Action<CategoryDrag, CategoryDropSlot, int>? ClashFound;

    public static IReadOnlyList<CategoryRow> Build(NoteSet shown, IReadOnlyCollection<int> expanded)
    {
        var rows = new List<CategoryRow>();
        AddList(shown, expanded, 0, 0, rows);
        return rows;
    }

    private static Color Colour(uint rgb) => Color.Color8((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private static void AddList(
        NoteSet shown,
        IReadOnlyCollection<int> expanded,
        int parentId,
        int depth,
        List<CategoryRow> rows)
    {
        var children = shown.Categories
            .Where(category => category.ParentId == parentId && category.Id != parentId)
            .ToList();
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            rows.Add(new CategoryRow(child.Id, true, depth, parentId, index));
            if (!expanded.Contains(child.Id) || depth + 1 >= NoteLimits.Depth)
            {
                continue;
            }

            AddList(shown, expanded, child.Id, depth + 1, rows);
            var subcategories = shown.Categories.Count(category =>
                category.ParentId == child.Id && category.Id != child.Id);
            for (var noteIndex = 0; noteIndex < child.Notes.Count; noteIndex++)
            {
                rows.Add(
                    new CategoryRow(child.Notes[noteIndex].Id, false, depth + 1, child.Id, subcategories + noteIndex));
            }
        }
    }

    private CategoryRowState StateOf(CategoryRow row)
    {
        if (row.IsCategory ? editor.CurrentCategoryId == row.Id : editor.SelectedNoteId == row.Id)
        {
            return row.IsCategory ? CategoryRowState.Current : CategoryRowState.Selected;
        }

        var category = row.IsCategory ? notes.FindCategory(row.Id) : notes.OwnerOf(row.Id);
        return category != null && !notes.IsShown(category) ? CategoryRowState.Dimmed : CategoryRowState.Normal;
    }
}
