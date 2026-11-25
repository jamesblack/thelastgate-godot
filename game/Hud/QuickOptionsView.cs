using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheLastGate.Hud;

public partial class QuickOptionsView : Control
{
    public IEnumerable<OptionButtonView> Buttons =>
        GetNode("Band/Buttons")
            .GetChildren()
            .Append(GetNode("OptionsBox/Options"))
            .Append(GetNode("ExitBox/Exit"))
            .Cast<OptionButtonView>();
}
