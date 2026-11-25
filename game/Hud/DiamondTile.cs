using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Hud;

// Lights up under the mouse without taking its clicks, which pass on to the world as they did before the tile was drawn.
[Tool]
public partial class DiamondTile : ProceduralPanel
{
    public DiamondTile()
    {
        MouseEntered += () => Modulate = ClickArea.Hovered;
        MouseExited += () => Modulate = Colors.White;
    }

    public override bool _HasPoint(Vector2 point) =>
        ProceduralLook.InDiamond(point, new Rect2(Vector2.Zero, Size))
        && (Hole.Size.Y == 0 || !ProceduralLook.InDiamond(point, Hole));
}
