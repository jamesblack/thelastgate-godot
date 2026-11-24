using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;
using TheLastGate.Hud;

namespace TheLastGate.Demo;

// Stands in for the HUD: it shows the labels the classic font set, as HudView does for everything under it.
[Tool]
public partial class ProceduralDepotDemo : Control
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    private SpriteLoader? ownSprites;

    public override void _Ready()
    {
        var sprites = Engine.IsEditorHint()
            ? EditorSprites.Shared
            : ownSprites = new SpriteLoader(DataPaths.Root(), SpriteCacheBytes);
        if (sprites == null)
        {
            return;
        }

        var context = new HudContext(new ClassicFonts(), sprites, null, true);
        foreach (var node in FindChildren("*", "", true, false))
        {
            if (node is IHudContextUser user)
            {
                user.ShowContext(context);
            }

            // Spike only: lifts the font set's Grey (72, 81, 81) to the art's lettering colour (86, 87, 76).
            if (node is HudLabel label)
            {
                label.SelfModulate = new Color(86f / 72f, 87f / 81f, 76f / 81f);
            }
        }
    }

    public override void _ExitTree() => ownSprites?.Dispose();
}
