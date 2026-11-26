using System;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Notes;
using TheLastGate.World;

namespace TheLastGate.Procedural;

[Tool]
public partial class ColourPicker : GridContainer
{
    private const string Scene = "res://Procedural/colour_picker.tscn";
    private const int SwatchSize = 14;
    private const string ChosenRing = "W";
    private const string Ring = "G";

    private int chosen;

    [Export]
    public int Chosen
    {
        get => chosen;
        set
        {
            chosen = Math.Clamp(value, 0, NotePalette.Size - 1);
            ShowChosen();
        }
    }

    public event Action<int>? ColourChosen;

    public static ColourPicker Load(int chosenColour)
    {
        var picker = GD.Load<PackedScene>(Scene).Instantiate<ColourPicker>();
        picker.Chosen = chosenColour;
        return picker;
    }

    public override void _Ready()
    {
        for (var colourIndex = 0; colourIndex < NotePalette.Size; colourIndex++)
        {
            AddChild(Swatch(colourIndex));
        }

        ShowChosen();
    }

    // ClickArea is not a tool script, so the editor's preview gets plain Controls that only show the colours.
    private Control Swatch(int colourIndex)
    {
        var swatch = Engine.IsEditorHint() ? new Control() : new ClickArea { LightsUp = true };
        swatch.Name = $"Swatch{colourIndex}";
        swatch.CustomMinimumSize = new Vector2(SwatchSize, SwatchSize);
        var look = new ProceduralPanel
        {
            Name = "Look",
            Profile = Ring,
            Fill = ProceduralFill.Blue,
            Padding = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        look.SetAnchorsPreset(LayoutPreset.FullRect);
        var rgb = NotePalette.Rgb(colourIndex);
        look.AddChild(
            new ColorRect
            {
                Color = Color.Color8((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb),
                MouseFilter = MouseFilterEnum.Ignore,
            });
        swatch.AddChild(look);
        if (swatch is ClickArea click)
        {
            click.Released += button =>
            {
                if (button == MapButton.Left)
                {
                    Choose(colourIndex);
                }
            };
        }

        return swatch;
    }

    private void Choose(int colourIndex)
    {
        Chosen = colourIndex;
        ColourChosen?.Invoke(colourIndex);
    }

    private void ShowChosen()
    {
        for (var colourIndex = 0; colourIndex < GetChildCount(); colourIndex++)
        {
            GetChild(colourIndex).GetNode<ProceduralPanel>("Look").Profile =
                colourIndex == chosen ? ChosenRing : Ring;
        }
    }
}
