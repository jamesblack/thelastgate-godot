using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralCheckBox : MarginContainer
{
    private const float DisabledAlpha = 0.6f;
    private bool disabled;

    private bool isChecked;
    private bool round;
    private Color shade = Colors.White;
    private string text = "";

    [Export]
    public bool Checked
    {
        get => isChecked;
        set
        {
            if (value == isChecked)
            {
                return;
            }

            isChecked = value;
            ShowState();
        }
    }

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
            ShowState();
        }
    }

    [Export]
    public string Text
    {
        get => text;
        set
        {
            if (value == text)
            {
                return;
            }

            text = value;
            ShowState();
        }
    }

    [Export]
    public bool Round
    {
        get => round;
        set
        {
            if (value == round)
            {
                return;
            }

            round = value;
            ShowState();
        }
    }

    public event Action<bool>? Toggled;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        ShowState();
        if (GetNode("Click") is not ClickArea click)
        {
            return;
        }

        click.ShadeChanged += newShade =>
        {
            shade = newShade;
            if (!disabled)
            {
                Modulate = shade;
            }
        };
        click.Released += button =>
        {
            if (button == MapButton.Left)
            {
                Toggle();
            }
        };
    }

    public void Toggle()
    {
        if (disabled)
        {
            return;
        }

        Checked = !isChecked;
        Toggled?.Invoke(isChecked);
    }

    private void ShowState()
    {
        if (!IsNodeReady() && GetChildCount() == 0)
        {
            return;
        }

        GetNode<Control>("Row/Box/Mark").Visible = isChecked;
        var shape = round ? ProceduralShape.Circle : ProceduralShape.Box;
        GetNode<ProceduralPanel>("Row/Box").Shape = shape;
        GetNode<ProceduralPanel>("Row/Box/Mark/Gold").Shape = shape;
        var label = GetNode<HudLabel>("Row/Label");
        label.Text = text;
        label.Visible = text.Length > 0;
        label.Colour = disabled ? TextColour.Dim : TextColour.Yellow;
        Modulate = disabled ? Colors.White with { A = DisabledAlpha } : shade;
    }
}
