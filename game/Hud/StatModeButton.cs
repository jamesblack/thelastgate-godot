using Godot;
using TheLastGate.Procedural;
using TheLastGate.Stats;

namespace TheLastGate.Hud;

public partial class StatModeButton : ClickArea
{
    [Export] public StatListMode Mode { get; set; }

    public void ShowMarked(StatPanel panel)
    {
        var marked = panel.IsMarked(Mode);
        GetNode<ProceduralPanel>("Cell").Fill = marked ? ProceduralFill.Selected : ProceduralFill.None;
        if (GetNodeOrNull<CanvasItem>("BothMarker") is { } bothMarker)
        {
            bothMarker.Visible = marked && panel.Mode == StatListMode.SkillsAndMeta;
        }
    }
}
