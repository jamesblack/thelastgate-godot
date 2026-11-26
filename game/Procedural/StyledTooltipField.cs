using Godot;

namespace TheLastGate.Procedural;

[Tool]
public partial class StyledTooltipField : LineEdit
{
    public override GodotObject? _MakeCustomTooltip(string forText) => ProceduralTooltip.Make(forText);
}
