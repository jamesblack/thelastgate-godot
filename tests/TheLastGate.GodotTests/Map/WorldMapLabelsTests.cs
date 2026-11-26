using GdUnit4;
using TheLastGate.Map;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class WorldMapLabelsTests
{
    private const double Width = 800;
    private const double Height = 600;

    private NoteSet notes = new();
    private WorldMapView view = new();

    [BeforeTest]
    public void StartWithNoNotes()
    {
        notes = new NoteSet();
        view = new WorldMapView { CentreTileX = 512, CentreTileY = 1024, PixelsPerTile = 4 };
    }

    [TestCase]
    public void PinCentre_IsTheMiddleOfItsTileOnWholePixels()
    {
        var pin = AddPin(512, 1024, "pin");

        AssertThat(WorldMapLabels.PinCentre(view, Width, Height, pin)).IsEqual((402f, 302f));
    }

    [TestCase]
    public void Place_PutsAPinsNameToTheRightOfItsMarker()
    {
        var pin = AddPin(512, 1024, "pin");

        var placed = Place();

        AssertThat(placed[pin.Id]).IsEqual(new LabelRect(410, 295, 451, 308));
    }

    [TestCase]
    public void Place_PutsAnEditedPinsNameBesideWhereItIsBeingMoved()
    {
        var pin = AddPin(512, 1024, "pin");

        var placed = WorldMapLabels.Place(notes, view, Width, Height, Measure, edited: pin with { TileY = 1034 });

        AssertThat(placed[pin.Id]).IsEqual(new LabelRect(450, 295, 491, 308));
    }

    [TestCase]
    public void Place_ShowsNoNamesWhenZoomedFarOut()
    {
        AddPin(512, 1024, "pin");
        view.PixelsPerTile = 2.6;

        AssertThat(Place()).IsEmpty();
        AssertThat(WorldMapLabels.NamesHidden(view)).IsTrue();
    }

    [TestCase]
    public void Place_KeepsNamesOffOtherPinsMarkers()
    {
        var first = AddPin(512, 1024, "first");
        AddPin(512, 1027, "second");

        AssertThat(Place()[first.Id].Left).IsEqual(353);
    }

    [TestCase]
    public void Place_KeepsNamesOffTextLabels()
    {
        var pin = AddPin(512, 1024, "pin");
        var text = notes.FindNote(notes.AddPoint(notes.Categories[0].Id, NoteKind.Text, 512, 1026, "note"))!;
        var (plate, _, _) = WorldMapLabels.TextPlate(view, Width, Height, text, Measure(text.Label));

        var placed = Place();

        AssertThat(placed[pin.Id].Overlaps(plate)).IsFalse();
    }

    [TestCase]
    public void Place_GivesTheSelectedPinTheFirstChoiceOfSpot()
    {
        var first = AddPin(512, 1024, "first");
        var second = AddPin(512, 1024, "second");

        AssertThat(Place()[first.Id].Left).IsEqual(410);
        AssertThat(Place(second.Id)[second.Id].Left).IsEqual(410);
        AssertThat(Place(second.Id)[first.Id].Left).IsEqual(353);
    }

    [TestCase]
    public void Place_SkipsPinsWithoutNamesOrWithNamesTurnedOff()
    {
        AddPin(512, 1024, "");
        var parent = notes.Categories[0].Id;
        var child = notes.AddCategory("Child", 0, parent);
        notes.AddPoint(child, NoteKind.Pin, 512, 1030, "hidden name");
        notes.FindCategory(parent)!.ShowLabels = false;

        AssertThat(Place()).IsEmpty();
    }

    [TestCase]
    public void Place_SkipsHiddenCategoriesButNotPinsFarOffThePanel()
    {
        var shown = notes.AddCategory("Shown", 0);
        var far = notes.AddPoint(shown, NoteKind.Pin, 512, 1024 + 200, "far");
        var nearby = notes.AddPoint(shown, NoteKind.Pin, 512, 1024 + 150, "nearby");
        var hidden = notes.AddCategory("Hidden", 0);
        notes.AddPoint(hidden, NoteKind.Pin, 512, 1024, "hidden");
        notes.FindCategory(hidden)!.Visible = false;

        AssertThat(Place().Keys.Order()).ContainsExactly(far, nearby);
    }

    // A pin past the edge of the panel takes room beside its neighbour whether or not it is in sight.
    [TestCase(0.0)]
    [TestCase(10.0)]
    [TestCase(-37.3)]
    [TestCase(0.6)]
    public void Panning_LeavesEveryNameWhereItWasBesideItsPin(double panTiles)
    {
        var edge = AddPin(512, 1024 + 160, "edge");
        AddPin(512, 1024 + 166, "beyond");
        view.CentreTileY += panTiles;

        var name = Place()[edge.Id];

        var (centreX, centreY) = WorldMapLabels.PinCentre(view, Width, Height, edge);
        AssertThat((name.Left - centreX, name.Top - centreY)).IsEqual((-49f, -7f));
    }

    [TestCase]
    public void PinOnPanel_KeepsAPinInSight()
    {
        var pin = AddPin(512, 1024, "pin");

        AssertThat(WorldMapLabels.PinOnPanel(view, Width, Height, pin, null)).IsTrue();
    }

    [TestCase]
    public void PinOnPanel_DropsAPinPastTheEdge()
    {
        var pin = AddPin(512, 1024 + 110, "pin");

        AssertThat(WorldMapLabels.PinOnPanel(view, Width, Height, pin, new LabelRect(8, -7, 40, 5))).IsFalse();
    }

    [TestCase]
    public void PinOnPanel_KeepsAPinWhoseNameReachesIn()
    {
        var pin = AddPin(512, 1024 + 110, "pin");

        AssertThat(WorldMapLabels.PinOnPanel(view, Width, Height, pin, new LabelRect(-60, -7, -10, 5))).IsTrue();
    }

    [TestCase]
    public void Layout_PlacesNamesAroundTheirPinsWithoutThePanel()
    {
        var pin = AddPin(512, 1024, "pin");

        var layout = WorldMapLabels.Layout(notes, view.PixelsPerTile, Measure);

        AssertThat(layout[pin.Id]).IsEqual(new LabelRect(8, -7, 49, 6));
    }

    [TestCase]
    public void TextPlate_CentresTheTextOnItsTileWithPadding()
    {
        var text = notes.FindNote(notes.AddPoint(notes.AddCategory("Text", 0), NoteKind.Text, 512, 1026, "note"))!;

        var (plate, textX, textY) = WorldMapLabels.TextPlate(view, Width, Height, text, (40, 12));

        AssertThat((textX, textY)).IsEqual((390f, 296f));
        AssertThat(plate).IsEqual(new LabelRect(386, 294, 434, 310));
    }

    private static (float Width, float Height) Measure(string text) => (40, 12);

    private Note AddPin(int tileX, int tileY, string label)
    {
        var category = notes.Categories.Count > 0 ? notes.Categories[0].Id : notes.AddCategory("Pins", 0);
        return notes.FindNote(notes.AddPoint(category, NoteKind.Pin, tileX, tileY, label))!;
    }

    private IReadOnlyDictionary<int, LabelRect> Place(int selectedNoteId = 0) =>
        WorldMapLabels.Place(notes, view, Width, Height, Measure, selectedNoteId);
}
