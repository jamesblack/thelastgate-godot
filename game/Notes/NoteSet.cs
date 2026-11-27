using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Notes;

// The player's map notes: categories nested up to eight deep, kept in the order the tree shows them among siblings.
// Categories and notes share one id counter, starting at 1; 0 means none.
public sealed class NoteSet
{
    private readonly List<NoteCategory> categories = [];
    private bool changed;
    private int nextId = 1;

    public IReadOnlyList<NoteCategory> Categories => categories;

    public bool Changed
    {
        get => changed;
        set
        {
            changed = value;
            if (value)
            {
                Revision++;
            }
        }
    }

    // Counts changes, so whatever shows the notes knows when to look again; saving clears Changed but not this.
    public int Revision { get; private set; }

    public bool ShowOnMinimap { get; set; }

    public bool ShowInWorld { get; set; }

    public int AddCategory(string name, int colourIndex, int parentId = 0)
    {
        if (!AcceptsChild(parentId) || categories.Count >= NoteLimits.Categories)
        {
            return 0;
        }

        var category = new NoteCategory
        {
            Id = nextId++,
            ParentId = parentId,
            Name = UniqueName(parentId, 0, name),
            ColourIndex = IsPaletteColour(colourIndex) ? colourIndex : 0,
        };
        categories.Add(category);
        Changed = true;
        return category.Id;
    }

    public NoteCategory? FindCategory(int categoryId) =>
        categories.FirstOrDefault(category => category.Id == categoryId);

    // Counted within each category of the subtree, against the pins before it there.
    public int CountDuplicatePins(int categoryId) =>
        categories
            .Where(category => IsWithin(category.Id, categoryId))
            .Sum(category => category.NoteList
                .Where((note, index) => category.NoteList.Take(index).Any(earlier => earlier.IsSamePin(note)))
                .Count());

    // The first copy stays, so the pin that has been on the map longest keeps its id and colour.
    public int RemoveDuplicatePins(int categoryId)
    {
        var removed = 0;
        foreach (var category in categories.Where(category => IsWithin(category.Id, categoryId)))
        {
            var kept = new List<Note>();
            foreach (var note in category.NoteList)
            {
                if (kept.Any(earlier => earlier.IsSamePin(note)))
                {
                    removed++;
                    continue;
                }

                kept.Add(note);
            }

            category.NoteList.Clear();
            category.NoteList.AddRange(kept);
        }

        if (removed > 0)
        {
            Changed = true;
        }

        return removed;
    }

    public static uint Rgb(NoteCategory category, Note note) =>
        NotePalette.Rgb(IsPaletteColour(note.ColourIndex) ? note.ColourIndex : category.ColourIndex);

    // An attached category keeps its name as it is, even if a sibling has it; imports decide how to handle clashes.
    public int Attach(NoteCategory detached, int parentId)
    {
        if (!AcceptsChild(parentId) || categories.Count >= NoteLimits.Categories)
        {
            return 0;
        }

        detached.ParentId = parentId;
        detached.Id = nextId++;
        for (var index = 0; index < detached.NoteList.Count; index++)
        {
            detached.NoteList[index] = detached.NoteList[index] with { Id = nextId++ };
        }

        categories.Add(detached);
        Changed = true;
        return detached.Id;
    }

    public int AddPoint(int categoryId, NoteKind kind, int tileX, int tileY, string label)
    {
        if (FindCategory(categoryId) is not { } category
            || kind is not (NoteKind.Pin or NoteKind.Text)
            || !IsTile(tileX)
            || !IsTile(tileY)
            || (kind == NoteKind.Text && label.Length == 0)
            || category.NoteList.Count >= NoteLimits.NotesPerCategory)
        {
            return 0;
        }

        return Add(category, new Note(kind, Fit(label), tileX, tileY, [], Note.CategoryColour));
    }

    public int AddPath(int categoryId, IReadOnlyList<PathSegment> segments, string label)
    {
        if (FindCategory(categoryId) is not { } category
            || !AreValid(segments)
            || category.NoteList.Count >= NoteLimits.NotesPerCategory
            || category.SegmentTotal + segments.Count > NoteLimits.SegmentsPerCategory)
        {
            return 0;
        }

        var start = segments[0];
        return Add(
            category,
            new Note(NoteKind.Path, Fit(label), start.StartX, start.StartY, segments.ToArray(), Note.CategoryColour));
    }

    public Note? FindNote(int noteId) => OwnerOf(noteId)?.NoteList.First(note => note.Id == noteId);

    public NoteCategory? OwnerOf(int noteId) =>
        categories.FirstOrDefault(category => category.NoteList.Any(note => note.Id == noteId));

    public bool RemoveNote(int noteId)
    {
        if (OwnerOf(noteId) is not { } owner)
        {
            return false;
        }

        owner.NoteList.RemoveAll(note => note.Id == noteId);
        Changed = true;
        return true;
    }

    public bool SetNoteColour(int noteId, int colourIndex)
    {
        if (OwnerOf(noteId) is not { } owner
            || (colourIndex != Note.CategoryColour && !IsPaletteColour(colourIndex)))
        {
            return false;
        }

        Store(owner, FindNote(noteId)! with { ColourIndex = colourIndex });
        return true;
    }

    // An edit changes the label and where the note is; its id, kind and colour stay. Text must keep a label, and a
    // path its category's segment limit.
    public bool ReplaceNote(int noteId, Note edited)
    {
        if (OwnerOf(noteId) is not { } owner)
        {
            return false;
        }

        var stored = FindNote(noteId)!;
        if (stored.Kind == NoteKind.Text && edited.Label.Length == 0)
        {
            return false;
        }

        if (stored.Kind == NoteKind.Path)
        {
            if (!AreValid(edited.Segments)
                || owner.SegmentTotal - stored.Segments.Count + edited.Segments.Count
                > NoteLimits.SegmentsPerCategory)
            {
                return false;
            }

            var start = edited.Segments[0];
            Store(
                owner,
                stored with
                {
                    Label = Fit(edited.Label),
                    TileX = start.StartX,
                    TileY = start.StartY,
                    Segments = edited.Segments.ToArray(),
                });
            return true;
        }

        if (!IsTile(edited.TileX) || !IsTile(edited.TileY))
        {
            return false;
        }

        Store(
            owner,
            stored with { Label = Fit(edited.Label), TileX = edited.TileX, TileY = edited.TileY, Segments = [] });
        return true;
    }

    public bool HasChildren(int categoryId) =>
        categoryId != 0 && categories.Any(category => category.ParentId == categoryId && category.Id != categoryId);

    public int Depth(int categoryId)
    {
        var depth = 0;
        for (var current = FindCategory(categoryId);
            current != null && depth <= NoteLimits.Depth;
            current = FindCategory(current.ParentId))
        {
            depth++;
        }

        return depth;
    }

    public int SubtreeHeight(int categoryId) => FindCategory(categoryId) == null ? 0 : HeightFrom(categoryId, 1);

    public bool IsWithin(int categoryId, int ancestorId)
    {
        var current = categoryId;
        for (var step = 0; step <= NoteLimits.Depth && current != 0; step++)
        {
            if (current == ancestorId)
            {
                return true;
            }

            current = FindCategory(current)?.ParentId ?? 0;
        }

        return false;
    }

    public bool IsShown(NoteCategory category) => AllTheWayUp(category, shown => shown.Visible);

    public bool LabelsShown(NoteCategory category) => AllTheWayUp(category, shown => shown.ShowLabels);

    public IReadOnlyList<NoteCategory> Tree(int rootId = 0)
    {
        var ordered = new List<NoteCategory>();
        if (rootId != 0)
        {
            if (FindCategory(rootId) is { } root)
            {
                Collect(root, 1, ordered);
            }

            return ordered;
        }

        foreach (var topLevel in categories.Where(category => category.ParentId == 0).ToList())
        {
            Collect(topLevel, 1, ordered);
        }

        return ordered;
    }

    public bool RemoveCategory(int categoryId)
    {
        if (FindCategory(categoryId) is not { } category || HasChildren(categoryId))
        {
            return false;
        }

        categories.Remove(category);
        Changed = true;
        return true;
    }

    public bool RenameCategory(int categoryId, string name)
    {
        if (FindCategory(categoryId) is not { } category || name.Length == 0)
        {
            return false;
        }

        category.Name = UniqueName(category.ParentId, categoryId, name);
        Changed = true;
        return true;
    }

    public bool SetCategoryColour(int categoryId, int colourIndex)
    {
        if (FindCategory(categoryId) is not { } category || !IsPaletteColour(colourIndex))
        {
            return false;
        }

        category.ColourIndex = colourIndex;
        Changed = true;
        return true;
    }

    // The category's own parent is allowed: moving there reorders it.
    public NoteMoveRefusal CheckCategoryMove(int categoryId, int newParentId)
    {
        if (FindCategory(categoryId) == null || (newParentId != 0 && FindCategory(newParentId) == null))
        {
            return NoteMoveRefusal.Missing;
        }

        if (IsWithin(newParentId, categoryId))
        {
            return NoteMoveRefusal.IntoItself;
        }

        return Depth(newParentId) + SubtreeHeight(categoryId) > NoteLimits.Depth
            ? NoteMoveRefusal.TooDeep
            : NoteMoveRefusal.None;
    }

    // Siblings show in their order in the list, so the category goes in just before the sibling now at that index.
    public bool MoveCategory(int categoryId, int newParentId, int index)
    {
        if (CheckCategoryMove(categoryId, newParentId) != NoteMoveRefusal.None)
        {
            return false;
        }

        var category = FindCategory(categoryId)!;
        categories.Remove(category);
        category.Name = UniqueName(newParentId, categoryId, category.Name);
        category.ParentId = newParentId;
        var siblings = categories.Where(sibling => sibling.ParentId == newParentId).ToList();
        var place = index >= 0 && index < siblings.Count
            ? categories.IndexOf(siblings[index])
            : siblings.Count > 0
                ? categories.IndexOf(siblings[^1]) + 1
                : categories.Count;
        categories.Insert(place, category);
        Changed = true;
        return true;
    }

    public NoteMoveRefusal CheckNoteMove(int noteId, int categoryId)
    {
        if (OwnerOf(noteId) is not { } owner || FindCategory(categoryId) is not { } target)
        {
            return NoteMoveRefusal.Missing;
        }

        return target != owner
            && (target.NoteList.Count >= NoteLimits.NotesPerCategory
                || target.SegmentTotal + FindNote(noteId)!.Segments.Count > NoteLimits.SegmentsPerCategory)
                ? NoteMoveRefusal.CategoryFull
                : NoteMoveRefusal.None;
    }

    public bool MoveNote(int noteId, int categoryId, int index)
    {
        if (CheckNoteMove(noteId, categoryId) != NoteMoveRefusal.None)
        {
            return false;
        }

        var note = FindNote(noteId)!;
        OwnerOf(noteId)!.NoteList.Remove(note);
        var target = FindCategory(categoryId)!;
        target.NoteList.Insert(index >= 0 && index < target.NoteList.Count ? index : target.NoteList.Count, note);
        Changed = true;
        return true;
    }

    public int Clash(int categoryId, int newParentId) =>
        FindCategory(categoryId) is { } category
        && FindNamed(newParentId, category.Name, categories.Count, categoryId) is { } match
            ? match.Id
            : 0;

    internal NoteSubtree TakeSubtree(int categoryId)
    {
        var taken = categories
            .Select((category, index) => (Index: index, Category: category))
            .Where(entry => IsWithin(entry.Category.Id, categoryId))
            .ToList();
        foreach (var entry in Enumerable.Reverse(taken))
        {
            categories.RemoveAt(entry.Index);
        }

        return new NoteSubtree(taken.First(entry => entry.Category.Id == categoryId).Category, taken);
    }

    internal void PutBack(NoteSubtree subtree)
    {
        foreach (var (index, category) in subtree.Taken)
        {
            categories.Insert(index, category);
        }
    }

    // An exact name wins; otherwise the first that differs only in ASCII case. Only the first limit categories are
    // looked at, so an import can leave out the ones it has just added.
    internal NoteCategory? FindNamed(int parentId, string name, int limit, int ignoredId = 0)
    {
        NoteCategory? looseMatch = null;
        foreach (var category in categories.Take(limit))
        {
            if (category.ParentId != parentId || category.Id == parentId || category.Id == ignoredId)
            {
                continue;
            }

            if (category.Name == name)
            {
                return category;
            }

            looseMatch ??= NoteText.SameName(category.Name, name) ? category : null;
        }

        return looseMatch;
    }

    private static bool IsPaletteColour(int colourIndex) => colourIndex is >= 0 and < NotePalette.Size;

    private static bool IsTile(int tile) => tile is >= 0 and < NoteLimits.Tiles;

    private static bool AreValid(IReadOnlyList<PathSegment> segments) =>
        segments.Count is >= 1 and <= NoteLimits.SegmentsPerPath
        && segments.All(segment =>
            IsTile(segment.StartX) && IsTile(segment.StartY) && IsTile(segment.EndX) && IsTile(segment.EndY));

    internal string UniqueCategoryName(int parentId, string wanted) => UniqueName(parentId, 0, wanted);

    internal void AppendNote(NoteCategory category, Note note) => Add(category, note);

    private static string Fit(string label) => NoteText.Fit(label, NoteLimits.LabelBytes);

    private int Add(NoteCategory category, Note note)
    {
        var added = note with { Id = nextId++ };
        category.NoteList.Add(added);
        Changed = true;
        return added.Id;
    }

    private void Store(NoteCategory owner, Note note)
    {
        owner.NoteList[owner.NoteList.FindIndex(stored => stored.Id == note.Id)] = note;
        Changed = true;
    }

    private bool AcceptsChild(int parentId) =>
        parentId == 0 || (FindCategory(parentId) != null && Depth(parentId) < NoteLimits.Depth);

    private int HeightFrom(int categoryId, int level)
    {
        if (level > NoteLimits.Depth)
        {
            return 0;
        }

        var tallest = categories
            .Where(category => category.ParentId == categoryId && category.Id != categoryId)
            .Select(child => HeightFrom(child.Id, level + 1))
            .DefaultIfEmpty(0)
            .Max();
        return tallest + 1;
    }

    private bool AllTheWayUp(NoteCategory category, Func<NoteCategory, bool> isOn)
    {
        var current = category;
        for (var step = 0; step <= NoteLimits.Depth && current != null; step++)
        {
            if (!isOn(current))
            {
                return false;
            }

            current = FindCategory(current.ParentId);
        }

        return true;
    }

    private void Collect(NoteCategory category, int level, List<NoteCategory> ordered)
    {
        ordered.Add(category);
        if (level >= NoteLimits.Depth)
        {
            return;
        }

        foreach (var child in categories.Where(child => child != category && child.ParentId == category.Id).ToList())
        {
            Collect(child, level + 1, ordered);
        }
    }

    // A name taken by a sibling, ignoring ASCII case, gets " (2)", " (3)" and so on, cut to keep within the limit.
    private string UniqueName(int parentId, int ignoredId, string wanted)
    {
        var name = NoteText.Fit(wanted, NoteLimits.CategoryNameBytes);
        for (var attempt = 2; NameInUse(parentId, ignoredId, name); attempt++)
        {
            var suffix = $" ({attempt})";
            name = NoteText.Fit(wanted, NoteLimits.CategoryNameBytes - suffix.Length) + suffix;
        }

        return name;
    }

    private bool NameInUse(int parentId, int ignoredId, string name) =>
        categories.Any(category =>
            category.Id != ignoredId && category.ParentId == parentId && NoteText.SameName(category.Name, name));
}
