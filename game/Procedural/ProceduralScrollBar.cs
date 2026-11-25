using Godot;
using Godot.Collections;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

// The shader draws everything the bar paints. With no edges every pixel is fill, wherever the grabber sits; with
// edges the shader would measure from the bar's own rect and draw the lower grabber as rings.
[Tool]
public partial class ProceduralScrollBar : VScrollBar
{
    private static readonly StringName[] GrabberStyles = ["grabber", "grabber_highlight", "grabber_pressed"];
    private static readonly Color[] GrabberShades = [Colors.White, ClickArea.Hovered, ClickArea.Held];

    private static Theme? look;

    private int minimumGrabberLength;

    public ProceduralScrollBar()
    {
        Material = ProceduralLook.Panel(ProceduralShape.Box, "", ProceduralFill.Green, ProceduralEdges.None);
        Theme = look ??= Look();
    }

    // A bar with no page has no grabber length of its own, so it draws this many pixels.
    [Export]
    public int MinimumGrabberLength
    {
        get => minimumGrabberLength;
        set
        {
            minimumGrabberLength = value;
            for (var style = 0; style < GrabberStyles.Length; style++)
            {
                if (value == 0)
                {
                    RemoveThemeStyleboxOverride(GrabberStyles[style]);
                }
                else
                {
                    AddThemeStyleboxOverride(GrabberStyles[style], Grabber(GrabberShades[style], value));
                }
            }
        }
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsStringName();
        var generated = name == CanvasItem.PropertyName.Material || name == Control.PropertyName.Theme
            || name.ToString().StartsWith("theme_override_styles/grabber");
        if (!generated)
        {
            return;
        }

        property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
    }

    private static Theme Look()
    {
        var theme = new Theme();
        var track = new StyleBoxEmpty();
        theme.SetStylebox("scroll", "VScrollBar", track);
        theme.SetStylebox("scroll_focus", "VScrollBar", track);
        for (var style = 0; style < GrabberStyles.Length; style++)
        {
            theme.SetStylebox(GrabberStyles[style], "VScrollBar", Grabber(GrabberShades[style], 0));
        }

        return theme;
    }

    private static StyleBoxFlat Grabber(Color shade, int minimumLength) =>
        new() { BgColor = shade, ContentMarginTop = minimumLength / 2f, ContentMarginBottom = minimumLength / 2f };
}
