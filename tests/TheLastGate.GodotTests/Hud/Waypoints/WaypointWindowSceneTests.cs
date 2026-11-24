using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Hud.Waypoints;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;
using static GdUnit4.Assertions;
using WaypointList = TheLastGate.State.Waypoints;

namespace TheLastGate.GodotTests.Hud.Waypoints;

[TestSuite]
[RequireGodotRuntime]
public class WaypointWindowSceneTests
{
    private const string ScenePath = "res://Hud/Waypoints/waypoint_window.tscn";
    private const string RowScenePath = "res://Hud/Waypoints/waypoint_row.tscn";
    private const int LastRow = WaypointWindow.VisibleRows - 1;
    private const int AllFound = ~0;

    private WaypointWindowView window = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        window = GD.Load<PackedScene>(ScenePath).Instantiate<WaypointWindowView>();
        Tree.Root.AddChild(window);
        window.Visible = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => window.Free();

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>(ScenePath).Instantiate<WaypointWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void Row_IsEveryRow()
    {
        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            AssertThat(window.Row(row).Name.ToString()).IsEqual($"Row{row}");
            AssertThat(window.Row(row).SceneFilePath).IsEqual(RowScenePath);
            AssertThat(window.Row(row).LightsUp).IsTrue();
        }
    }

    [TestCase]
    public void ShowRow_FoundWaypoint()
    {
        window.ShowRow(0, WaypointList.All[3], true, false);

        AssertThat(Name(0).Text).IsEqual("Aston, Crossroads");
        AssertThat(Description(0).Text).IsEqual("\"Between ivory and violet.\"");
        AssertThat(Picture(0).SpriteNumber).IsEqual(4503);
        AssertThat(Picture(0).Visible).IsTrue();
    }

    [TestCase]
    public void ShowRow_UnknownWaypoint()
    {
        window.ShowRow(0, WaypointList.All[3], false, false);

        AssertThat(Name(0).Text).IsEqual("Unknown Location");
        AssertThat(Description(0).Text).IsEqual("\"???\"");
        AssertThat(Picture(0).SpriteNumber).IsEqual(4536);
    }

    [TestCase]
    public void ShowRow_Selected_FillsSteelBlue()
    {
        window.ShowRow(0, WaypointList.All[0], true, true);
        AssertThat(Cell(0, "PictureCell").Fill).IsEqual(ProceduralFill.Selected);
        AssertThat(Cell(0, "TextCell").Fill).IsEqual(ProceduralFill.Selected);

        window.ShowRow(0, WaypointList.All[0], true, false);
        AssertThat(Cell(0, "PictureCell").Fill).IsEqual(ProceduralFill.Red);
        AssertThat(Cell(0, "TextCell").Fill).IsEqual(ProceduralFill.Red);
    }

    [TestCase]
    public void ShowRow_Selected_BrightensThePicture()
    {
        window.ShowRow(0, WaypointList.All[0], true, true);
        AssertThat(Picture(0).Modulate).IsEqual(new Color(2, 2, 2));

        window.ShowRow(0, WaypointList.All[0], true, false);
        AssertThat(Picture(0).Modulate).IsEqual(Colors.White);
    }

    [TestCase]
    public void ShowRow_Empty_ShowsABlankRowThatCannotBeClicked()
    {
        window.ShowRow(5, null, false, false);

        AssertThat(window.Row(5).Visible).IsTrue();
        AssertThat(Picture(5).Visible).IsFalse();
        AssertThat(Name(5).Visible).IsFalse();
        AssertThat(Description(5).Visible).IsFalse();
        AssertThat(window.Row(5).MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        AssertThat(Cell(5, "TextCell").Fill).IsEqual(ProceduralFill.Red);

        window.ShowRow(5, WaypointList.All[0], true, false);
        AssertThat(window.Row(5).MouseFilter).IsEqual(Control.MouseFilterEnum.Stop);
        AssertThat(Name(5).Visible).IsTrue();
    }

    [TestCase]
    public void AnEmptiedRow_StopsLightingUp()
    {
        window.Row(1).EmitSignal(Control.SignalName.MouseEntered);
        AssertThat(window.Row(1).Modulate).IsEqual(ClickArea.Hovered);

        window.ShowRow(1, null, false, false);

        AssertThat(window.Row(1).Modulate).IsEqual(Colors.White);
    }

    [TestCase]
    public void ShowWaypoints_KeepsEightRowsForFewMatches()
    {
        var waypoints = new WaypointWindow(new WindowPause());
        var player = new PlayerState();
        waypoints.Open();
        window.ShowWaypoints(waypoints, player);
        waypoints.SetSearch("lynbore", AllFound);

        window.ShowWaypoints(waypoints, player);

        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            AssertThat(window.Row(row).Visible).IsTrue();
        }

        AssertThat(Name(1).Visible).IsTrue();
        AssertThat(Name(2).Visible).IsFalse();
        AssertThat(Name(LastRow).Visible).IsFalse();
    }

    [TestCase]
    public void TheLastRow_HasNoBottomLine()
    {
        var withBottom = ProceduralEdges.Right | ProceduralEdges.Bottom;
        AssertThat(Cell(0, "PictureCell").Edges).IsEqual(withBottom);
        AssertThat(Cell(0, "TextCell").Edges).IsEqual(withBottom);
        AssertThat(Cell(LastRow, "PictureCell").Edges).IsEqual(ProceduralEdges.Right);
        AssertThat(Cell(LastRow, "TextCell").Edges).IsEqual(ProceduralEdges.Right);
    }

    [TestCase]
    public void OnlyTheRowFillsFade()
    {
        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            AssertThat(Cell(row, "PictureCell").FadesAs).IsEqual(WindowBackground.Waypoints);
            AssertThat(Cell(row, "TextCell").FadesAs).IsEqual(WindowBackground.Waypoints);
            AssertThat(Cell(row, "PictureCell").SolidRings).IsTrue();
            AssertThat(Cell(row, "TextCell").SolidRings).IsTrue();
        }

        AssertThat(Panel("%Frame").FadesAs).IsEqual(WindowBackground.None);
        AssertThat(Panel("%ScrollColumn").FadesAs).IsEqual(WindowBackground.None);
        AssertThat(Panel("%Footer").FadesAs).IsEqual(WindowBackground.None);
    }

    [TestCase]
    public void TheGrabber_IsDrawnByTheShaderInGreen()
    {
        var material = (ShaderMaterial)window.ScrollBar.Material;

        AssertThat(material.GetShaderParameter("fill").AsInt32()).IsEqual((int)ProceduralFill.Green);
        AssertThat(material.GetShaderParameter("profile_width").AsInt32()).IsEqual(0);
        AssertThat(material.GetShaderParameter("edges").AsInt32()).IsEqual((int)ProceduralEdges.None);
    }

    [TestCase]
    public void TheWheelScrolls_OnlyOverTheRows()
    {
        AssertThat(window.WheelArea).IsSame(window.GetNode<Control>("%Rows"));
        AssertThat(window.WheelArea.MouseFilter).IsEqual(Control.MouseFilterEnum.Pass);
    }

    [TestCase]
    public void CloseButton_IsTheSharedCloseButton() =>
        AssertThat(window.CloseButton.SceneFilePath).IsEqual("res://Procedural/close_button.tscn");

    private ProceduralPanel Cell(int row, string cell) => window.Row(row).GetNode<ProceduralPanel>($"Cells/{cell}");

    private ArchiveSprite Picture(int row) => window.Row(row).GetNode<ArchiveSprite>("Cells/PictureCell/Picture");

    private Label Name(int row) => window.Row(row).GetNode<Label>("Cells/TextCell/Text/Name");

    private Label Description(int row) => window.Row(row).GetNode<Label>("Cells/TextCell/Text/Description");

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(window.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    private ProceduralPanel Panel(string path) => window.GetNode<ProceduralPanel>(path);
}
