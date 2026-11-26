using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class GlyphButton : ProceduralPanel
{
    private const float DisabledAlpha = 0.6f;

    private bool disabled;
    private string glyph = "+";

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
    public string Glyph
    {
        get => glyph;
        set
        {
            glyph = value;
            if (GetNodeOrNull<Label>("%Glyph") is { } label)
            {
                label.Text = value;
            }
        }
    }

    public event Action? Pressed;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        Glyph = glyph;
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
            if (button == MapButton.Left && !disabled)
            {
                Pressed?.Invoke();
            }
        };
    }

    private void ShowState()
    {
        Modulate = disabled ? Colors.White with { A = DisabledAlpha } : Colors.White;
        if (GetNodeOrNull<HudLabel>("%Glyph") is { } label)
        {
            label.Colour = disabled ? TextColour.Dim : TextColour.Yellow;
        }
    }
}
