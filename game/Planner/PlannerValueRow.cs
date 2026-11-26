using System;
using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Planner;

// A planner row with one value: the server's, and when the plan changes it, the planned value and the difference.
public partial class PlannerValueRow : ProceduralPanel
{
    private const string Outlined = "G";

    public event Action? NameClicked;

    // A combat stat's name can be clicked to focus it while the planned values are shown.
    public void AllowFocus(bool allowed) =>
        GetNode<Control>("%NameClick").MouseFilter = allowed ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

    public override void _Ready()
    {
        GetNode<Control>("%NameClick").GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            {
                NameClicked?.Invoke();
            }
        };
    }

    // The ring takes the padding's place, so the row's contents stay where they are.
    public void ShowOutline(bool outlined)
    {
        Profile = outlined ? Outlined : "";
        Padding = outlined ? 0 : 1;
    }

    public void Show(string name, string value)
    {
        GetNode<Label>("%Name").Text = name;
        GetNode<Label>("%Value").Text = value;
        GetNode<Label>("%Planned").Text = "";
        GetNode<Label>("%Difference").Text = "";
    }

    public void ShowPlanned(string name, int current, int planned)
    {
        Show(name, current.ToString());
        if (planned == current)
        {
            return;
        }

        GetNode<Label>("%Planned").Text = planned.ToString();
        GetNode<Label>("%Difference").Text = $"{planned - current:+0;-0}";
    }
}
