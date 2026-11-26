using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheLastGate.Map;
using TheLastGate.Notes;

namespace TheLastGate.Windows;

public enum NoteLabelPurpose
{
    None,
    Pin,
    Text,
    Path,
}

// What the world map's tools do with the mouse: Pin and Text ask for a name for the tile clicked, Line drags out the
// segments of a path that is named when it is finished, and Select picks a note. A note being edited is changed on a
// copy, which the map draws in its place until it is saved or cancelled.
public sealed class WorldMapEditor
{
    public const double SelectRadius = 8;

    // A press that moves no further than this is still a click, so placing a pin needs no perfectly still hand.
    public const double ClickSlack = 2;

    private const double SnapPixels = 8;

    private readonly HashSet<int> expanded = [];
    private readonly List<PathSegment> pending = [];
    private int labelCategoryId;
    private (int X, int Y) labelTile;
    private bool leftDragged;
    private bool leftHeld;
    private (double X, double Y) leftPressedAt;
    private bool pressOnWorld;
    private (int X, int Y) pressTile;
    private bool rightDragged;
    private bool rightHeld;
    private (double X, double Y) rightPressedAt;

    public WorldMapTool Tool { get; private set; }

    public int CurrentCategoryId { get; private set; }

    public int SelectedNoteId { get; private set; }

    public IReadOnlyList<PathSegment> PendingSegments => pending;

    public int PendingCategoryId { get; private set; }

    public Note? Edited { get; private set; }

    public bool CanSaveEdit => Edited is not { Kind: NoteKind.Text, Label.Length: 0 };

    public NoteLabelPurpose LabelPurpose { get; private set; }

    // Enter finishes a path only while the map has the player's attention.
    public bool Focused { get; set; }

    private bool DrawsSegments => Edited?.Kind == NoteKind.Path || (Edited == null && Tool == WorldMapTool.Line);

    private IReadOnlyList<PathSegment> DrawnSegments => Edited?.Kind == NoteKind.Path
        ? Edited.Segments
        : Edited == null && Tool == WorldMapTool.Line
            ? pending
            : [];

    public IReadOnlyCollection<int> ExpandedCategories => expanded;

    // A merge gives the notes it moves new ids, so nothing may be mid-edit while notes are dragged about.
    public bool CanDrag => Edited == null && pending.Count == 0;

    private void ExpandWithAncestors(NoteSet notes, int categoryId)
    {
        for (var depth = 0; categoryId != 0 && depth <= NoteLimits.Depth; depth++)
        {
            if (notes.FindCategory(categoryId) is not { } category)
            {
                return;
            }

            Expand(categoryId, true);
            categoryId = category.ParentId;
        }
    }

    public event Action<string>? Problem;

    public void Expand(int categoryId, bool isExpanded)
    {
        if (isExpanded)
        {
            expanded.Add(categoryId);
        }
        else
        {
            expanded.Remove(categoryId);
        }
    }

    public void ChooseCategory(int categoryId) => CurrentCategoryId = categoryId;

    // A new category becomes the one new notes go into, and is shown open under its parent.
    public int CreateCategory(NoteSet notes, string name, int colourIndex, int parentId)
    {
        var created = notes.AddCategory(name, colourIndex, parentId);
        if (created == 0)
        {
            Problem?.Invoke(
                notes.Depth(parentId) >= NoteLimits.Depth
                    ? "Categories can only nest 8 deep."
                    : "Too many categories.");
            return 0;
        }

        CurrentCategoryId = created;
        if (parentId != 0)
        {
            Expand(parentId, true);
        }

        return created;
    }

    // Nothing the editor holds may point into a category that has gone.
    public void DeleteCategory(NoteSet notes, int categoryId)
    {
        if (Edited != null && notes.OwnerOf(Edited.Id)?.Id == categoryId)
        {
            CancelEdit();
        }

        if (SelectedNoteId != 0 && notes.OwnerOf(SelectedNoteId)?.Id == categoryId)
        {
            SelectedNoteId = 0;
        }

        if (PendingCategoryId == categoryId)
        {
            pending.Clear();
        }

        if (CurrentCategoryId == categoryId)
        {
            CurrentCategoryId = 0;
        }

        Expand(categoryId, false);
        notes.RemoveCategory(categoryId);
    }

    public int RemoveDuplicatePins(NoteSet notes, int categoryId)
    {
        var removed = notes.RemoveDuplicatePins(categoryId);
        if (Edited != null && notes.FindNote(Edited.Id) == null)
        {
            CancelEdit();
        }

        if (notes.FindNote(SelectedNoteId) == null)
        {
            SelectedNoteId = 0;
        }

        Problem?.Invoke(WorldMapWording.DuplicatePinsRemoved(removed));
        return removed;
    }

    // A finished import is chosen and shown open, with the category it went into.
    public bool FinishImport(NoteImportResult result, int parentId)
    {
        if (result.Status != NoteImportStatus.Ok)
        {
            return false;
        }

        CurrentCategoryId = result.RootId;
        if (parentId != 0)
        {
            Expand(parentId, true);
        }

        Expand(result.RootId, true);
        return true;
    }

    public void EnsureCurrentCategory(NoteSet notes)
    {
        if (notes.FindCategory(CurrentCategoryId) == null)
        {
            CurrentCategoryId = notes.Categories.Count > 0 ? notes.Categories[0].Id : 0;
        }

        if (!WorldMapWindow.IsAvailable(Tool, CurrentCategoryId != 0))
        {
            ChooseTool(WorldMapTool.Select);
        }
    }

    // Putting down the Line tool finishes the path being drawn; picking it up starts a new one.
    public void ChooseTool(WorldMapTool tool)
    {
        if (tool == Tool)
        {
            return;
        }

        CancelEdit();
        if (Tool == WorldMapTool.Line)
        {
            AskForPathName();
        }

        Tool = tool;
        if (tool == WorldMapTool.Line)
        {
            pending.Clear();
            PendingCategoryId = CurrentCategoryId;
        }
    }

    public void Select(int noteId)
    {
        if (Edited != null && Edited.Id != noteId)
        {
            CancelEdit();
        }

        SelectedNoteId = noteId;
    }

    public void BeginEdit(NoteSet notes, int noteId)
    {
        ChooseTool(WorldMapTool.Select);
        CancelEdit();
        if (notes.FindNote(noteId) is not { } note)
        {
            return;
        }

        Edited = note;
        SelectedNoteId = noteId;
    }

    public void RenameEdited(string label)
    {
        if (Edited != null)
        {
            Edited = Edited with { Label = label };
        }
    }

    // A path edited down to no segments is deleted.
    public void SaveEdit(NoteSet notes)
    {
        if (Edited == null)
        {
            return;
        }

        if (Edited is { Kind: NoteKind.Path, Segments.Count: 0 })
        {
            notes.RemoveNote(Edited.Id);
            if (SelectedNoteId == Edited.Id)
            {
                SelectedNoteId = 0;
            }
        }
        else
        {
            notes.ReplaceNote(Edited.Id, Edited);
        }

        CancelEdit();
    }

    public void CancelEdit() => Edited = null;

    public void Delete(NoteSet notes, int noteId)
    {
        if (Edited?.Id == noteId)
        {
            CancelEdit();
        }

        if (SelectedNoteId == noteId)
        {
            SelectedNoteId = 0;
        }

        notes.RemoveNote(noteId);
    }

    public void DeleteSegment(NoteSet notes, int noteId, int segmentIndex)
    {
        if (notes.FindNote(noteId) is not { Kind: NoteKind.Path } path
            || segmentIndex < 0
            || segmentIndex >= path.Segments.Count)
        {
            return;
        }

        if (path.Segments.Count == 1)
        {
            Delete(notes, noteId);
            return;
        }

        notes.ReplaceNote(noteId, path with { Segments = Without(path.Segments, segmentIndex) });
    }

    public void Recolour(NoteSet notes, int noteId, int colourIndex)
    {
        if (notes.SetNoteColour(noteId, colourIndex) && Edited?.Id == noteId)
        {
            Edited = Edited with { ColourIndex = colourIndex };
        }
    }

    // Dropping onto the category an item is already in isn't a target; the line between rows reorders instead.
    public NoteMoveRefusal CheckDrop(NoteSet notes, NoteDrag drag, NoteDropSlot slot)
    {
        if (!CanDrag)
        {
            return NoteMoveRefusal.Busy;
        }

        if (drag.IsCategory)
        {
            return slot.Onto && notes.FindCategory(drag.Id)?.ParentId == slot.ParentId
                ? NoteMoveRefusal.Missing
                : notes.CheckCategoryMove(drag.Id, slot.ParentId);
        }

        return slot.ParentId == 0 || (slot.Onto && notes.OwnerOf(drag.Id)?.Id == slot.ParentId)
            ? NoteMoveRefusal.Missing
            : notes.CheckNoteMove(drag.Id, slot.ParentId);
    }

    public NoteDropResult Drop(NoteSet notes, NoteDrag drag, NoteDropSlot slot)
    {
        var refusal = CheckDrop(notes, drag, slot);
        if (refusal != NoteMoveRefusal.None)
        {
            return new NoteDropResult(NoteDropOutcome.Refused, refusal, 0);
        }

        var index = slot.Onto ? -1 : slot.Index;
        if (drag.IsCategory)
        {
            var clash = notes.Clash(drag.Id, slot.ParentId);
            if (clash != 0)
            {
                return new NoteDropResult(NoteDropOutcome.Clash, NoteMoveRefusal.None, clash);
            }

            notes.MoveCategory(drag.Id, slot.ParentId, index);
        }
        else
        {
            notes.MoveNote(drag.Id, slot.ParentId, index);
        }

        ExpandWithAncestors(notes, slot.ParentId);
        return new NoteDropResult(NoteDropOutcome.Moved, NoteMoveRefusal.None, 0);
    }

    public NoteImportResult MergeDropped(NoteSet notes, int categoryId, int targetId)
    {
        var result = NoteImport.MergeMoved(notes, categoryId, targetId);
        if (result.Status != NoteImportStatus.Ok)
        {
            return result;
        }

        CurrentCategoryId = targetId;
        if (notes.FindNote(SelectedNoteId) == null)
        {
            SelectedNoteId = 0;
        }

        ExpandWithAncestors(notes, notes.FindCategory(targetId)!.ParentId);
        return result;
    }

    public bool RenameDropped(NoteSet notes, int categoryId, NoteDropSlot slot, string name)
    {
        var wanted = name.Trim();
        if (wanted.Length == 0 || notes.FindNamed(slot.ParentId, wanted, notes.Categories.Count, categoryId) != null
            || !notes.MoveCategory(categoryId, slot.ParentId, slot.Onto ? -1 : slot.Index))
        {
            return false;
        }

        notes.RenameCategory(categoryId, wanted);
        ExpandWithAncestors(notes, slot.ParentId);
        return true;
    }

    // A press near an end of the path being drawn or edited starts from that end, so segments join up.
    public void PressLeft(WorldMapPanel panel, double panelX, double panelY)
    {
        var (tileX, tileY, onWorld) = panel.View.NearestTileUnder(panel.Width, panel.Height, panelX, panelY);
        pressOnWorld = onWorld;
        pressTile = SnapToDrawnEnd(panel, tileX, tileY);
        leftHeld = true;
        leftDragged = false;
        leftPressedAt = (panelX, panelY);
    }

    // Dragging a pin or text being edited carries it along.
    public void MoveMouse(WorldMapPanel panel, double panelX, double panelY)
    {
        if (rightHeld && double.Hypot(panelX - rightPressedAt.X, panelY - rightPressedAt.Y) > ClickSlack)
        {
            rightDragged = true;
        }

        if (!leftHeld)
        {
            return;
        }

        if (double.Hypot(panelX - leftPressedAt.X, panelY - leftPressedAt.Y) > ClickSlack)
        {
            leftDragged = true;
        }

        if (leftDragged && Edited != null && Edited.Kind != NoteKind.Path)
        {
            var (tileX, tileY, _) = panel.View.NearestTileUnder(panel.Width, panel.Height, panelX, panelY);
            Edited = Edited with { TileX = tileX, TileY = tileY };
        }
    }

    public void ReleaseLeft(NoteSet notes, WorldMapPanel panel, double panelX, double panelY)
    {
        if (!leftHeld)
        {
            return;
        }

        MoveMouse(panel, panelX, panelY);
        leftHeld = false;
        if (Edited != null)
        {
            FinishEditPress(notes, panel, panelX, panelY);
            return;
        }

        switch (Tool)
        {
            case WorldMapTool.Select:
                if (!leftDragged)
                {
                    Select(
                        panel.View.HitTest(notes, panel.Width, panel.Height, panelX, panelY, SelectRadius)?.NoteId
                        ?? 0);
                }

                break;
            case WorldMapTool.Pin or WorldMapTool.Text:
                if (!leftDragged && pressOnWorld)
                {
                    AskForPointName(Tool == WorldMapTool.Pin ? NoteLabelPurpose.Pin : NoteLabelPurpose.Text);
                }

                break;
            case WorldMapTool.Line:
                if (leftDragged && pressOnWorld)
                {
                    AddPendingSegment(notes, EndUnder(panel, panelX, panelY));
                }

                break;
            case WorldMapTool.Pan:
                break;
            default:
                throw new UnreachableException();
        }
    }

    public void PressRight(double panelX, double panelY)
    {
        rightHeld = true;
        rightDragged = false;
        rightPressedAt = (panelX, panelY);
    }

    // A right click on a note, not a right drag, opens the note's menu.
    public NoteHit? ReleaseRight(NoteSet notes, WorldMapPanel panel, double panelX, double panelY)
    {
        if (!rightHeld)
        {
            return null;
        }

        MoveMouse(panel, panelX, panelY);
        rightHeld = false;
        return rightDragged || leftHeld
            ? null
            : panel.View.HitTest(notes, panel.Width, panel.Height, panelX, panelY, SelectRadius);
    }

    // The segment a drag would add if the button were let go here.
    public PathSegment? Preview(WorldMapPanel panel, double panelX, double panelY)
    {
        if (!leftHeld || !leftDragged || !pressOnWorld || !DrawsSegments)
        {
            return null;
        }

        var (endX, endY) = EndUnder(panel, panelX, panelY);
        return new PathSegment(pressTile.X, pressTile.Y, endX, endY);
    }

    public int DrawingCategoryId(NoteSet notes)
    {
        if (Edited != null)
        {
            return notes.OwnerOf(Edited.Id)?.Id ?? 0;
        }

        return pending.Count > 0 ? PendingCategoryId : CurrentCategoryId;
    }

    public bool CanConfirmLabel(string label) => LabelPurpose != NoteLabelPurpose.Text || label.Length > 0;

    public void ConfirmLabel(NoteSet notes, string label)
    {
        var addedId = LabelPurpose switch
        {
            NoteLabelPurpose.Pin => notes.AddPoint(labelCategoryId, NoteKind.Pin, labelTile.X, labelTile.Y, label),
            NoteLabelPurpose.Text => notes.AddPoint(labelCategoryId, NoteKind.Text, labelTile.X, labelTile.Y, label),
            NoteLabelPurpose.Path => notes.AddPath(PendingCategoryId, pending.ToArray(), label),
            NoteLabelPurpose.None => -1,
            _ => throw new UnreachableException(),
        };
        if (LabelPurpose == NoteLabelPurpose.Path)
        {
            pending.Clear();
        }

        LabelPurpose = NoteLabelPurpose.None;
        if (addedId == 0)
        {
            Problem?.Invoke("Category is full.");
        }
    }

    // A path finished by putting down the Line tool has nowhere to go back to, so cancelling its name throws it away.
    public void CancelLabel()
    {
        if (LabelPurpose == NoteLabelPurpose.Path && Tool != WorldMapTool.Line)
        {
            pending.Clear();
        }

        LabelPurpose = NoteLabelPurpose.None;
    }

    public bool FinishPendingPath()
    {
        if (!Focused || Tool != WorldMapTool.Line || pending.Count == 0 || LabelPurpose != NoteLabelPurpose.None)
        {
            return false;
        }

        AskForPathName();
        return true;
    }

    public bool DiscardPendingPath()
    {
        if (Tool != WorldMapTool.Line || pending.Count == 0)
        {
            return false;
        }

        pending.Clear();
        return true;
    }

    // A path still being drawn outlives the map closing; going back to the Line tool lets Enter still finish it.
    public void DropInterruptedWork()
    {
        CancelEdit();
        if (LabelPurpose == NoteLabelPurpose.Path && pending.Count > 0)
        {
            Tool = WorldMapTool.Line;
        }

        LabelPurpose = NoteLabelPurpose.None;
        leftHeld = false;
        rightHeld = false;
    }

    private static PathSegment[] Without(IReadOnlyList<PathSegment> segments, int segmentIndex) =>
        segments.Where((_, index) => index != segmentIndex).ToArray();

    private (int X, int Y) SnapToDrawnEnd(WorldMapPanel panel, int tileX, int tileY) =>
        WorldMapView.SnapToEnd(
            DrawnSegments,
            tileX,
            tileY,
            Math.Max(1, SnapPixels / panel.View.PixelsPerTile)) ?? (tileX, tileY);

    private (int X, int Y) EndUnder(WorldMapPanel panel, double panelX, double panelY)
    {
        var (tileX, tileY, _) = panel.View.NearestTileUnder(panel.Width, panel.Height, panelX, panelY);
        return SnapToDrawnEnd(panel, tileX, tileY);
    }

    private void FinishEditPress(NoteSet notes, WorldMapPanel panel, double panelX, double panelY)
    {
        var editedPath = Edited!.Kind == NoteKind.Path;
        if (leftDragged)
        {
            if (editedPath && pressOnWorld)
            {
                AddEditedSegment(notes, EndUnder(panel, panelX, panelY));
            }

            return;
        }

        if (editedPath)
        {
            var segmentIndex = panel.View.HitSegment(Edited, panel.Width, panel.Height, panelX, panelY, SelectRadius);
            if (segmentIndex >= 0)
            {
                Edited = Edited with { Segments = Without(Edited.Segments, segmentIndex) };
                return;
            }
        }

        var hitId = panel.View.HitTest(notes, panel.Width, panel.Height, panelX, panelY, SelectRadius)?.NoteId ?? 0;
        if (hitId != 0 && hitId != Edited.Id)
        {
            Select(hitId);
        }
    }

    private void AddPendingSegment(NoteSet notes, (int X, int Y) end)
    {
        if (end == pressTile)
        {
            return;
        }

        if (pending.Count >= NoteLimits.SegmentsPerPath)
        {
            Problem?.Invoke("Path is full.");
            return;
        }

        if (pending.Count == 0)
        {
            PendingCategoryId = CurrentCategoryId;
        }

        if (!HasSegmentRoom(notes, PendingCategoryId, pending.Count + 1))
        {
            Problem?.Invoke("Category is full.");
            return;
        }

        pending.Add(new PathSegment(pressTile.X, pressTile.Y, end.X, end.Y));
    }

    private void AddEditedSegment(NoteSet notes, (int X, int Y) end)
    {
        var edited = Edited!;
        if (end == pressTile)
        {
            return;
        }

        if (edited.Segments.Count >= NoteLimits.SegmentsPerPath)
        {
            Problem?.Invoke("Path is full.");
            return;
        }

        var storedCount = notes.FindNote(edited.Id)?.Segments.Count ?? 0;
        if (notes.OwnerOf(edited.Id) is { } owner
            && !HasSegmentRoom(notes, owner.Id, edited.Segments.Count + 1 - storedCount))
        {
            Problem?.Invoke("Category is full.");
            return;
        }

        Edited = edited with
        {
            Segments = [.. edited.Segments, new PathSegment(pressTile.X, pressTile.Y, end.X, end.Y)],
        };
    }

    private static bool HasSegmentRoom(NoteSet notes, int categoryId, int extraSegments) =>
        notes.FindCategory(categoryId) is not { } category
        || category.SegmentTotal + extraSegments <= NoteLimits.SegmentsPerCategory;

    private void AskForPointName(NoteLabelPurpose purpose)
    {
        LabelPurpose = purpose;
        labelCategoryId = CurrentCategoryId;
        labelTile = pressTile;
    }

    private void AskForPathName()
    {
        if (pending.Count > 0)
        {
            LabelPurpose = NoteLabelPurpose.Path;
        }
    }
}
