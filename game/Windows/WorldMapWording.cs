using System.Diagnostics;
using TheLastGate.Notes;

namespace TheLastGate.Windows;

// The words the world map's category column, menus and prompts use for notes and categories.
public static class WorldMapWording
{
    public static string KindPrefix(NoteKind kind) => kind switch
    {
        NoteKind.Pin => "Pin: ",
        NoteKind.Text => "Text: ",
        NoteKind.Path => "Path: ",
        _ => throw new UnreachableException(),
    };

    public static string MergeQuestion(string moved, string? target) =>
        target == null ? $"Move \"{moved}\" to the top level?" : $"Move \"{moved}\" into \"{target}\"?";

    public static string MergeClash(string? target, string name) =>
        $"{(target == null ? "The top level" : $"\"{target}\"")} already has a category called \"{name}\".";

    public static string DeleteNoteQuestion(Note note) => note.Label.Length > 0
        ? $"Delete {Noun(note.Kind)} \"{note.Label}\"?"
        : $"Delete this unnamed {Noun(note.Kind)}?";

    public static string DeleteCategoryQuestion(NoteCategory category) =>
        $"Delete category \"{category.Name}\" and its {category.Notes.Count} annotations?";

    public static string DuplicatePinsItem(int count) =>
        count > 0 ? $"Remove {Pins(count)}..." : "Remove duplicate pins...";

    public static string DuplicatePinsQuestion(int count, string categoryName, bool hasSubcategories) =>
        $"Remove {Pins(count)} from \"{categoryName}\"{(hasSubcategories ? " and its subcategories" : "")}?";

    public static string DuplicatePinsRemoved(int count) => $"Removed {Pins(count)}.";

    private static string Pins(int count) => $"{count} duplicate pin{(count == 1 ? "" : "s")}";

    private static string Noun(NoteKind kind) => kind switch
    {
        NoteKind.Pin => "pin",
        NoteKind.Text => "text label",
        NoteKind.Path => "path",
        _ => throw new UnreachableException(),
    };
}
