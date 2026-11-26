using Godot;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralTextBox : TextEdit
{
    // A column bar fills its 9-pixel column less the padding; this one has no column to fill.
    private const int ScrollBarWidth = 7;

    public override void _Ready()
    {
        var bar = GetVScrollBar();
        ProceduralScrollBar.Style(bar);
        bar.UseParentMaterial = false;
        bar.CustomMinimumSize = new Vector2(ScrollBarWidth, 0);
    }
}
