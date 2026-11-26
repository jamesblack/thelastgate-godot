using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.State;

namespace TheLastGate.Planner;

// One stat row of the planner: name, base, current, the - and + buttons, the preview "total +bought +gain" and the
// next point's cost.
public partial class PlannerStatRow : ProceduralPanel
{
    private const string Outlined = "G";

    private PlannerRow? shown;

    public int? Slot => shown?.Slot;

    private GlyphButton Lower => GetNode<GlyphButton>("%Lower");

    private GlyphButton Raise => GetNode<GlyphButton>("%Raise");

    // The row's stat plan slot, and true to raise it or false to lower it.
    public event Action<int, bool>? GlyphClicked;

    // The row's stat plan slot.
    public event Action<int>? NameClicked;

    public override void _Ready()
    {
        Lower.Pressed += () => ClickGlyph(false);
        Raise.Pressed += () => ClickGlyph(true);
        GetNode<Control>("%NameClick").GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } && shown != null)
            {
                NameClicked?.Invoke(shown.Slot);
            }
        };
    }

    // The ring takes the padding's place, so the row's contents stay where they are.
    public void ShowOutline(bool outlined)
    {
        Profile = outlined ? Outlined : "";
        Padding = outlined ? 0 : 1;
    }

    public void Show(PlannerRow row)
    {
        shown = row;
        GetNode<Label>("%Name").Text = row.Name;
        GetNode<Label>("%Base").Text = row.Base.ToString();
        GetNode<Label>("%Current").Text = row.Current.ToString();
        var total = GetNode<HudLabel>("%Total");
        total.Text = row.Preview.ToString();
        total.Colour = row.Preview != row.Current ? TextColour.White : TextColour.Yellow;
        GetNode<Label>("%Bought").Text = row.DirectRaise != 0 ? $"+{row.DirectRaise}" : "";
        GetNode<Label>("%Gain").Text = row.AttributeGain != 0 ? $"{row.AttributeGain:+0;-0}" : "";
        GetNode<Label>("%Cost").Text = row.Cost?.ToString() ?? "-";
        Fill = row.DirectRaise != 0 ? ProceduralFill.Selected : ProceduralFill.None;
        GetNode<CanvasItem>("%Marker").Visible = row.UsesRaisedAttribute;
        Lower.Disabled = !row.CanLower;
        Raise.Disabled = !row.CanRaise;

        // Only skills can be focused.
        GetNode<Control>("%NameClick").MouseFilter =
            row.Slot >= StatSlots.FirstSkill ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void ClickGlyph(bool raise)
    {
        if (shown != null)
        {
            GlyphClicked?.Invoke(shown.Slot, raise);
        }
    }
}
