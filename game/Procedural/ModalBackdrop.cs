using Godot;

namespace TheLastGate.Procedural;

// A window lifts its backdrops into its overlays, where each covers the whole screen. It accepts its clicks itself
// so the world under it never takes them.
public partial class ModalBackdrop : ColorRect
{
    public ModalBackdrop() => AddToGroup(WindowOverlays.Group);

    public override void _EnterTree() => GetViewport().SizeChanged += CoverTheScreen;

    public override void _ExitTree() => GetViewport().SizeChanged -= CoverTheScreen;

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && Visible)
        {
            CoverTheScreen();
        }
    }

    private void CoverTheScreen()
    {
        GlobalPosition = Vector2.Zero;
        Size = GetViewportRect().Size;
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton)
        {
            AcceptEvent();
        }
    }
}
