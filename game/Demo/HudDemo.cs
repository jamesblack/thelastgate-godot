using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;
using TheLastGate.Game;
using TheLastGate.Hud;
using TheLastGate.State;

namespace TheLastGate.Demo;

public partial class HudDemo : Control
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    public override void _Ready()
    {
        var state = new GameState();
        var hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        AddChild(hud);
        hud.Sprites = new SpriteLoader(DataPaths.Root(), SpriteCacheBytes);
        hud.Show(state, new GameSession(state), new ClassicFonts(), _ => { });
        hud.Refresh(0);
    }
}
