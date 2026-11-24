using Godot;
using Godot.Collections;
using TheLastGate.Hud;
using TheLastGate.Windows;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralLine : Line2D, IHudContextUser
{
    private string profile = "";

    public ProceduralLine()
    {
        AddToGroup(HudContext.Group);
        Material = ProceduralLook.Line(profile);
    }

    // Only the first ring is drawn, as the line's colour: G gold (the default), B blue, R red, W white.
    [Export]
    public string Profile
    {
        get => profile;
        set
        {
            profile = value;
            Material = ProceduralLook.Line(profile);
        }
    }

    [Export] public WindowBackground FadesAs { get; set; }

    public void ShowContext(HudContext context) =>
        SelfModulate = Colors.White with { A = WindowBackdrop.Alpha(context.ClientSettings, FadesAs) };

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsString() != CanvasItem.PropertyName.Material)
        {
            return;
        }

        property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
    }
}
