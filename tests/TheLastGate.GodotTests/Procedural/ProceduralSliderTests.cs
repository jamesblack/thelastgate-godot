using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralSliderTests
{
    private const float Width = 200;
    private const float GrabberWidth = 7;

    private readonly List<double> values = [];
    private ProceduralSlider slider = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowASlider()
    {
        slider = GD.Load<PackedScene>("res://Procedural/slider.tscn").Instantiate<ProceduralSlider>();
        slider.MinValue = 0;
        slider.MaxValue = 100;
        slider.Step = 1;
        Tree.Root.AddChild(slider);
        slider.Size = new Vector2(Width, 11);
        values.Clear();
        slider.ValueChanged += values.Add;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheSlider() => slider.Free();

    [TestCase]
    public void Press_JumpsToTheValue()
    {
        Press(GrabberWidth / 2 + 0.75f * (Width - GrabberWidth));

        AssertThat(slider.Value).IsEqual(75d);
        AssertThat(values).ContainsExactly(75d);
    }

    [TestCase]
    public void Drag_PastTheEnd_Clamps()
    {
        Press(Width / 2);

        Drag(Width + 300);
        AssertThat(slider.Value).IsEqual(100d);

        Drag(-50);
        AssertThat(slider.Value).IsEqual(0d);
    }

    [TestCase]
    public void Release_EndsTheDrag()
    {
        Press(Width / 2);
        slider._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });

        Drag(Width);

        AssertThat(slider.Value).IsEqual(50d);
    }

    [TestCase]
    public async Task SetValueNoSignal_MovesTheGrabber()
    {
        slider.SetValueNoSignal(100);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(slider.GetNode<Control>("Grabber").Position.X).IsEqual(Width - GrabberWidth);
        AssertThat(values).IsEmpty();
    }

    [TestCase]
    public void Hover_LightsTheGrabber()
    {
        slider.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(slider.GetNode<CanvasItem>("Grabber").Modulate).IsEqual(ClickArea.Hovered);
    }

    private void Press(float x) => slider._GuiInput(
        new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = new Vector2(x, 5),
        });

    private void Drag(float x) => slider._GuiInput(
        new InputEventMouseMotion
        {
            ButtonMask = MouseButtonMask.Left,
            Position = new Vector2(x, 5),
        });
}
