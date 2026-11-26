using Godot;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

[Tool]
public partial class WarningMark : HudLabel
{
    public override GodotObject? _MakeCustomTooltip(string forText) => ProceduralTooltip.Make(forText);
}
