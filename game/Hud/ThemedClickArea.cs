using Godot;

namespace TheLastGate.Hud;

// A ClickArea that draws its theme type's button styles, so it keeps ClickArea's left and right Released signal.
[Tool]
public partial class ThemedClickArea : ClickArea
{
    private bool hovered;

    public StyleBox ShownStyle => GetThemeStylebox(hovered ? "hover" : "normal");

    public override void _Notification(int what)
    {
        if (what is not ((int)NotificationMouseEnter or (int)NotificationMouseExit))
        {
            return;
        }

        hovered = what == NotificationMouseEnter;
        QueueRedraw();
    }

    public override void _Draw() => DrawStyleBox(ShownStyle, new Rect2(Vector2.Zero, Size));
}
