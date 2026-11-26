using GdUnit4;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Notes;

[TestSuite]
public class NoteMovesTests
{
    private const int Last = -1;

    private NoteSet set = new();

    [BeforeTest]
    public void StartWithNoNotes() => set = new NoteSet();

    [TestCase]
    public void MoveNote_WithinItsCategory_Reorders()
    {
        var category = set.AddCategory("Camps", 0);
        var first = set.AddPoint(category, NoteKind.Pin, 1, 1, "a");
        var second = set.AddPoint(category, NoteKind.Pin, 2, 2, "b");
        var third = set.AddPoint(category, NoteKind.Pin, 3, 3, "c");

        AssertThat(set.MoveNote(third, category, 0)).IsTrue();

        AssertThat(NoteIds(category)).ContainsExactly(third, first, second);
    }

    [TestCase]
    public void MoveNote_ToAnotherCategory_LandsAtTheIndexAndKeepsItsId()
    {
        var from = set.AddCategory("From", 0);
        var to = set.AddCategory("To", 0);
        var moving = set.AddPoint(from, NoteKind.Pin, 1, 1, "moving");
        var first = set.AddPoint(to, NoteKind.Pin, 2, 2, "first");
        var second = set.AddPoint(to, NoteKind.Pin, 3, 3, "second");

        AssertThat(set.MoveNote(moving, to, 1)).IsTrue();

        AssertThat(NoteIds(to)).ContainsExactly(first, moving, second);
        AssertThat(set.FindCategory(from)!.Notes).IsEmpty();
    }

    [TestCase]
    public void MoveNote_ToLast_GoesAtTheEnd()
    {
        var from = set.AddCategory("From", 0);
        var to = set.AddCategory("To", 0);
        var moving = set.AddPoint(from, NoteKind.Pin, 1, 1, "moving");
        var there = set.AddPoint(to, NoteKind.Pin, 2, 2, "there");

        AssertThat(set.MoveNote(moving, to, Last)).IsTrue();

        AssertThat(NoteIds(to)).ContainsExactly(there, moving);
    }

    [TestCase]
    public void MoveNote_IntoAFullCategory_IsRefused()
    {
        var from = set.AddCategory("From", 0);
        var full = set.AddCategory("Full", 0);
        var moving = set.AddPoint(from, NoteKind.Pin, 1, 1, "moving");
        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            set.AddPoint(full, NoteKind.Pin, 1, 1, "");
        }

        AssertThat(set.CheckNoteMove(moving, full)).IsEqual(NoteMoveRefusal.CategoryFull);
        AssertThat(set.MoveNote(moving, full, Last)).IsFalse();
        AssertThat(set.OwnerOf(moving)!.Id).IsEqual(from);
    }

    [TestCase]
    public void MoveNote_OverTheSegmentLimit_IsRefused()
    {
        var from = set.AddCategory("From", 0);
        var busy = set.AddCategory("Busy", 0);
        var moving = set.AddPath(from, [new PathSegment(1, 1, 2, 2), new PathSegment(2, 2, 3, 3)], "moving");
        var segments = Enumerable.Range(0, NoteLimits.SegmentsPerPath)
            .Select(_ => new PathSegment(1, 1, 2, 2))
            .ToArray();
        for (var path = 0; path < NoteLimits.SegmentsPerCategory / NoteLimits.SegmentsPerPath; path++)
        {
            set.AddPath(busy, segments, "");
        }

        AssertThat(set.CheckNoteMove(moving, busy)).IsEqual(NoteMoveRefusal.CategoryFull);
    }

    [TestCase]
    public void CheckNoteMove_IntoItsOwnCategory_IsAllowed()
    {
        var category = set.AddCategory("Camps", 0);
        var pin = set.AddPoint(category, NoteKind.Pin, 1, 1, "a");

        AssertThat(set.CheckNoteMove(pin, category)).IsEqual(NoteMoveRefusal.None);
        AssertThat(set.CheckNoteMove(999, category)).IsEqual(NoteMoveRefusal.Missing);
    }

    [TestCase]
    public void MoveCategory_AmongSiblings_Reorders()
    {
        var first = set.AddCategory("A", 0);
        var second = set.AddCategory("B", 0);
        var third = set.AddCategory("C", 0);

        AssertThat(set.MoveCategory(third, 0, 0)).IsTrue();

        AssertThat(set.Tree().Select(category => category.Id)).ContainsExactly(third, first, second);
    }

    [TestCase]
    public void MoveCategory_ToANewParent_LandsAtTheIndex()
    {
        var parent = set.AddCategory("Parent", 0);
        var first = set.AddCategory("First", 0, parent);
        var second = set.AddCategory("Second", 0, parent);
        var moving = set.AddCategory("Moving", 0);
        var child = set.AddCategory("Child", 0, moving);

        AssertThat(set.MoveCategory(moving, parent, 1)).IsTrue();

        AssertThat(set.Tree().Select(category => category.Id))
            .ContainsExactly(parent, first, moving, child, second);
        AssertThat(set.FindCategory(moving)!.ParentId).IsEqual(parent);
    }

    [TestCase]
    public void CheckCategoryMove_IntoItsOwnSubcategory_IsIntoItself()
    {
        var parent = set.AddCategory("Parent", 0);
        var child = set.AddCategory("Child", 0, parent);

        AssertThat(set.CheckCategoryMove(parent, child)).IsEqual(NoteMoveRefusal.IntoItself);
        AssertThat(set.CheckCategoryMove(parent, parent)).IsEqual(NoteMoveRefusal.IntoItself);
        AssertThat(set.CheckCategoryMove(child, parent)).IsEqual(NoteMoveRefusal.None);
        AssertThat(set.CheckCategoryMove(child, 999)).IsEqual(NoteMoveRefusal.Missing);
    }

    [TestCase]
    public void CheckCategoryMove_PastEightLevels_IsTooDeep()
    {
        var deepest = 0;
        for (var level = 1; level <= NoteLimits.Depth; level++)
        {
            deepest = set.AddCategory("Level", 0, deepest);
        }

        var pair = set.AddCategory("Pair", 0);
        set.AddCategory("Under", 0, pair);

        AssertThat(set.CheckCategoryMove(pair, deepest)).IsEqual(NoteMoveRefusal.TooDeep);
        AssertThat(set.MoveCategory(pair, deepest, Last)).IsFalse();
    }

    [TestCase]
    public void Clash_FindsTheSameNameIgnoringCase()
    {
        var places = set.AddCategory("Places", 0);
        var there = set.AddCategory("Dungeons", 0, places);
        var moving = set.AddCategory("dungeons", 0);

        AssertThat(set.Clash(moving, places)).IsEqual(there);
        AssertThat(set.Clash(moving, 0)).IsEqual(0);
        AssertThat(set.Clash(there, places)).IsEqual(0);
    }

    private List<int> NoteIds(int categoryId) =>
        set.FindCategory(categoryId)!.Notes.Select(note => note.Id).ToList();
}
