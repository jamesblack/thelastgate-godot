using System.Diagnostics;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;

namespace TheLastGate.Planner;

// One line of the planner's Focus panel: its text, indented for the raises a route buys, and a value at the right.
public partial class PlannerFocusLine : HBoxContainer
{
    private const int TextLeft = 2;
    private const int IndentedTextLeft = 10;

    public void Show(FocusLine line)
    {
        GetNode<Control>("%Indent").CustomMinimumSize = new Vector2(line.Indented ? IndentedTextLeft : TextLeft, 0);
        var text = GetNode<HudLabel>("%Text");
        text.Text = line.Text;
        text.Colour = line.Tone switch
        {
            FocusTone.Heading => TextColour.White,
            FocusTone.Normal => TextColour.Yellow,
            FocusTone.SkillRaise => TextColour.Green,
            FocusTone.AttributeRaise => TextColour.Blue,
            FocusTone.OtherRoute => TextColour.Grey,
            _ => throw new UnreachableException(),
        };
        GetNode<Label>("%Value").Text = line.Value ?? "";
    }
}
