using Godot;

namespace TheLastGate.Procedural;

// Godot stops passing a click up at a top_level control before it checks for Stop, so a top_level backdrop has to
// accept its clicks itself or the world under it takes them as well.
public partial class ModalBackdrop : ColorRect
{
    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton)
        {
            AcceptEvent();
        }
    }
}
