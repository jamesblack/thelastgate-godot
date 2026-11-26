using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ResizeGripTests
{
    private ResizeGrip grip = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAGrip()
    {
        grip = GD.Load<PackedScene>("res://Procedural/resize_grip.tscn").Instantiate<ResizeGrip>();
        Tree.Root.AddChild(grip);
    }

    [AfterTest]
    public void FreeTheGrip() => grip.Free();

    [TestCase]
    public void Hover_LightsTheGrip()
    {
        grip.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(grip.Modulate).IsEqual(ClickArea.Hovered);
    }

    [TestCase]
    public void Holding_LightsItMoreAndDraggingReportsTheMove()
    {
        var moves = new List<Vector2>();
        grip.Dragged += moves.Add;

        grip._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
        grip._GuiInput(new InputEventMouseMotion { Relative = new Vector2(4, 3) });

        AssertThat(grip.Modulate).IsEqual(ClickArea.Held);
        AssertThat(moves).ContainsExactly(new Vector2(4, 3));
    }

    [TestCase]
    public void TheGrip_IsSixGoldDots()
    {
        AssertThat(grip.GetChildCount()).IsEqual(6);
        AssertThat(grip.GetChild<ProceduralPanel>(0).Profile).IsEqual("GG");
    }
}
