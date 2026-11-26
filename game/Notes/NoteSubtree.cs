using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Notes;

public sealed class NoteSubtree
{
    internal NoteSubtree(NoteCategory root, IReadOnlyList<(int Index, NoteCategory Category)> taken)
    {
        Root = root;
        Taken = taken;
    }

    internal NoteCategory Root { get; }

    internal IReadOnlyList<(int Index, NoteCategory Category)> Taken { get; }

    // The root comes first, as in an imported set.
    public NoteSet AsImport()
    {
        var copy = new NoteSet();
        AttachCopy(copy, Root, 0);
        return copy;
    }

    private void AttachCopy(NoteSet copy, NoteCategory original, int parentId)
    {
        var attachedId = copy.Attach(
            NoteCategory.Detached(
                original.Name,
                original.ColourIndex,
                original.Visible,
                original.ShowLabels,
                original.Notes),
            parentId);
        foreach (var child in Taken.Select(entry => entry.Category).Where(child => child.ParentId == original.Id))
        {
            AttachCopy(copy, child, attachedId);
        }
    }
}
