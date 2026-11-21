using Godot;
using Godot.Collections;

namespace TheLastGate.Hud;

[Tool]
public partial class DarkModeShift : Control, IHudContextUser
{
    private bool darkMode;
    private Vector2 shift;

    public DarkModeShift() => AddToGroup(HudContext.Group);

    [Export]
    public Vector2 Shift
    {
        get => shift;
        set
        {
            shift = value;
            ApplyDarkMode();
        }
    }

    public void ShowContext(HudContext context)
    {
        darkMode = context.DarkMode;
        ApplyDarkMode();
    }

    // The position is never saved, so a dark-mode preview in the editor can't end up stored in the scene.
    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsString();
        if (name is "offset_left" or "offset_top" or "offset_right" or "offset_bottom")
        {
            property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
        }
    }

    private void ApplyDarkMode() => Position = darkMode ? shift : Vector2.Zero;
}
