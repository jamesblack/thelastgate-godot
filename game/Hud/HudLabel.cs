using Godot;
using Godot.Collections;
using TheLastGate.Assets;

namespace TheLastGate.Hud;

// The nearest theme with any of a label's types wins, so a window theme's own Label colour would beat the variation.
// Each label carries the HUD text theme itself. Neither it nor the variation is saved, as both follow from Colour.
[Tool]
public partial class HudLabel : Label
{
    private TextColour colour = TextColour.Yellow;

    public HudLabel()
    {
        Theme = HudTextTheme.Load();
        ThemeTypeVariation = HudTextTheme.Variation(colour);
    }

    [Export]
    public TextColour Colour
    {
        get => colour;
        set
        {
            colour = value;
            ThemeTypeVariation = HudTextTheme.Variation(colour);
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsStringName();
        if (name != Control.PropertyName.Theme && name != Control.PropertyName.ThemeTypeVariation)
        {
            return;
        }

        property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
    }
}
