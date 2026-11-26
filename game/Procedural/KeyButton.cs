using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class KeyButton : ProceduralPanel
{
    private string caption = "";
    private bool waiting;

    [Export]
    public string Caption
    {
        get => caption;
        set
        {
            if (value == caption)
            {
                return;
            }

            caption = value;
            if (GetNodeOrNull<Label>("Label") is { } label)
            {
                label.Text = value;
            }
        }
    }

    [Export]
    public bool Waiting
    {
        get => waiting;
        set
        {
            if (value == waiting)
            {
                return;
            }

            waiting = value;
            ShowWaiting();
        }
    }

    public string Tooltip
    {
        set => GetNode<Control>("Click").TooltipText = value;
    }

    public event Action? Pressed;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        if (caption.Length > 0)
        {
            GetNode<Label>("Label").Text = caption;
        }

        ShowWaiting();
        if (GetNode("Click") is not ClickArea click)
        {
            return;
        }

        click.ShadeChanged += shade => Modulate = waiting ? ClickArea.Hovered : shade;
        click.Released += button =>
        {
            if (button == MapButton.Left)
            {
                Pressed?.Invoke();
            }
        };
    }

    private void ShowWaiting()
    {
        Fill = waiting ? ProceduralFill.Selected : ProceduralFill.Blue;
        Modulate = waiting ? ClickArea.Hovered : Colors.White;
        if (GetNodeOrNull<HudLabel>("Label") is { } label)
        {
            label.Colour = waiting ? TextColour.White : TextColour.Yellow;
        }
    }
}
