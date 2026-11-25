using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheLastGate.Hud;

public partial class HotbarView : Control
{
    public IEnumerable<HotbarButtonView> Buttons => GetNode("%Buttons").GetChildren().Cast<HotbarButtonView>();
}
