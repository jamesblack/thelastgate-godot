using TheLastGate.Notes;

namespace TheLastGate.Windows;

public readonly record struct CategoryDrag(int Id, bool IsCategory);

// Onto drops into the category ParentId, last; otherwise the item lands at Index in ParentId's list.
public readonly record struct CategoryDropSlot(int ParentId, int Index, bool Onto);

public enum NoteDropOutcome
{
    Moved,
    Refused,
    Clash,
}

public readonly record struct NoteDropResult(NoteDropOutcome Outcome, NoteMoveRefusal Refusal, int ClashId);
