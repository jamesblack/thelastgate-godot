using System;
using Godot;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

[Tool]
public partial class ResizeGrip : Control
{
    private bool dragging;
    private bool hovered;

    public event Action<Vector2>? Dragged;

    public override void _Ready()
    {
        MouseEntered += () => SetHovered(true);
        MouseExited += () => SetHovered(false);
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                dragging = click.Pressed;
                Reshade();
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when dragging:
                Dragged?.Invoke(motion.Relative);
                AcceptEvent();
                break;
        }
    }

    private void SetHovered(bool isHovered)
    {
        hovered = isHovered;
        Reshade();
    }

    private void Reshade() => Modulate = dragging ? ClickArea.Held : hovered ? ClickArea.Hovered : Colors.White;
}
