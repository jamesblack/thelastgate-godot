using TheLastGate.Planner;

namespace TheLastGate.Windows;

// The skill planner, opened by its button under the skill list or its hotkey while the option is on, and its Advanced
// window. Closing the planner closes both; Escape closes the Advanced window first.
public sealed class PlannerWindow
{
    public bool IsOpen { get; private set; }

    public bool AdvancedOpen { get; set; }

    public PlannerFocus? Focus { get; private set; }

    // Focusing what is already focused clears the focus.
    public void ToggleFocus(PlannerFocus focus) => Focus = focus == Focus ? null : focus;

    public void ClearFocus() => Focus = null;

    public bool Toggle()
    {
        if (IsOpen)
        {
            Close();
            return false;
        }

        IsOpen = true;
        return true;
    }

    public bool CloseByEscape()
    {
        if (!AdvancedOpen)
        {
            return Close();
        }

        AdvancedOpen = false;
        return true;
    }

    public bool Close()
    {
        var wasOpen = IsOpen || AdvancedOpen;
        IsOpen = false;
        AdvancedOpen = false;
        return wasOpen;
    }
}
