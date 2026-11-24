using Godot;
using Godot.Collections;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

// The shader draws everything the bar paints. With no edges every pixel is fill, wherever the grabber sits; with
// edges the shader would measure from the bar's own rect and draw the lower grabber as rings.
[Tool]
public partial class ProceduralScrollBar : VScrollBar
{
    private static Theme? look;

    public ProceduralScrollBar()
    {
        Material = ProceduralLook.Panel(ProceduralShape.Box, "", ProceduralFill.Green, ProceduralEdges.None);
        Theme = look ??= Look();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsStringName();
        if (name != CanvasItem.PropertyName.Material && name != Control.PropertyName.Theme)
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
        theme.SetStylebox("grabber", "VScrollBar", Grabber(Colors.White));
        theme.SetStylebox("grabber_highlight", "VScrollBar", Grabber(ClickArea.Hovered));
        theme.SetStylebox("grabber_pressed", "VScrollBar", Grabber(ClickArea.Held));
        return theme;
    }

    private static StyleBoxFlat Grabber(Color shade) => new() { BgColor = shade };
}
