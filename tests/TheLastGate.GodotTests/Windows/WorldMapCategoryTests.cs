using GdUnit4;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class WorldMapCategoryTests
{
    private WorldMapEditor editor = null!;
    private NoteSet notes = new();
    private List<string> problems = [];
    private WorldMapWindow window = new();

    [BeforeTest]
    public void OpenAnEmptyEditor()
    {
        notes = new NoteSet();
        problems = [];
        window = new WorldMapWindow();
        editor = window.Editor;
        editor.Problem += problems.Add;
    }

    [TestCase]
    public void Expand_OpensAndClosesACategory()
    {
        var category = notes.AddCategory("Camps", 0);

        editor.Expand(category, true);
        AssertThat(editor.ExpandedCategories).Contains(category);

        editor.Expand(category, false);
        AssertThat(editor.ExpandedCategories).IsEmpty();
    }

    [TestCase]
    public void ChooseCategory_IsWhereNewNotesGo()
    {
        notes.AddCategory("First", 0);
        var second = notes.AddCategory("Second", 1);

        editor.ChooseCategory(second);
        editor.EnsureCurrentCategory(notes);

        AssertThat(editor.CurrentCategoryId).IsEqual(second);
    }

    [TestCase]
    public void CreateCategory_ChoosesItAndOpensItsParent()
    {
        var parent = notes.AddCategory("Parent", 0);

        var created = editor.CreateCategory(notes, "Child", 3, parent);

        AssertThat(notes.FindCategory(created)!.ParentId).IsEqual(parent);
        AssertThat(notes.FindCategory(created)!.ColourIndex).IsEqual(3);
        AssertThat(editor.CurrentCategoryId).IsEqual(created);
        AssertThat(editor.ExpandedCategories).Contains(parent);
    }

    [TestCase]
    public void CreateCategory_SaysWhenItWouldNestTooDeep()
    {
        var parent = 0;
        for (var depth = 0; depth < NoteLimits.Depth; depth++)
        {
            parent = notes.AddCategory($"Level {depth}", 0, parent);
        }

        AssertThat(editor.CreateCategory(notes, "Too deep", 0, parent)).IsEqual(0);
        AssertThat(problems).ContainsExactly("Categories can only nest 8 deep.");
    }

    [TestCase]
    public void CreateCategory_SaysWhenThereAreTooMany()
    {
        for (var count = 0; count < NoteLimits.Categories; count++)
        {
            notes.AddCategory($"Category {count}", 0);
        }

        AssertThat(editor.CreateCategory(notes, "One more", 0, 0)).IsEqual(0);
        AssertThat(problems).ContainsExactly("Too many categories.");
    }

    [TestCase]
    public void DeleteCategory_LetsGoOfEverythingInIt()
    {
        var kept = notes.AddCategory("Kept", 0);
        var doomed = notes.AddCategory("Doomed", 0);
        var pin = notes.AddPoint(doomed, NoteKind.Pin, 10, 10, "Camp");
        editor.ChooseCategory(doomed);
        editor.Expand(doomed, true);
        editor.BeginEdit(notes, pin);

        editor.DeleteCategory(notes, doomed);

        AssertThat(notes.FindCategory(doomed)).IsNull();
        AssertThat(editor.Edited).IsNull();
        AssertThat(editor.SelectedNoteId).IsEqual(0);
        AssertThat(editor.ExpandedCategories).IsEmpty();
        editor.EnsureCurrentCategory(notes);
        AssertThat(editor.CurrentCategoryId).IsEqual(kept);
    }

    [TestCase]
    public void DeleteCategory_ThrowsAwayAPathBeingDrawnIntoIt()
    {
        var doomed = notes.AddCategory("Doomed", 0);
        editor.EnsureCurrentCategory(notes);
        editor.ChooseTool(WorldMapTool.Line);
        DrawOneSegment();

        editor.DeleteCategory(notes, doomed);

        AssertThat(editor.PendingSegments).IsEmpty();
    }

    [TestCase]
    public void RemoveDuplicatePins_SaysHowManyWent()
    {
        var category = notes.AddCategory("Camps", 0);
        notes.AddPoint(category, NoteKind.Pin, 10, 10, "Camp");
        var copy = notes.AddPoint(category, NoteKind.Pin, 10, 10, "Camp");
        editor.Select(copy);

        AssertThat(editor.RemoveDuplicatePins(notes, category)).IsEqual(1);

        AssertThat(problems).ContainsExactly("Removed 1 duplicate pin.");
        AssertThat(editor.SelectedNoteId).IsEqual(0);
    }

    [TestCase]
    public void RemoveDuplicatePins_CountsInThePlural()
    {
        var category = notes.AddCategory("Camps", 0);
        for (var copies = 0; copies < 3; copies++)
        {
            notes.AddPoint(category, NoteKind.Pin, 10, 10, "Camp");
        }

        editor.RemoveDuplicatePins(notes, category);

        AssertThat(problems).ContainsExactly("Removed 2 duplicate pins.");
    }

    [TestCase]
    public void FinishImport_ChoosesAndOpensTheImportedCategory()
    {
        var parent = notes.AddCategory("Shared", 0);

        AssertThat(editor.FinishImport(new NoteImportResult(NoteImportStatus.Ok, 42, ""), parent)).IsTrue();

        AssertThat(editor.CurrentCategoryId).IsEqual(42);
        AssertThat(editor.ExpandedCategories.Order()).ContainsExactly(parent, 42);
    }

    [TestCase]
    public void FinishImport_ThatFailed_ChangesNothing()
    {
        AssertThat(editor.FinishImport(new NoteImportResult(NoteImportStatus.TooDeep, 0, ""), 0)).IsFalse();

        AssertThat(editor.ExpandedCategories).IsEmpty();
    }

    [TestCase]
    public void ShowNote_AsksTheMapToMoveToItAndStopsFollowing()
    {
        var category = notes.AddCategory("Camps", 0);
        var pin = notes.FindNote(notes.AddPoint(category, NoteKind.Pin, 300, 700, "Camp"))!;
        window.Follow = true;

        window.ShowNote(pin);

        AssertThat(window.MoveRequest).IsEqual((300, 700));
        AssertThat(window.Follow).IsFalse();
    }

    [TestCase]
    public void ShowNote_ForAPath_MovesToWhereItStarts()
    {
        var category = notes.AddCategory("Roads", 0);
        var road = notes.FindNote(notes.AddPath(category, [new PathSegment(40, 50, 60, 70)], "Road"))!;

        window.ShowNote(road);

        AssertThat(window.MoveRequest).IsEqual((40, 50));
    }

    private void DrawOneSegment()
    {
        var view = new WorldMapView { CentreTileX = 512, CentreTileY = 1024, PixelsPerTile = 4 };
        var panel = new WorldMapPanel(view, 800, 600);
        var (startX, startY) = view.TileToPanel(800, 600, 500.5, 1000.5);
        var (endX, endY) = view.TileToPanel(800, 600, 500.5, 1010.5);
        editor.PressLeft(panel, startX, startY);
        editor.MoveMouse(panel, endX, endY);
        editor.ReleaseLeft(notes, panel, endX, endY);
    }
}
