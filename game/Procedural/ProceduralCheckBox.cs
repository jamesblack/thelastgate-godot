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

    public event Action<bool>? Toggled;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        ShowState();
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
            if (button == MapButton.Left)
            {
                Toggle();
            }
        };
    }

    private void Toggle()
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
        var label = GetNode<HudLabel>("Row/Label");
        label.Text = text;
        label.Visible = text.Length > 0;
        label.Colour = disabled ? TextColour.Dim : TextColour.Yellow;
        Modulate = disabled ? Colors.White with { A = DisabledAlpha } : Colors.White;
    }
}
