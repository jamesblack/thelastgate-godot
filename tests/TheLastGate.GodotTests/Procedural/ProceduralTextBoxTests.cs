using GdUnit4;
using Godot;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralTextBoxTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void TextBox_ScrollBarHasTheProceduralLook()
    {
        var box = GD.Load<PackedScene>("res://Procedural/text_box.tscn").Instantiate<ProceduralTextBox>();
        Tree.Root.AddChild(box);

        AssertThat(box.GetVScrollBar().Material)
            .IsSame(ProceduralLook.Panel(ProceduralShape.Box, "", ProceduralFill.Green, ProceduralEdges.None));
        AssertThat(box.GetVScrollBar().UseParentMaterial).IsFalse();
        box.Free();
    }

    [TestCase]
    public void TextBox_ScrollBarIsAsWideAsTheColumnBar()
    {
        var box = GD.Load<PackedScene>("res://Procedural/text_box.tscn").Instantiate<ProceduralTextBox>();
        Tree.Root.AddChild(box);

        AssertThat(box.GetVScrollBar().GetCombinedMinimumSize().X).IsEqual(7f);
        box.Free();
    }

    [TestCase]
    public void WaypointSearch_IsTheSharedTextField()
    {
        var window = GD.Load<PackedScene>("res://Hud/Waypoints/waypoint_window.tscn").Instantiate();

        AssertThat(window.GetNode<LineEdit>("%Search").SceneFilePath).IsEqual("res://Procedural/text_field.tscn");
        window.Free();
    }
}
