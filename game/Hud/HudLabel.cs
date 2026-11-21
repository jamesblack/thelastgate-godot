using Godot;
using Godot.Collections;
using TheLastGate.Assets;

namespace TheLastGate.Hud;

[Tool]
public partial class HudLabel : Label, IHudContextUser
{
    private TextColour colour = TextColour.Yellow;
    private IFontSet? fonts;

    public HudLabel() => AddToGroup(HudContext.Group);

    [Export]
    public TextColour Colour
    {
        get => colour;
        set
        {
            colour = value;
            ApplyFont();
        }
    }

    public void ShowContext(HudContext context)
    {
        fonts = context.Fonts;
        ApplyFont();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsString();
        if (name != "theme_override_fonts/font" && name != "theme_override_font_sizes/font_size"
            && name != "theme_override_colors/font_color")
        {
            return;
        }

        property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
    }

    private void ApplyFont()
    {
        if (fonts == null)
        {
            return;
        }

        var font = fonts.For(colour);
        AddThemeFontOverride("font", font.Font);
        AddThemeFontSizeOverride("font_size", font.Size);
        AddThemeColorOverride("font_color", font.Tint);
    }
}
