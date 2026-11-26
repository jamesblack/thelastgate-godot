using Godot;
using Godot.Collections;
using TheLastGate.Assets;

namespace TheLastGate.Hud;

// The nearest theme with any of a label's types wins, so a window theme's own Label colour would beat the variation.
// Each label carries the HUD text theme itself. Neither it nor the variation is saved, as both follow from Colour.
[Tool]
public partial class HudLabel : Label
{
    private bool caption;
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
            ShowVariation();
        }
    }

    // Captions name a slot or a box rather than saying something, and draw in the font set's caption font.
    [Export]
    public bool Caption
    {
        get => caption;
        set
        {
            caption = value;
            ShowVariation();
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

    private void ShowVariation() =>
        ThemeTypeVariation = caption ? HudTextTheme.CaptionVariation(colour) : HudTextTheme.Variation(colour);
}
