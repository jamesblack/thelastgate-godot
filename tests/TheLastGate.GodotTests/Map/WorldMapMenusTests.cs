using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class WorldMapMenusTests
{
    private static readonly Vector2 Somewhere = new(100, 100);

    private NoteSet notes = new();
    private WorldMapWindowView view = null!;
    private WorldMapWindow window = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private WorldMapCategories Categories => view.GetNode<WorldMapCategories>("%Categories");

    private ProceduralMenu CategoryMenu => view.GetNode<ProceduralMenu>("%Menu");

    private ProceduralMenu NoteMenu => view.GetNode<ProceduralMenu>("%NoteMenu");

    [BeforeTest]
    public async Task ShowTheMap()
    {
        notes = new NoteSet();
        window = new WorldMapWindow();
        window.OpenAtPlayer();
        view = GD.Load<PackedScene>("res://Map/world_map_window.tscn").Instantiate<WorldMapWindowView>();
        Tree.Root.AddChild(view);
        view.ShowWindow(window, null, notes, (0, 0));
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheMap() => view.Free();

    [TestCase]
    public void CategoryMenu_HasTheAgreedItems()
    {
        var places = notes.AddCategory("Places", 0);

        Categories.OpenCategoryMenu(places, Somewhere);

        AssertThat(Texts(CategoryMenu))
            .ContainsExactly(
                "Places",
                "Show on map",
                "Show labels",
                "New subcategory...",
                "Import here...",
                "Rename",
                "Colour",
                "Export",
                "Remove duplicate pins...",
                "Delete category...");
    }

    [TestCase]
    public void CategoryMenu_HasNoMoveOrSendTo()
    {
        var places = notes.AddCategory("Places", 0);

        Categories.OpenCategoryMenu(places, Somewhere);

        AssertThat(Texts(CategoryMenu)).NotContains("Move up", "Move down", "Send to");
    }

    [TestCase]
    public void NoteMenu_HasTheAgreedItems()
    {
        var places = notes.AddCategory("Places", 0);
        var pin = notes.AddPoint(places, NoteKind.Pin, 1, 1, "Tavern");

        view.OpenNoteMenu(new NoteHit(pin, -1), Somewhere);

        AssertThat(Texts(NoteMenu)).ContainsExactly("Edit", "Colour", "Delete...");
    }

    [TestCase]
    public void ShowOnMap_KeepsTheMenuOpenAndHidesTheCategory()
    {
        var places = notes.AddCategory("Places", 0);
        Categories.OpenCategoryMenu(places, Somewhere);

        var row = Rows(CategoryMenu).First(row => Text(row) == "Show on map");
        row.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(CategoryMenu.IsOpen).IsTrue();
        AssertThat(notes.FindCategory(places)!.Visible).IsFalse();
    }

    [TestCase]
    public void ChoosingAColour_RecoloursTheCategoryAndCloses()
    {
        var places = notes.AddCategory("Places", 0);
        Categories.OpenCategoryMenu(places, Somewhere);

        var picker = view.GetNode<ProceduralMenu>("%ColourMenu")
            .FindChildren("*", nameof(GridContainer), true, false)
            .Cast<ColourPicker>()
            .Single();
        picker.GetNode<ClickArea>("Swatch5").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(notes.FindCategory(places)!.ColourIndex).IsEqual(5);
        AssertThat(CategoryMenu.IsOpen).IsFalse();
    }

    private static IEnumerable<Node> Rows(ProceduralMenu menu) =>
        menu.GetNode("Frame/Items").GetChildren().Where(row => row.HasNode("Cells/Label"));

    private static string Text(Node row) => row.GetNode<HudLabel>("Cells/Label").Text;

    private static List<string> Texts(ProceduralMenu menu) => Rows(menu).Select(Text).ToList();
}
