using Godot;

namespace TheLastGate.Hud;

public partial class ChatView : Control
{
    public ChatLog Log => GetNode<ChatLog>("%ChatLog");

    public ChatLine Line => GetNode<ChatLine>("%ChatLine");
}
