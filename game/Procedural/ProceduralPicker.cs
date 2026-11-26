using System;
using System.Collections.Generic;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralPicker : ProceduralPanel
{
    private const float DisabledAlpha = 0.6f;

    private bool disabled;
    private IReadOnlyList<string> names = [];
    private bool shownOpen;

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
            if (value && IsInsideTree())
            {
                Menu.Close();
            }

            Shade(Colors.White);
        }
    }

    // For the style guide: the choice the box shows, and whether it looks open.
    [Export] public string PreviewChoice { get; set; } = "";

    [Export] public bool PreviewOpen { get; set; }

    public int Selected { get; private set; }

    public string Tooltip
    {
        set => GetNode<Control>("Click").TooltipText = value;
    }

    private ProceduralMenu Menu => GetNode<ProceduralMenu>("Menu");

    public event Action<int>? ItemChosen;

    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        Shade(PreviewOpen ? ClickArea.Hovered : Colors.White);
        if (PreviewChoice.Length > 0)
        {
            GetNode<Label>("%Value").Text = PreviewChoice;
        }

        if (GetNode("Click") is not ClickArea click)
        {
            return;
        }

        click.ShadeChanged += shade => Shade(Menu.IsOpen ? ClickArea.Hovered : shade);
        click.Released += button =>
        {
            if (button == MapButton.Left && !disabled)
            {
                Open();
            }
        };
        Menu.ItemChosen += index =>
        {
            Select(index);
            ItemChosen?.Invoke(index);
        };
    }

    // The menu has no closed signal, so the box notices it closing here and stops looking open.
    public override void _Process(double delta)
    {
        if (shownOpen && !Menu.IsOpen)
        {
            shownOpen = false;
            Shade(Colors.White);
        }
    }

    public void SetChoices(IReadOnlyList<string> choices)
    {
        names = choices;
        Select(Math.Clamp(Selected, 0, Math.Max(choices.Count - 1, 0)));
    }

    public void Select(int index)
    {
        Selected = index;
        GetNode<Label>("%Value").Text = index < names.Count ? names[index] : "";
    }

    private void Open()
    {
        Menu.Clear();
        for (var index = 0; index < names.Count; index++)
        {
            Menu.AddItem(names[index], index);
        }

        Menu.GetNode<Control>("Frame").CustomMinimumSize = new Vector2(Size.X, 0);
        Menu.Popup(GlobalPosition + new Vector2(0, Size.Y));
        shownOpen = true;
        Shade(ClickArea.Hovered);
    }

    // The menu is a child, so the box lights itself and its text rather than everything under it.
    private void Shade(Color shade)
    {
        var colour = disabled ? Colors.White with { A = DisabledAlpha } : shade;
        SelfModulate = colour;
        if (GetNodeOrNull<CanvasItem>("Cells") is { } cells)
        {
            cells.Modulate = colour;
        }

        foreach (var path in (string[])["%Value", "%Arrow"])
        {
            if (GetNodeOrNull<HudLabel>(path) is { } label)
            {
                label.Colour = disabled ? TextColour.Dim : TextColour.Yellow;
            }
        }
    }
}
