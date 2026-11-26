using Godot;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralTextBox : TextEdit
{
    public override void _Ready() => ProceduralScrollBar.StyleFree(GetVScrollBar());
}
