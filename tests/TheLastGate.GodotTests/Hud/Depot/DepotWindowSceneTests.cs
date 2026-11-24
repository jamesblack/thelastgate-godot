using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Hud.Depot;
using TheLastGate.Net;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
[RequireGodotRuntime]
public class DepotWindowSceneTests
{
    private const string ScenePath = "res://Hud/Depot/depot_window.tscn";
    private const int Columns = 8;
    private const int LastRow = 56;
    private const int TabColumns = 4;

    private DepotWindowView window = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        window = GD.Load<PackedScene>(ScenePath).Instantiate<DepotWindowView>();
        Tree.Root.AddChild(window);
        window.Visible = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => window.Free();

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>(ScenePath).Instantiate<DepotWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void Cell_IsEveryGridCellInOrder()
    {
        for (var cell = 0; cell < DepotClicks.Cells; cell++)
        {
            AssertThat(window.Cell(cell).GetParent().Name.ToString()).IsEqual($"Cell{cell}");
        }
    }

    [TestCase]
    public void Cells_ShareTheirLines()
    {
        AssertThat(CellEdges(0)).IsEqual(ProceduralEdges.Right | ProceduralEdges.Bottom);
        AssertThat(CellEdges(Columns - 1)).IsEqual(ProceduralEdges.Bottom);
        AssertThat(CellEdges(LastRow)).IsEqual(ProceduralEdges.Right);
        AssertThat(CellEdges(DepotClicks.Cells - 1)).IsEqual(ProceduralEdges.None);
    }

    [TestCase]
    public void TheGrid_Is263PixelsSquare() =>
        AssertThat(window.GetNode<Control>("%Grid").Size).IsEqual(new Vector2(263, 263));

    [TestCase]
    public void PageButton_IsEveryTabInOrder()
    {
        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            AssertThat(Tab(page).Name.ToString()).IsEqual($"Page{page}");
        }
    }

    [TestCase]
    public void Tabs_ShareTheirLines()
    {
        AssertThat(Tab(0).Edges).IsEqual(ProceduralEdges.Right | ProceduralEdges.Bottom);
        AssertThat(Tab(TabColumns - 1).Edges).IsEqual(ProceduralEdges.Bottom);
        AssertThat(Tab(TabColumns).Edges).IsEqual(ProceduralEdges.Right);
        AssertThat(Tab(DepotPackets.Pages - 1).Edges).IsEqual(ProceduralEdges.None);
    }

    [TestCase]
    public void TheTabs_AreAsWideAsTheGrid() =>
        AssertThat(window.GetNode<Control>("%Tabs").Size.X).IsEqual(263);

    [TestCase]
    public void ShowPage_MarksOnlyTheCurrentTab()
    {
        const int current = 5;

        window.ShowPage(current);

        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            var selected = page == current;
            AssertThat(Tab(page).Fill).IsEqual(selected ? ProceduralFill.Selected : ProceduralFill.Red);
            AssertThat(TabLabel(page).Colour).IsEqual(selected ? TextColour.Yellow : TextColour.Grey);
        }
    }

    [TestCase]
    public void TheEditorPreview_IsNotSavedIntoTheScene()
    {
        var saved = GD.Load<PackedScene>(ScenePath).Instantiate<DepotWindowView>();

        AssertThat(saved.GetNode<ProceduralPanel>("%Tabs/Page2").Fill).IsEqual(ProceduralFill.Red);
        AssertThat(saved.Cell(0).PreviewSprite).IsEqual(0);
        saved.Free();
    }

    [TestCase]
    public void SavingInTheEditor_LeavesNoTabSelected()
    {
        window.ShowPage(2);

        window.Notification((int)Node.NotificationEditorPreSave);
        var packed = new PackedScene();
        packed.Pack(window);
        var saved = packed.Instantiate<DepotWindowView>();

        AssertThat(saved.GetNode<ProceduralPanel>("%Tabs/Page2").Fill).IsEqual(ProceduralFill.Red);
        AssertThat(saved.GetNode<HudLabel>("%Tabs/Page2/Click/Label").Colour).IsEqual(TextColour.Grey);
        saved.Free();
    }

    [TestCase]
    public void OnlyTheItemsFade()
    {
        AssertThat(Panel("%Items").FadesAs).IsEqual(WindowBackground.Depot);
        AssertThat(Panel("%Pages").FadesAs).IsEqual(WindowBackground.None);
        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            AssertThat(Tab(page).FadesAs).IsEqual(WindowBackground.None);
        }

        AssertThat(Panel("%Frame").FadesAs).IsEqual(WindowBackground.None);
    }

    [TestCase]
    public void ThePages_ShareTheFramesBorder() =>
        AssertThat(Panel("%Pages").Edges).IsEqual(ProceduralEdges.Top);

    [TestCase]
    public void CloseButton_IsTheSharedCloseButton() =>
        AssertThat(window.CloseButton.SceneFilePath).IsEqual("res://Procedural/close_button.tscn");

    private ProceduralEdges CellEdges(int cell) => ((ProceduralPanel)window.Cell(cell).GetParent()).Edges;

    private ProceduralPanel Tab(int page) => (ProceduralPanel)window.PageButton(page).GetParent();

    private HudLabel TabLabel(int page) => window.PageButton(page).GetNode<HudLabel>("Label");

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(window.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    private ProceduralPanel Panel(string path) => window.GetNode<ProceduralPanel>(path);
}
