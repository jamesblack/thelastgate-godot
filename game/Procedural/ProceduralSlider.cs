using Godot;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralSlider : Range
{
    private const int GrooveHeight = 3;

    private bool dragging;
    private bool hovered;
    private double placedRatio = -1;
    private Vector2 placedSize;

    private Control Grabber => GetNode<Control>("Grabber");

    private float Travel => Mathf.Max(Size.X - Grabber.Size.X, 0);

    public override void _Ready()
    {
        MouseEntered += () => ShowHover(true);
        MouseExited += () => ShowHover(false);
        Place();
    }

    // SetValueNoSignal raises nothing, so the parts follow the value here.
    public override void _Process(double delta)
    {
        if (Ratio != placedRatio || Size != placedSize)
        {
            Place();
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                dragging = button.Pressed;
                if (dragging)
                {
                    MoveTo(button.Position.X);
                }

                ShowHover(hovered);
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when dragging:
                MoveTo(motion.Position.X);
                AcceptEvent();
                break;
        }
    }

    private void MoveTo(float x)
    {
        var travel = Travel;
        var ratio = travel > 0 ? Mathf.Clamp((x - Grabber.Size.X / 2) / travel, 0, 1) : 0;
        Value = MinValue + ratio * (MaxValue - MinValue);
    }

    private void ShowHover(bool over)
    {
        hovered = over;
        Grabber.Modulate = hovered || dragging ? ClickArea.Hovered : Colors.White;
    }

    private void Place()
    {
        placedRatio = Ratio;
        placedSize = Size;
        var middle = Mathf.Floor(Size.Y / 2);
        var grabber = Grabber;
        var left = Mathf.Round((float)Ratio * Travel);
        var groove = GetNode<Control>("Groove");
        groove.Position = new Vector2(0, middle - 1);
        groove.Size = new Vector2(Size.X, GrooveHeight);
        var filled = GetNode<Control>("Filled");
        filled.Position = new Vector2(1, middle);
        filled.Size = new Vector2(Mathf.Max(left + grabber.Size.X / 2 - 1, 0), 1);
        grabber.Position = new Vector2(left, Mathf.Floor((Size.Y - grabber.Size.Y) / 2));
    }
}
