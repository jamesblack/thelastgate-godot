using GdUnit4;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class WorldMapDropTests
{
    private const int Last = -1;

    private WorldMapEditor editor = null!;
    private NoteSet notes = new();

    [BeforeTest]
    public void OpenAnEmptyEditor()
    {
        notes = new NoteSet();
        editor = new WorldMapWindow().Editor;
    }

    [TestCase]
    public void Drop_WhileEditing_IsRefused()
    {
        var from = notes.AddCategory("From", 0);
        var to = notes.AddCategory("To", 0);
        var pin = notes.AddPoint(from, NoteKind.Pin, 1, 1, "Camp");
        editor.BeginEdit(notes, pin);

        var result = editor.Drop(notes, new NoteDrag(pin, false), Onto(to));

        AssertThat(editor.CanDrag).IsFalse();
        AssertThat(result.Outcome).IsEqual(NoteDropOutcome.Refused);
        AssertThat(result.Refusal).IsEqual(NoteMoveRefusal.Busy);
        AssertThat(notes.OwnerOf(pin)!.Id).IsEqual(from);
    }

    [TestCase]
    public void Drop_NoteOntoACategory_GoesLast()
    {
        var from = notes.AddCategory("From", 0);
        var to = notes.AddCategory("To", 0);
        var there = notes.AddPoint(to, NoteKind.Pin, 2, 2, "There");
        var pin = notes.AddPoint(from, NoteKind.Pin, 1, 1, "Camp");

        var result = editor.Drop(notes, new NoteDrag(pin, false), Onto(to));

        AssertThat(result.Outcome).IsEqual(NoteDropOutcome.Moved);
        AssertThat(notes.FindCategory(to)!.Notes.Select(note => note.Id)).ContainsExactly(there, pin);
    }

    [TestCase]
    public void Drop_OntoItsOwnParent_IsNotATarget()
    {
        var parent = notes.AddCategory("Parent", 0);
        var child = notes.AddCategory("Child", 0, parent);
        var pin = notes.AddPoint(parent, NoteKind.Pin, 1, 1, "Camp");

        AssertThat(editor.CheckDrop(notes, new NoteDrag(child, true), Onto(parent)))
            .IsEqual(NoteMoveRefusal.Missing);
        AssertThat(editor.CheckDrop(notes, new NoteDrag(pin, false), Onto(parent)))
            .IsEqual(NoteMoveRefusal.Missing);
    }

    [TestCase]
    public void Drop_OnALine_Reorders()
    {
        var first = notes.AddCategory("A", 0);
        var second = notes.AddCategory("B", 0);

        var result = editor.Drop(notes, new NoteDrag(second, true), new NoteDropSlot(0, 0, false));

        AssertThat(result.Outcome).IsEqual(NoteDropOutcome.Moved);
        AssertThat(notes.Tree().Select(category => category.Id)).ContainsExactly(second, first);
    }

    [TestCase]
    public void Drop_ANoteAtTheTopLevel_IsRefused()
    {
        var from = notes.AddCategory("From", 0);
        var pin = notes.AddPoint(from, NoteKind.Pin, 1, 1, "Camp");

        AssertThat(editor.CheckDrop(notes, new NoteDrag(pin, false), new NoteDropSlot(0, 0, false)))
            .IsEqual(NoteMoveRefusal.Missing);
    }

    [TestCase]
    public void Drop_CategoryWithAClash_ReportsTheClashAndMovesNothing()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);

        var result = editor.Drop(notes, new NoteDrag(moving, true), Onto(places));

        AssertThat(result.Outcome).IsEqual(NoteDropOutcome.Clash);
        AssertThat(result.ClashId).IsEqual(there);
        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(0);
    }

    [TestCase]
    public void MergeDropped_MakesTheTargetCurrent()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        editor.ChooseCategory(moving);

        var result = editor.MergeDropped(notes, moving, there);

        AssertThat(result.Status).IsEqual(NoteImportStatus.Ok);
        AssertThat(editor.CurrentCategoryId).IsEqual(there);
        AssertThat(editor.ExpandedCategories).Contains(places);
    }

    [TestCase]
    public void MergeDropped_ClearsASelectionThatWasMerged()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        var pin = notes.AddPoint(moving, NoteKind.Pin, 1, 1, "Boss");
        editor.Select(pin);

        editor.MergeDropped(notes, moving, there);

        AssertThat(editor.SelectedNoteId).IsEqual(0);
    }

    [TestCase]
    public void RenameDropped_KeepsIds()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        var pin = notes.AddPoint(moving, NoteKind.Pin, 1, 1, "Boss");

        AssertThat(editor.RenameDropped(notes, moving, Onto(places), "Old dungeons")).IsTrue();

        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(places);
        AssertThat(notes.FindCategory(moving)!.Name).IsEqual("Old dungeons");
        AssertThat(notes.OwnerOf(pin)!.Id).IsEqual(moving);
    }

    [TestCase]
    public void RenameDropped_ToATakenName_IsRefused()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);

        AssertThat(editor.RenameDropped(notes, moving, Onto(places), "dungeons")).IsFalse();
        AssertThat(editor.RenameDropped(notes, moving, Onto(places), "  ")).IsFalse();
        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(0);
    }

    [TestCase]
    public void Drop_ExpandsTheDestination()
    {
        var top = notes.AddCategory("Top", 0);
        var middle = notes.AddCategory("Middle", 0, top);
        var moving = notes.AddCategory("Moving", 0);

        editor.Drop(notes, new NoteDrag(moving, true), Onto(middle));

        AssertThat(editor.ExpandedCategories.Order()).ContainsExactly(top, middle);
    }

    private static NoteDropSlot Onto(int categoryId) => new(categoryId, Last, true);
}
