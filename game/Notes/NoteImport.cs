using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TheLastGate.Notes;

public enum NoteImportStatus
{
    Ok,
    NameTaken,
    TooManyCategories,
    TooDeep,
    CategoryFull,
    Failed,
}

public sealed record NoteImportResult(NoteImportStatus Status, int RootId, string CategoryName);

// Adding or merging an imported tree runs the same walk twice: a counting pass that only checks the limits, then the
// pass that changes the set, so a limit never stops an import halfway.
public static class NoteImport
{
    // An exact name wins; otherwise the first that differs only in ASCII case.
    public static int Conflict(NoteSet set, NoteSet imported, int parentId) =>
        imported.Categories.Count > 0
        && FindMatch(set, parentId, imported.Categories[0].Name, set.Categories.Count) is { } match
            ? match.Id
            : 0;

    public static NoteImportResult Add(NoteSet set, NoteSet imported, int parentId, string? rootName)
    {
        if (Refusal(set, imported) is { } refused)
        {
            return refused;
        }

        if (parentId != 0 && set.FindCategory(parentId) == null)
        {
            return Failed();
        }

        var name = NoteText.Fit(rootName ?? imported.Categories[0].Name, NoteLimits.CategoryNameBytes);
        if (name.Length == 0)
        {
            return Failed();
        }

        if (FindMatch(set, parentId, name, set.Categories.Count) != null)
        {
            return new NoteImportResult(NoteImportStatus.NameTaken, 0, "");
        }

        var rootLevel = set.Depth(parentId) + 1;
        var counting = new ImportPass(set, imported, false);
        var planned = counting.PlaceSubtree(imported.Categories[0], parentId, rootLevel, name, out _);
        if (planned != NoteImportStatus.Ok)
        {
            return new NoteImportResult(planned, 0, counting.FullCategory);
        }

        var applying = new ImportPass(set, imported, true);
        var status = applying.PlaceSubtree(imported.Categories[0], parentId, rootLevel, name, out var rootId);
        return new NoteImportResult(status, rootId, applying.FullCategory);
    }

    // Matching categories keep their own name, colour and settings and gain the notes they don't already have.
    public static NoteImportResult Merge(NoteSet set, NoteSet imported, int targetId) =>
        Merge(set, imported, targetId, true);

    private static NoteImportResult MergeTakenOut(NoteSet set, int categoryId, int targetId, bool applying)
    {
        if (set.FindCategory(categoryId) == null || set.FindCategory(targetId) == null
            || set.IsWithin(targetId, categoryId))
        {
            return Failed();
        }

        var subtree = set.TakeSubtree(categoryId);
        var result = Merge(set, subtree.AsImport(), targetId, applying);
        if (!applying || result.Status != NoteImportStatus.Ok)
        {
            set.PutBack(subtree);
        }
        else
        {
            set.Changed = true;
        }

        return result;
    }

    private static NoteImportResult Merge(NoteSet set, NoteSet imported, int targetId, bool applying)
    {
        if (Refusal(set, imported) is { } refused)
        {
            return refused;
        }

        if (set.FindCategory(targetId) is not { } target)
        {
            return Failed();
        }

        var depth = set.Depth(targetId);
        var counting = new ImportPass(set, imported, false);
        var planned = counting.MergeCategory(imported.Categories[0], target, depth);
        if (planned != NoteImportStatus.Ok)
        {
            return new NoteImportResult(planned, 0, counting.FullCategory);
        }

        if (!applying)
        {
            return new NoteImportResult(NoteImportStatus.Ok, targetId, "");
        }

        var status = new ImportPass(set, imported, true).MergeCategory(imported.Categories[0], target, depth);
        return new NoteImportResult(status, status == NoteImportStatus.Ok ? targetId : 0, "");
    }

    // The dragged tree is taken out first, so it doesn't count against the limits and may merge into a same-named
    // category that held it; a refused merge puts it back as it was.
    public static NoteImportResult MergeMoved(NoteSet set, int categoryId, int targetId) =>
        MergeTakenOut(set, categoryId, targetId, true);

    public static NoteImportResult CheckMergeMoved(NoteSet set, int categoryId, int targetId) =>
        MergeTakenOut(set, categoryId, targetId, false);

    public static string Problem(NoteImportResult result) => result.Status switch
    {
        NoteImportStatus.NameTaken => "That name is already taken.",
        NoteImportStatus.TooManyCategories => $"Import would go over the {NoteLimits.Categories} category limit.",
        NoteImportStatus.TooDeep => $"Import would nest categories more than {NoteLimits.Depth} deep.",
        NoteImportStatus.CategoryFull => $"Import would overfill \"{result.CategoryName}\".",
        NoteImportStatus.Failed => "Import failed.",
        _ => throw new UnreachableException(),
    };

    public static string ConflictMessage(string? destinationName, string name) =>
        destinationName == null
            ? $"You already have a category called \"{name}\"."
            : $"\"{destinationName}\" already has a category called \"{name}\".";

    private static NoteImportResult Failed() => new(NoteImportStatus.Failed, 0, "");

    private static NoteImportResult? Refusal(NoteSet set, NoteSet imported)
    {
        if (imported.Categories.Count < 1)
        {
            return Failed();
        }

        return imported.Categories.Count > NoteLimits.Categories || set.Categories.Count > NoteLimits.Categories
            ? new NoteImportResult(NoteImportStatus.TooManyCategories, 0, "")
            : null;
    }

    // Only categories that were there before the import can be matched, so two incoming siblings with the same name
    // and no local match are each added under a name of their own rather than merged into one another.
    private static NoteCategory? FindMatch(NoteSet set, int parentId, string name, int limit) =>
        set.FindNamed(parentId, name, limit);

    private static bool Same(Note left, Note right) =>
        left.Kind == NoteKind.Pin && right.Kind == NoteKind.Pin ? left.IsSamePin(right) : left.IsIdenticalTo(right);

    private sealed class ImportPass(NoteSet set, NoteSet imported, bool applying)
    {
        private readonly Dictionary<NoteCategory, NoteCategory> matchedLocal = [];
        private readonly int originalCount = set.Categories.Count;
        private readonly Dictionary<NoteCategory, (int Notes, int Segments)> planned = [];
        private readonly HashSet<NoteCategory> visited = [];
        private int addedCategories;

        public string FullCategory { get; private set; } = "";

        public NoteImportStatus PlaceSubtree(
            NoteCategory incoming,
            int localParentId,
            int level,
            string? rootName,
            out int placedId)
        {
            placedId = 0;
            visited.Add(incoming);
            if (!applying)
            {
                if (level > NoteLimits.Depth)
                {
                    return NoteImportStatus.TooDeep;
                }

                if (originalCount + ++addedCategories > NoteLimits.Categories)
                {
                    return NoteImportStatus.TooManyCategories;
                }

                if (incoming.Notes.Count > NoteLimits.NotesPerCategory
                    || incoming.SegmentTotal > NoteLimits.SegmentsPerCategory)
                {
                    FullCategory = rootName ?? incoming.Name;
                    return NoteImportStatus.CategoryFull;
                }
            }
            else
            {
                var name = rootName ?? set.UniqueCategoryName(localParentId, incoming.Name);
                var copy = NoteCategory.Detached(
                    incoming.Name,
                    incoming.ColourIndex,
                    incoming.Visible,
                    incoming.ShowLabels,
                    incoming.Notes);
                placedId = set.Attach(copy, localParentId);
                if (placedId == 0)
                {
                    return NoteImportStatus.Failed;
                }

                copy.Name = name;
            }

            foreach (var child in UnvisitedChildren(incoming))
            {
                var status = PlaceSubtree(child, placedId, level + 1, null, out _);
                if (status != NoteImportStatus.Ok)
                {
                    return status;
                }
            }

            return NoteImportStatus.Ok;
        }

        public NoteImportStatus MergeCategory(NoteCategory incoming, NoteCategory local, int level)
        {
            visited.Add(incoming);
            matchedLocal[incoming] = local;
            var status = applying ? MoveNotes(incoming, local) : PlanNotes(incoming, local);
            if (status != NoteImportStatus.Ok)
            {
                return status;
            }

            foreach (var child in UnvisitedChildren(incoming))
            {
                status = FindMatch(set, local.Id, child.Name, originalCount) is { } match
                    ? MergeCategory(child, match, level + 1)
                    : PlaceSubtree(child, local.Id, level + 1, null, out _);
                if (status != NoteImportStatus.Ok)
                {
                    return status;
                }
            }

            return NoteImportStatus.Ok;
        }

        private List<NoteCategory> UnvisitedChildren(NoteCategory parent) =>
            imported.Categories.Where(child => !visited.Contains(child) && child.ParentId == parent.Id).ToList();

        private NoteImportStatus PlanNotes(NoteCategory incoming, NoteCategory local)
        {
            var (notes, segments) = planned.GetValueOrDefault(local);
            for (var index = 0; index < incoming.Notes.Count; index++)
            {
                if (!IsKnown(incoming, local, index))
                {
                    notes++;
                    segments += incoming.Notes[index].Segments.Count;
                }
            }

            planned[local] = (notes, segments);
            if (local.Notes.Count + notes > NoteLimits.NotesPerCategory
                || local.SegmentTotal + segments > NoteLimits.SegmentsPerCategory)
            {
                FullCategory = local.Name;
                return NoteImportStatus.CategoryFull;
            }

            return NoteImportStatus.Ok;
        }

        // Matching against every earlier incoming note bound for the same category gives the same answer as matching
        // against what the second pass will have added by then.
        private bool IsKnown(NoteCategory incoming, NoteCategory local, int index)
        {
            var note = incoming.Notes[index];
            return local.Notes.Any(known => Same(known, note))
                || incoming.Notes.Take(index).Any(earlier => Same(earlier, note))
                || matchedLocal.Any(match =>
                    match.Key != incoming && match.Value == local
                    && match.Key.Notes.Any(earlier => Same(earlier, note)));
        }

        private NoteImportStatus MoveNotes(NoteCategory incoming, NoteCategory local)
        {
            foreach (var note in incoming.Notes)
            {
                if (!local.Notes.Any(known => Same(known, note)))
                {
                    set.AppendNote(local, note);
                }
            }

            return NoteImportStatus.Ok;
        }
    }
}
