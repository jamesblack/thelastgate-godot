using GdUnit4;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class WorldMapEditorTests
{
    private const double Width = 800;
    private const double Height = 600;

    private WorldMapEditor editor = null!;
    private NoteSet notes = new();
    private WorldMapPanel panel;
    private List<string> problems = [];
    private WorldMapView view = new();
    private WorldMapWindow window = new();

    [BeforeTest]
    public void OpenAnEmptyEditor()
    {
        notes = new NoteSet();
        problems = [];
        view = new WorldMapView { CentreTileX = 512, CentreTileY = 1024, PixelsPerTile = 4 };
        window = new WorldMapWindow();
        editor = window.Editor;
        editor.Problem += problems.Add;
        panel = new WorldMapPanel(view, Width, Height);
    }

    [TestCase]
    public void NearestTileUnder_HoldsACursorOffTheWorldToItsEdge()
    {
        var (offX, offY) = view.TileToPanel(Width, Height, -5.5, 1000.5);
        var (onX, onY) = view.TileToPanel(Width, Height, 500.5, 1000.5);

        AssertThat(view.NearestTileUnder(Width, Height, offX, offY)).IsEqual((0, 1000, false));
        AssertThat(view.NearestTileUnder(Width, Height, onX, onY)).IsEqual((500, 1000, true));
    }

    [TestCase]
    public void SnapToEnd_FindsTheNearestEndWithinReach()
    {
        PathSegment[] segments = [new(90, 100, 100, 140), new(100, 140, 120, 160)];

        AssertThat(WorldMapView.SnapToEnd(segments, 101, 141, 2)).IsEqual((100, 140));
        AssertThat(WorldMapView.SnapToEnd(segments, 101, 141, 1)).IsNull();
    }

    [TestCase]
    public void EnsureCurrentCategory_PicksTheFirstCategory()
    {
        var first = notes.AddCategory("First", 0);
        notes.AddCategory("Second", 1);

        editor.EnsureCurrentCategory(notes);

        AssertThat(editor.CurrentCategoryId).IsEqual(first);
    }

    [TestCase]
    public void EnsureCurrentCategory_PutsDownADrawingToolWithNoCategoryToDrawInto()
    {
        editor.ChooseTool(WorldMapTool.Pin);

        editor.EnsureCurrentCategory(notes);

        AssertThat(editor.CurrentCategoryId).IsEqual(0);
        AssertThat(editor.Tool).IsEqual(WorldMapTool.Select);
    }

    [TestCase]
    public void PinTool_AClickAsksForANameThenAddsThePin()
    {
        var category = UseCategory();
        editor.ChooseTool(WorldMapTool.Pin);

        Click(500, 1000);
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.Pin);
        editor.ConfirmLabel(notes, "Camp");

        var pins = notes.FindCategory(category)!.Notes;
        AssertThat(pins).HasSize(1);
        var pin = pins[0];
        AssertThat((pin.Kind, pin.Label, pin.TileX, pin.TileY)).IsEqual((NoteKind.Pin, "Camp", 500, 1000));
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.None);
    }

    [TestCase(2, NoteLabelPurpose.Pin)]
    [TestCase(3, NoteLabelPurpose.None)]
    public void PinTool_StillPlacesAPinWhenTheMouseMovesACoupleOfPixels(double nudge, NoteLabelPurpose asked)
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Pin);

        Click(500, 1000, nudge);

        AssertThat(editor.LabelPurpose).IsEqual(asked);
    }

    [TestCase]
    public void PinTool_PlacesNothingOffTheWorld()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Pin);

        Click(-5, 1000);

        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.None);
    }

    [TestCase]
    public void TextTool_NeedsSomeText()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Text);

        Click(500, 1000);

        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.Text);
        AssertThat(editor.CanConfirmLabel("")).IsFalse();
        AssertThat(editor.CanConfirmLabel("Here")).IsTrue();
    }

    [TestCase]
    public void APinMayHaveNoName()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Pin);

        Click(500, 1000);

        AssertThat(editor.CanConfirmLabel("")).IsTrue();
    }

    [TestCase]
    public void CancelLabel_AddsNothing()
    {
        var category = UseCategory();
        editor.ChooseTool(WorldMapTool.Pin);
        Click(500, 1000);

        editor.CancelLabel();

        AssertThat(notes.FindCategory(category)!.Notes).IsEmpty();
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.None);
    }

    [TestCase]
    public void ConfirmLabel_SaysWhenTheCategoryIsFull()
    {
        var category = UseCategory();
        for (var count = 0; count < NoteLimits.NotesPerCategory; count++)
        {
            notes.AddPoint(category, NoteKind.Pin, 10, count, "");
        }

        editor.ChooseTool(WorldMapTool.Pin);
        Click(500, 1000);
        editor.ConfirmLabel(notes, "Camp");

        AssertThat(problems).ContainsExactly("Category is full.");
    }

    [TestCase]
    public void LineTool_ADragAddsASegmentToThePathBeingDrawn()
    {
        var category = UseCategory();
        editor.ChooseTool(WorldMapTool.Line);

        Drag(500, 1000, 500, 1010);

        AssertThat(editor.PendingSegments).ContainsExactly(new PathSegment(500, 1000, 500, 1010));
        AssertThat(editor.PendingCategoryId).IsEqual(category);
        AssertThat(notes.FindCategory(category)!.Notes).IsEmpty();
    }

    [TestCase]
    public void LineTool_SnapsBothEndsOfASegmentToTheEndsAlreadyDrawn()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);

        Drag(501, 1011, 501, 999);

        AssertThat(editor.PendingSegments[1]).IsEqual(new PathSegment(500, 1010, 500, 1000));
    }

    [TestCase]
    public void LineTool_AClickAddsNoSegment()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);

        Click(500, 1000, 2);

        AssertThat(editor.PendingSegments).IsEmpty();
    }

    [TestCase]
    public void LineTool_SaysWhenTheCategoryHasNoRoomForAnotherSegment()
    {
        var category = UseCategory();
        var longPath = Enumerable.Range(0, NoteLimits.SegmentsPerPath)
            .Select(step => new PathSegment(5, step, 6, step))
            .ToArray();
        for (var count = 0; count < NoteLimits.SegmentsPerCategory / NoteLimits.SegmentsPerPath; count++)
        {
            notes.AddPath(category, longPath, "");
        }

        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);

        AssertThat(editor.PendingSegments).IsEmpty();
        AssertThat(problems).ContainsExactly("Category is full.");
    }

    [TestCase]
    public void LineTool_SaysWhenThePathIsFull()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        for (var step = 0; step <= NoteLimits.SegmentsPerPath; step++)
        {
            var row = 10 + step / 100 * 10;
            var column = 10 + step % 100 * 5;
            Drag(row, column, row + 3, column);
        }

        AssertThat(editor.PendingSegments.Count).IsEqual(NoteLimits.SegmentsPerPath);
        AssertThat(problems).ContainsExactly("Path is full.");
    }

    [TestCase]
    public void Preview_ShowsTheSegmentBeingDraggedOutToWhereItWouldSnap()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);
        var (pressX, pressY) = At(505, 1005);
        var (mouseX, mouseY) = At(501, 1011);

        editor.PressLeft(panel, pressX, pressY);
        AssertThat(editor.Preview(panel, pressX, pressY)).IsNull();
        editor.MoveMouse(panel, mouseX, mouseY);

        AssertThat(editor.Preview(panel, mouseX, mouseY)).IsEqual(new PathSegment(505, 1005, 500, 1010));
    }

    [TestCase]
    public void Preview_IsOnlyForDrawingSegments()
    {
        UseCategory();
        var (pressX, pressY) = At(505, 1005);
        var (mouseX, mouseY) = At(510, 1005);

        editor.PressLeft(panel, pressX, pressY);
        editor.MoveMouse(panel, mouseX, mouseY);

        AssertThat(editor.Preview(panel, mouseX, mouseY)).IsNull();
    }

    [TestCase]
    public void FinishPendingPath_AsksForTheNameThenAddsThePath()
    {
        var category = UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);
        Drag(500, 1010, 510, 1010);
        editor.Focused = true;

        AssertThat(editor.FinishPendingPath()).IsTrue();
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.Path);
        editor.ConfirmLabel(notes, "Road");

        var roads = notes.FindCategory(category)!.Notes;
        AssertThat(roads).HasSize(1);
        var road = roads[0];
        AssertThat(road.Label).IsEqual("Road");
        AssertThat(road.Segments)
            .ContainsExactly(new PathSegment(500, 1000, 500, 1010), new PathSegment(500, 1010, 510, 1010));
        AssertThat(editor.PendingSegments).IsEmpty();
    }

    [TestCase]
    public void FinishPendingPath_WaitsForTheMapToHaveFocus()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);
        editor.Focused = false;

        AssertThat(editor.FinishPendingPath()).IsFalse();
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.None);
    }

    [TestCase]
    public void PuttingDownTheLineTool_AsksForThePathsName()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);

        editor.ChooseTool(WorldMapTool.Select);

        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.Path);
    }

    [TestCase]
    public void CancellingThatName_ThrowsThePathAway()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);
        editor.ChooseTool(WorldMapTool.Select);

        editor.CancelLabel();

        AssertThat(editor.PendingSegments).IsEmpty();
    }

    [TestCase]
    public void DiscardPendingPath_ThrowsAwayThePathBeingDrawn()
    {
        UseCategory();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);

        AssertThat(editor.DiscardPendingPath()).IsTrue();
        AssertThat(editor.PendingSegments).IsEmpty();
        AssertThat(editor.DiscardPendingPath()).IsFalse();
    }

    [TestCase]
    public void Escape_ThrowsAwayAPathBeingDrawnBeforeClosingTheMap()
    {
        var map = new WorldMapWindow();
        map.OpenAtPlayer();
        UseCategory(map.Editor);
        map.Editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010, map.Editor);

        AssertThat(map.CloseByEscape()).IsTrue();
        AssertThat(map.IsOpen).IsTrue();
        AssertThat(map.Editor.PendingSegments).IsEmpty();

        AssertThat(map.CloseByEscape()).IsTrue();
        AssertThat(map.IsOpen).IsFalse();
    }

    [TestCase]
    public void Closing_KeepsAPathWaitingForItsNameAndGoesBackToTheLineTool()
    {
        UseCategory();
        window.OpenAtPlayer();
        editor.ChooseTool(WorldMapTool.Line);
        Drag(500, 1000, 500, 1010);
        editor.ChooseTool(WorldMapTool.Select);

        window.Close();

        AssertThat(editor.Tool).IsEqual(WorldMapTool.Line);
        AssertThat(editor.PendingSegments).HasSize(1);
        AssertThat(editor.LabelPurpose).IsEqual(NoteLabelPurpose.None);
    }

    [TestCase]
    public void Opening_GivesTheMapFocus()
    {
        window.OpenAtPlayer();

        AssertThat(editor.Focused).IsTrue();
    }

    [TestCase]
    public void SelectTool_AClickSelectsTheNoteUnderTheCursorOrNothing()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");

        Click(500, 1000, 2);
        AssertThat(editor.SelectedNoteId).IsEqual(pin);

        Click(520, 1000);
        AssertThat(editor.SelectedNoteId).IsEqual(0);
    }

    [TestCase]
    public void EditingAPin_DraggingMovesOnlyTheCopyUntilSaved()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.BeginEdit(notes, pin);

        Drag(500, 1000, 505, 1003);

        AssertThat((editor.Edited!.TileX, editor.Edited.TileY)).IsEqual((505, 1003));
        AssertThat((notes.FindNote(pin)!.TileX, notes.FindNote(pin)!.TileY)).IsEqual((500, 1000));

        editor.SaveEdit(notes);

        AssertThat(editor.Edited).IsNull();
        AssertThat((notes.FindNote(pin)!.TileX, notes.FindNote(pin)!.TileY)).IsEqual((505, 1003));
    }

    [TestCase]
    public void BeginEdit_PicksUpTheSelectToolAndSelectsTheNote()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.ChooseTool(WorldMapTool.Pin);

        editor.BeginEdit(notes, pin);

        AssertThat(editor.Tool).IsEqual(WorldMapTool.Select);
        AssertThat(editor.SelectedNoteId).IsEqual(pin);
    }

    [TestCase]
    public void CancelEdit_LeavesTheNoteAsItWas()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.BeginEdit(notes, pin);
        Drag(500, 1000, 505, 1003);
        editor.RenameEdited("Moved");

        editor.CancelEdit();

        AssertThat(editor.Edited).IsNull();
        AssertThat((notes.FindNote(pin)!.Label, notes.FindNote(pin)!.TileX, notes.FindNote(pin)!.TileY))
            .IsEqual(("Camp", 500, 1000));
    }

    [TestCase]
    public void RenameEdited_RenamesTheNoteWhenSaved()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.BeginEdit(notes, pin);

        editor.RenameEdited("Old camp");
        editor.SaveEdit(notes);

        AssertThat(notes.FindNote(pin)!.Label).IsEqual("Old camp");
    }

    [TestCase]
    public void CanSaveEdit_NotForTextWithNoText()
    {
        var category = UseCategory();
        var text = notes.AddPoint(category, NoteKind.Text, 500, 1000, "Here");
        editor.BeginEdit(notes, text);

        editor.RenameEdited("");

        AssertThat(editor.CanSaveEdit).IsFalse();
    }

    [TestCase]
    public void EditingAPath_ADragAddsASegmentAndAClickRemovesOne()
    {
        var category = UseCategory();
        var road = notes.AddPath(category, [new PathSegment(500, 1000, 500, 1010)], "Road");
        editor.BeginEdit(notes, road);

        Drag(500, 1010, 505, 1010);
        Click(500, 1005);

        AssertThat(editor.Edited!.Segments).ContainsExactly(new PathSegment(500, 1010, 505, 1010));
        AssertThat(notes.FindNote(road)!.Segments).HasSize(1);

        editor.SaveEdit(notes);

        AssertThat(notes.FindNote(road)!.Segments).ContainsExactly(new PathSegment(500, 1010, 505, 1010));
    }

    [TestCase]
    public void SavingAPathWithNoSegmentsLeft_DeletesIt()
    {
        var category = UseCategory();
        var road = notes.AddPath(category, [new PathSegment(500, 1000, 500, 1010)], "Road");
        editor.BeginEdit(notes, road);
        Click(500, 1005);

        editor.SaveEdit(notes);

        AssertThat(notes.FindNote(road)).IsNull();
        AssertThat(editor.SelectedNoteId).IsEqual(0);
    }

    [TestCase]
    public void WhileEditing_ClickingAnotherNoteSelectsItAndStopsEditing()
    {
        var category = UseCategory();
        var first = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "First");
        var second = notes.AddPoint(category, NoteKind.Pin, 520, 1000, "Second");
        editor.BeginEdit(notes, first);

        Click(520, 1000);

        AssertThat(editor.SelectedNoteId).IsEqual(second);
        AssertThat(editor.Edited).IsNull();
    }

    [TestCase]
    public void DrawingCategoryId_IsTheEditedPathsCategory()
    {
        UseCategory();
        var other = notes.AddCategory("Other", 1);
        var road = notes.AddPath(other, [new PathSegment(500, 1000, 500, 1010)], "Road");

        editor.BeginEdit(notes, road);

        AssertThat(editor.DrawingCategoryId(notes)).IsEqual(other);
    }

    [TestCase]
    public void Delete_RemovesTheNoteAndForgetsIt()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.BeginEdit(notes, pin);

        editor.Delete(notes, pin);

        AssertThat(notes.FindNote(pin)).IsNull();
        AssertThat(editor.Edited).IsNull();
        AssertThat(editor.SelectedNoteId).IsEqual(0);
    }

    [TestCase]
    public void DeleteSegment_LeavesTheRestOfThePath()
    {
        var category = UseCategory();
        var road = notes.AddPath(
            category,
            [new PathSegment(500, 1000, 500, 1010), new PathSegment(500, 1010, 510, 1010)],
            "Road");

        editor.DeleteSegment(notes, road, 0);

        AssertThat(notes.FindNote(road)!.Segments).ContainsExactly(new PathSegment(500, 1010, 510, 1010));
    }

    [TestCase]
    public void DeleteSegment_OfAOneSegmentPath_DeletesThePath()
    {
        var category = UseCategory();
        var road = notes.AddPath(category, [new PathSegment(500, 1000, 500, 1010)], "Road");

        editor.DeleteSegment(notes, road, 0);

        AssertThat(notes.FindNote(road)).IsNull();
    }

    [TestCase]
    public void Recolour_AlsoRecoloursTheEditedCopy()
    {
        var category = UseCategory();
        var pin = notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        editor.BeginEdit(notes, pin);

        editor.Recolour(notes, pin, 3);

        AssertThat(notes.FindNote(pin)!.ColourIndex).IsEqual(3);
        AssertThat(editor.Edited!.ColourIndex).IsEqual(3);
    }

    [TestCase]
    public void ARightClick_FindsTheNoteForItsMenu()
    {
        var category = UseCategory();
        var road = notes.AddPath(
            category,
            [new PathSegment(500, 1000, 500, 1010), new PathSegment(500, 1010, 510, 1010)],
            "Road");
        var (pointX, pointY) = At(505, 1010);

        editor.PressRight(pointX, pointY);
        editor.MoveMouse(panel, pointX + 2, pointY);

        AssertThat(editor.ReleaseRight(notes, panel, pointX + 2, pointY)).IsEqual(new NoteHit(road, 1));
    }

    [TestCase]
    public void ARightDrag_PansInsteadOfOpeningTheMenu()
    {
        var category = UseCategory();
        notes.AddPoint(category, NoteKind.Pin, 500, 1000, "Camp");
        var (pointX, pointY) = At(500, 1000);

        editor.PressRight(pointX, pointY);
        editor.MoveMouse(panel, pointX + 10, pointY);
        editor.MoveMouse(panel, pointX, pointY);

        AssertThat(editor.ReleaseRight(notes, panel, pointX, pointY)).IsNull();
    }

    private int UseCategory(WorldMapEditor? used = null)
    {
        var category = notes.Categories.Count > 0 ? notes.Categories[0].Id : notes.AddCategory("Notes", 0);
        (used ?? editor).EnsureCurrentCategory(notes);
        return category;
    }

    private (double X, double Y) At(int tileX, int tileY) => view.TileToPanel(Width, Height, tileX + 0.5, tileY + 0.5);

    private void Click(int tileX, int tileY, double nudge = 0)
    {
        var (pointX, pointY) = At(tileX, tileY);
        editor.PressLeft(panel, pointX, pointY);
        editor.MoveMouse(panel, pointX + nudge, pointY);
        editor.ReleaseLeft(notes, panel, pointX + nudge, pointY);
    }

    private void Drag(int fromX, int fromY, int toX, int toY, WorldMapEditor? used = null)
    {
        var dragging = used ?? editor;
        var (startX, startY) = At(fromX, fromY);
        var (endX, endY) = At(toX, toY);
        dragging.PressLeft(panel, startX, startY);
        dragging.MoveMouse(panel, endX, endY);
        dragging.ReleaseLeft(notes, panel, endX, endY);
    }
}
