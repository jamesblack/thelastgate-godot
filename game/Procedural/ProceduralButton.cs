using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralButton : ProceduralPanel
{
    private const float DisabledAlpha = 0.6f;

    private string caption = "";
    private bool disabled;
    private bool on;

    [Export]
    public bool Disabled
    {
        get => disabled;
        set
        {
            if (value == disabled)
            {
                return;
            }

            disabled = value;
            ShowOn();
        }
    }

    [Export]
    public string Caption
    {
        get => caption;
        set
        {
            caption = value;
            if (GetNodeOrNull<Label>("Label") is { } label && value.Length > 0)
            {
                label.Text = value;
            }
        }
    }

    [Export]
    public bool On
    {
        get => on;
        set
        {
            if (value == on)
            {
                return;
            }

            on = value;
            ShowOn();
        }
    }

    public event Action? Pressed;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        Caption = caption;
        if (on || disabled)
        {
            ShowOn();
        }

        if (GetNode("Click") is not ClickArea click)
        {
            return;
        }

        click.ShadeChanged += shade =>
        {
            if (!disabled)
            {
                Modulate = shade;
            }
        };
        click.Released += button =>
        {
            if (button == MapButton.Left && !disabled)
            {
                Pressed?.Invoke();
            }
        };
    }

    private void ShowOn()
    {
        Fill = on ? ProceduralFill.Selected : ProceduralFill.Red;
        Modulate = disabled ? Colors.White with { A = DisabledAlpha } : Colors.White;
        if (GetNodeOrNull<HudLabel>("Label") is { } label)
        {
            label.Colour = disabled ? TextColour.Dim : on ? TextColour.Yellow : TextColour.Grey;
        }
    }
}
