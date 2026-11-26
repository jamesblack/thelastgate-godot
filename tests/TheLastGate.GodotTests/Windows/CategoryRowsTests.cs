using GdUnit4;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class CategoryRowsTests
{
    private const float Top = 0.1f;
    private const float Middle = 0.5f;

    private HashSet<int> expanded = [];
    private NoteSet notes = new();

    [BeforeTest]
    public void StartWithNoNotes()
    {
        notes = new NoteSet();
        expanded = [];
    }

    [TestCase]
    public void Build_HidesCollapsedChildren()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        var quests = notes.AddCategory("Quests", 0);

        var rows = CategoryRows.Build(notes, expanded);

        AssertThat(rows.Select(row => row.Id)).ContainsExactly(places, quests);
    }

    [TestCase]
    public void Build_ShowsNotesAfterSubcategories()
    {
        var places = notes.AddCategory("Places", 0);
        var tavern = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        var dungeons = notes.AddCategory("Dungeons", 0, places);
        expanded.Add(places);

        var rows = CategoryRows.Build(notes, expanded);

        AssertThat(rows)
            .ContainsExactly(
                new CategoryRow(places, true, 0, 0, 0),
                new CategoryRow(dungeons, true, 1, places, 0),
                new CategoryRow(tavern, false, 1, places, 1));
    }

    [TestCase]
    public void Slot_MiddleOfACategory_IsOnto()
    {
        var places = notes.AddCategory("Places", 0);
        var quests = notes.AddCategory("Quests", 0);

        AssertThat(Slot(new NoteDrag(quests, true), 0, Middle, 0)).IsEqual(new NoteDropSlot(places, -1, true));
    }

    [TestCase]
    public void Slot_MiddleOfANote_IsALine()
    {
        var places = notes.AddCategory("Places", 0);
        var first = notes.AddPoint(places, NoteKind.Pin, 1, 1, "First");
        notes.AddPoint(places, NoteKind.Pin, 2, 2, "Second");
        expanded.Add(places);

        AssertThat(Slot(new NoteDrag(first, false), 2, Middle + 0.1f, 1))
            .IsEqual(new NoteDropSlot(places, 1, false));
    }

    [TestCase]
    public void Slot_BetweenSiblings_IsBeforeTheLowerOne()
    {
        notes.AddCategory("A", 0);
        notes.AddCategory("B", 0);
        var third = notes.AddCategory("C", 0);

        AssertThat(Slot(new NoteDrag(third, true), 1, Top, 0)).IsEqual(new NoteDropSlot(0, 1, false));
        AssertThat(Slot(new NoteDrag(third, true), 0, Top, 0)).IsEqual(new NoteDropSlot(0, 0, false));
    }

    [TestCase]
    public void Slot_DownInItsOwnList_CountsWithoutItself()
    {
        var first = notes.AddCategory("A", 0);
        notes.AddCategory("B", 0);
        notes.AddCategory("C", 0);

        AssertThat(Slot(new NoteDrag(first, true), 2, Top, 0)).IsEqual(new NoteDropSlot(0, 1, false));
    }

    [TestCase]
    public void Slot_EndOfExpandedCategory_CursorPicksTheLevel()
    {
        var a = notes.AddCategory("A", 0);
        notes.AddCategory("a1", 0, a);
        notes.AddCategory("B", 0);
        var moving = notes.AddCategory("Moving", 0);
        expanded.Add(a);

        AssertThat(Slot(new NoteDrag(moving, true), 2, Top, 1)).IsEqual(new NoteDropSlot(a, 1, false));
        AssertThat(Slot(new NoteDrag(moving, true), 2, Top, 0)).IsEqual(new NoteDropSlot(0, 1, false));
    }

    [TestCase]
    public void Slot_AfterTheLastRow_CanBeTheTopLevelEnd()
    {
        var a = notes.AddCategory("A", 0);
        notes.AddCategory("a1", 0, a);
        var moving = notes.AddCategory("Moving", 0, a);
        expanded.Add(a);

        AssertThat(Slot(new NoteDrag(moving, true), 3, Top, 0)).IsEqual(new NoteDropSlot(0, 1, false));
        AssertThat(Slot(new NoteDrag(moving, true), 3, Top, 1)).IsEqual(new NoteDropSlot(a, 1, false));
    }

    [TestCase]
    public void Slot_CategoryNeverLandsAmongNotes()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        notes.AddPoint(places, NoteKind.Pin, 1, 1, "First");
        notes.AddPoint(places, NoteKind.Pin, 2, 2, "Second");
        var moving = notes.AddCategory("Moving", 0);
        expanded.Add(places);

        AssertThat(Slot(new NoteDrag(moving, true), 3, Top, 1)).IsNull();
        AssertThat(Slot(new NoteDrag(moving, true), 2, Top, 1)).IsEqual(new NoteDropSlot(places, 1, false));
    }

    [TestCase]
    public void Slot_NoteNeverLandsAmongSubcategories()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        notes.AddCategory("Shops", 0, places);
        var from = notes.AddCategory("From", 0);
        var pin = notes.AddPoint(from, NoteKind.Pin, 1, 1, "Camp");
        expanded.Add(places);

        AssertThat(Slot(new NoteDrag(pin, false), 2, Top, 1)).IsNull();
        AssertThat(Slot(new NoteDrag(pin, false), 3, Top, 1)).IsEqual(new NoteDropSlot(places, 0, false));
    }

    [TestCase]
    public void Slot_FirstChildOfAnExpandedCategory_IsInsideIt()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Moving", 0);
        expanded.Add(places);

        AssertThat(Slot(new NoteDrag(moving, true), 1, Top, 0)).IsEqual(new NoteDropSlot(places, 0, false));
    }

    private NoteDropSlot? Slot(NoteDrag drag, int rowIndex, float fractionDown, int cursorLevel) =>
        CategoryRows.Slot(CategoryRows.Build(notes, expanded), drag, rowIndex, fractionDown, cursorLevel);
}
