using System;
using Godot;
using TheLastGate.World;

namespace TheLastGate.Hud;

public partial class ClickArea : Control
{
    [Signal]
    public delegate void ReleasedEventHandler(MapButton button);

    public static readonly Color Hovered = new(1.6f, 1.6f, 1.6f);
    public static readonly Color Held = new(2f, 2f, 2f);

    private bool held;
    private bool hovered;

    public ClickArea()
    {
        MouseEntered += () => ShowHovered(true);
        MouseExited += () => ShowHovered(false);
    }

    [Export] public bool LightsUp { get; set; }

    // For areas whose tooltip depends on where in them the mouse is.
    public Func<Vector2, string>? TooltipFor { get; set; }

    public event Action<Color>? ShadeChanged;

    public void ClearHover() => ShowHeld(false, false);

    public override string _GetTooltip(Vector2 atPosition) => TooltipFor?.Invoke(atPosition) ?? TooltipText;

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton click
            || click.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            return;
        }

        AcceptEvent();
        if (click.ButtonIndex is MouseButton.Left or MouseButton.Right)
        {
            ShowHeld(click.Pressed, click.Pressed || Contains(click.Position));
        }

        if (click.Pressed || !Contains(click.Position))
        {
            return;
        }

        var button = click.ButtonIndex switch
        {
            MouseButton.Left => MapButton.Left,
            MouseButton.Right => MapButton.Right,
            _ => MapButton.None,
        };
        if (button != MapButton.None)
        {
            EmitSignal(SignalName.Released, Variant.From(button));
        }
    }

    private void ShowHovered(bool over)
    {
        hovered = over;
        Reshade();
    }

    private void ShowHeld(bool down, bool over)
    {
        held = down;
        hovered = over;
        Reshade();
    }

    private void Reshade()
    {
        var shade = held ? Held : hovered ? Hovered : Colors.White;
        if (LightsUp)
        {
            Modulate = shade;
        }

        ShadeChanged?.Invoke(shade);
    }

    protected virtual bool Contains(Vector2 point) => new Rect2(Vector2.Zero, Size).HasPoint(point);
}
