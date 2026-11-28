using GdUnit4;
using Godot;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class NoteListSourceTests
{
    private WorldMapEditor editor = new();
    private HashSet<int> expanded = [];
    private NoteSet notes = new();

    [BeforeTest]
    public void StartWithNoNotes()
    {
        notes = new NoteSet();
        editor = new WorldMapEditor();
        expanded = [];
    }

    [TestCase]
    public void Build_HidesCollapsedChildren()
    {
        var places = notes.AddCategory("Places", 0);
        notes.AddCategory("Dungeons", 0, places);
        notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        var quests = notes.AddCategory("Quests", 0);

        var rows = NoteListSource.Build(notes, expanded);

        AssertThat(rows.Select(row => row.Id)).ContainsExactly(places, quests);
    }

    [TestCase]
    public void Build_ShowsNotesAfterSubcategories()
    {
        var places = notes.AddCategory("Places", 0);
        var tavern = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        var dungeons = notes.AddCategory("Dungeons", 0, places);
        expanded.Add(places);

        var rows = NoteListSource.Build(notes, expanded);

        AssertThat(rows)
            .ContainsExactly(
                new CategoryRow(places, true, 0, 0, 0),
                new CategoryRow(dungeons, true, 1, places, 0),
                new CategoryRow(tavern, false, 1, places, 1));
    }

    [TestCase]
    public void BuildRows_FollowsTheEditorsExpandedCategories()
    {
        var places = notes.AddCategory("Places", 0);
        var tavern = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        editor.Expand(places, true);

        var rows = new NoteListSource(notes, editor).BuildRows();

        AssertThat(rows.Select(row => row.Id)).ContainsExactly(places, tavern);
    }

    [TestCase]
    public void Look_NoteWithItsCategorysColour_HasNoSwatchAndAPrefix()
    {
        var places = notes.AddCategory("Places", 2);
        var tavern = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");

        var look = new NoteListSource(notes, editor).Look(new CategoryRow(tavern, false, 1, places, 0));

        AssertThat(look.Text).IsEqual("Pin: Tavern");
        AssertThat(look.Swatch).IsNull();
    }

    [TestCase]
    public void Look_Category_HasItsNameAndColour()
    {
        var places = notes.AddCategory("Places", 2);

        var look = new NoteListSource(notes, editor).Look(new CategoryRow(places, true, 0, 0, 0));

        AssertThat(look.Text).IsEqual("Places");
        AssertThat(look.Swatch).IsEqual(Color.Color8(0xFF, 0x6B, 0x6B));
        AssertThat(look.State).IsEqual(CategoryRowState.Normal);
    }

    [TestCase]
    public void Look_PicksOutTheCurrentCategoryAndSelectedNote()
    {
        var places = notes.AddCategory("Places", 0);
        var tavern = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");
        editor.ChooseCategory(places);
        editor.Select(tavern);
        var source = new NoteListSource(notes, editor);

        AssertThat(source.Look(new CategoryRow(places, true, 0, 0, 0)).State).IsEqual(CategoryRowState.Current);
        AssertThat(source.Look(new CategoryRow(tavern, false, 1, places, 0)).State)
            .IsEqual(CategoryRowState.Selected);
    }

    [TestCase]
    public void Look_InsideAHiddenCategory_IsDimmed()
    {
        var places = notes.AddCategory("Places", 0);
        var shops = notes.AddCategory("Shops", 0, places);
        var armourer = notes.AddPoint(shops, NoteKind.Pin, 1, 1, "Armourer");
        notes.FindCategory(places)!.Visible = false;
        var source = new NoteListSource(notes, editor);

        AssertThat(source.Look(new CategoryRow(shops, true, 1, places, 0)).State).IsEqual(CategoryRowState.Dimmed);
        AssertThat(source.Look(new CategoryRow(armourer, false, 2, shops, 0)).State)
            .IsEqual(CategoryRowState.Dimmed);
    }

    [TestCase]
    public void Revision_ChangesWithTheNotes()
    {
        var source = new NoteListSource(notes, editor);
        var before = source.Revision;

        notes.AddCategory("Places", 0);

        AssertThat(source.Revision).IsNotEqual(before);
    }

    [TestCase]
    public void Drop_WithAClash_RaisesClashFoundAndMovesNothing()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        var source = new NoteListSource(notes, editor);
        var clashes = new List<int>();
        source.ClashFound += (_, _, clashId) => clashes.Add(clashId);

        var moved = source.Drop(new CategoryDrag(moving, true), new CategoryDropSlot(places, -1, true));

        AssertThat(moved).IsFalse();
        AssertThat(clashes).ContainsExactly(there);
        AssertThat(notes.FindCategory(moving)!.ParentId).IsEqual(0);
    }

    [TestCase]
    public void Drop_ThatMoves_ReturnsTrue()
    {
        var first = notes.AddCategory("A", 0);
        var second = notes.AddCategory("B", 0);
        var source = new NoteListSource(notes, editor);

        AssertThat(source.Drop(new CategoryDrag(second, true), new CategoryDropSlot(0, 0, false))).IsTrue();
        AssertThat(notes.Tree().Select(category => category.Id)).ContainsExactly(second, first);
    }
}
