using Godot;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralTooltip : ProceduralPanel
{
    private const string ScenePath = "res://Procedural/tooltip.tscn";

    public static ProceduralTooltip Make(string text)
    {
        var tooltip = GD.Load<PackedScene>(ScenePath).Instantiate<ProceduralTooltip>();
        tooltip.GetNode<Label>("Text").Text = text;
        return tooltip;
    }
}
