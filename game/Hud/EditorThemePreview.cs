using Godot;

namespace TheLastGate.Hud;

// The editor shows a window with a textured copy of its theme, and puts the real theme back while the scene is
// saved, so the copy's textures never reach the scene file.
public sealed class EditorThemePreview
{
    private readonly Theme original;
    private readonly Theme textured;
    private readonly Control window;

    private EditorThemePreview(Control window, Theme original, Theme textured)
    {
        this.window = window;
        this.original = original;
        this.textured = textured;
        window.Theme = textured;
    }

    public static EditorThemePreview? Show(Control window)
    {
        if (!Engine.IsEditorHint() || window.Theme is not { } theme || EditorSprites.Shared is not { } sprites)
        {
            return null;
        }

        return new EditorThemePreview(window, theme, ThemeSprites.Textured(theme, sprites));
    }

    public void Notify(int what)
    {
        if (what == Node.NotificationEditorPreSave)
        {
            window.Theme = original;
        }
        else if (what == Node.NotificationEditorPostSave)
        {
            window.Theme = textured;
        }
    }
}
