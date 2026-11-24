using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;
using TheLastGate.Hud;

namespace TheLastGate.Demo;

[Tool]
public partial class GoldLineDemo : Control
{
    private SpriteLoader? sprites;
    private EditorThemePreview? themePreview;

    public override void _Notification(int what) => themePreview?.Notify(what);

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            themePreview = EditorThemePreview.Show(this);
            return;
        }

        sprites = new SpriteLoader(DataPaths.Root(), 64L * 1024 * 1024);
        if (Theme != null)
        {
            ThemeSprites.Apply(Theme, sprites);
        }
    }

    public override void _ExitTree() => sprites?.Dispose();
}
