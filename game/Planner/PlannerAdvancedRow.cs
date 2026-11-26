using System;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;

namespace TheLastGate.Planner;

// One row of the planner's Advanced window: a tick and its name, and for the rows that have them, what one copy does,
// how many copies there are and how many the skill tree adds.
[Tool]
public partial class PlannerAdvancedRow : HBoxContainer
{
    private ProceduralCheckBox Tick => GetNode<ProceduralCheckBox>("%Tick");

    private LineEdit Count => GetNode<LineEdit>("%Count");

    private HudLabel Effect => GetNode<HudLabel>("%Effect");

    public event Action? Toggled;

    public event Action<int>? CountChosen;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        Tick.Toggled += _ => Toggled?.Invoke();
        Count.TextChanged += TypeCount;
        Count.EditingToggled += editing =>
        {
            if (!editing)
            {
                CountChosen?.Invoke(int.TryParse(Count.Text, out var copies) ? copies : 0);
            }
        };
    }

    public void Show(string name, bool ticked, bool enabled) => ShowTick(name, ticked, enabled);

    // A row the planner always counts, such as the skill tree's.
    public void ShowAlwaysOn(string name) => ShowTick(name, true, false);

    // A line under a row, such as a spell's warning.
    public void ShowNote(string text)
    {
        Tick.Visible = false;
        var note = GetNode<Label>("%Note");
        note.Visible = true;
        note.Text = text;
    }

    public void ShowEffect(string effect)
    {
        Effect.Text = effect;
        Effect.Visible = effect.Length > 0;
    }

    // The count being typed is left alone until the typing ends.
    public void ShowCount(int copies, bool enabled, string help)
    {
        Count.Visible = true;
        Count.Editable = enabled;
        Count.TooltipText = help;
        if (!Count.IsEditing())
        {
            Count.Text = copies.ToString();
        }
    }

    public void ShowTree(string text) => GetNode<Label>("%Tree").Text = text;

    private void ShowTick(string name, bool ticked, bool enabled)
    {
        Tick.Visible = true;
        Tick.Text = name;
        Tick.Checked = ticked;
        Tick.Disabled = !enabled;
        Effect.Colour = enabled ? TextColour.Grey : TextColour.Dim;
    }

    // Only digits can be typed. A count takes effect as it is typed; an empty box or 0 waits for the typing to end,
    // which makes it 1.
    private void TypeCount(string text)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        if (digits != text)
        {
            var caret = Count.CaretColumn;
            Count.Text = digits;
            Count.CaretColumn = Math.Min(caret, digits.Length);
        }

        if (int.TryParse(digits, out var copies) && copies > 0)
        {
            CountChosen?.Invoke(copies);
        }
    }
}
