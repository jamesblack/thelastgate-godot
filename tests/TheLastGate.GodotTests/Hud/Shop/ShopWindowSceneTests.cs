using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Hud.Shop;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Shop;

[TestSuite]
[RequireGodotRuntime]
public class ShopWindowSceneTests
{
    private const string ScenePath = "res://Hud/Shop/shop_window.tscn";
    private const int Columns = 8;
    private const int LastRow = 56;

    private ShopWindowView window = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        window = GD.Load<PackedScene>(ScenePath).Instantiate<ShopWindowView>();
        Tree.Root.AddChild(window);
        window.Visible = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => window.Free();

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>(ScenePath).Instantiate<ShopWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void Cell_IsEveryGridCellInOrder()
    {
        for (var cell = 0; cell < ShopClicks.Slots; cell++)
        {
            AssertThat(window.Cell(cell).GetParent().Name.ToString()).IsEqual($"Cell{cell}");
        }
    }

    [TestCase]
    public void Cells_ShareTheirLines()
    {
        AssertThat(Edges(0)).IsEqual(ProceduralEdges.Right | ProceduralEdges.Bottom);
        AssertThat(Edges(Columns - 1)).IsEqual(ProceduralEdges.Bottom);
        AssertThat(Edges(LastRow)).IsEqual(ProceduralEdges.Right);
        AssertThat(Edges(ShopClicks.Slots - 1)).IsEqual(ProceduralEdges.Right);
    }

    [TestCase]
    public void ThePlacesPastTheLastSlot_AreOneBlankBox()
    {
        var grid = window.GetNode<Control>("%Grid");
        AssertThat(grid.GetChildCount()).IsEqual(Columns * Columns);
        for (var place = ShopClicks.Slots; place < Columns * Columns; place++)
        {
            var blank = grid.GetChild<Control>(place);
            AssertThat(blank.FindChildren("*", nameof(ItemSlotView), true, false)).IsEmpty();
            AssertThat(blank.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        }
    }

    [TestCase]
    public void TheEditorPreview_IsNotSavedIntoTheScene()
    {
        var saved = GD.Load<PackedScene>(ScenePath).Instantiate<ShopWindowView>();

        AssertThat(saved.Cell(0).PreviewSprite).IsEqual(0);
        saved.Free();
    }

    [TestCase]
    public void TheGrid_Is263PixelsSquare() =>
        AssertThat(window.GetNode<Control>("%Grid").Size).IsEqual(new Vector2(263, 263));

    [TestCase]
    public void OnlyTheContentFades()
    {
        AssertThat(Panel("%Items").FadesAs).IsEqual(WindowBackground.Shop);
        AssertThat(Panel("%Footer").FadesAs).IsEqual(WindowBackground.Shop);
        AssertThat(Panel("Window/Frame").FadesAs).IsEqual(WindowBackground.None);
    }

    [TestCase]
    public void TheFooter_SharesTheFramesBorder() =>
        AssertThat(Panel("%Footer").Edges).IsEqual(ProceduralEdges.Top);

    [TestCase]
    public void CloseButton_IsTheSharedCloseButton() =>
        AssertThat(window.CloseButton.SceneFilePath).IsEqual("res://Procedural/close_button.tscn");

    private ProceduralEdges Edges(int cell) => ((ProceduralPanel)window.Cell(cell).GetParent()).Edges;

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(window.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    private ProceduralPanel Panel(string path) => window.GetNode<ProceduralPanel>(path);
}
