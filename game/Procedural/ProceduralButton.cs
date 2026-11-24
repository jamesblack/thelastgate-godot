using Godot;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

[Tool]
public partial class ProceduralButton : ProceduralPanel
{
    // ClickArea is not a tool script, so in the editor the click area is a plain Control and there is nothing to follow.
    public override void _Ready()
    {
        if (GetNode("Click") is ClickArea click)
        {
            click.ShadeChanged += shade => Modulate = shade;
        }
    }
}
