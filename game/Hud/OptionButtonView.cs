using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Hud;

public partial class OptionButtonView : ClickArea
{
    [Export] public OptionButton Kind { get; set; }

    public void ShowMarked(bool marked, bool doubleMarked)
    {
        GetNode<ProceduralPanel>("Cell").Fill = marked ? ProceduralFill.Selected : ProceduralFill.None;
        if (GetNodeOrNull<CanvasItem>("BothMarker") is { } bothMarker)
        {
            bothMarker.Visible = doubleMarked;
        }
    }
}
