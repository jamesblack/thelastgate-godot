using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Hud;

public partial class OptionButtonView : ClickArea
{
    [Export] public OptionButton Kind { get; set; }

    public void ShowMarked(bool marked) =>
        GetNode<ProceduralPanel>("Cell").Fill = marked ? ProceduralFill.Selected : ProceduralFill.None;
}
