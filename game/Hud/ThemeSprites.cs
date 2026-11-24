using Godot;
using TheLastGate.Assets;

namespace TheLastGate.Hud;

// The sprites live in the archives rather than the project, so a theme's StyleBoxTexture names its sprite in
// metadata and gets the texture once the sprites are loaded. Not on the shared theme in the editor: saving a
// scene there would write the texture's pixels into the theme file. The editor gets a textured copy instead.
public static class ThemeSprites
{
    private const string SpriteKey = "sprite";

    public static Theme Textured(Theme theme, SpriteLoader sprites)
    {
        var copy = (Theme)theme.Duplicate(true);
        Texture(copy, sprites);
        return copy;
    }

    public static void Apply(Theme theme, SpriteLoader sprites)
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        Texture(theme, sprites);
    }

    private static void Texture(Theme theme, SpriteLoader sprites)
    {
        foreach (var type in theme.GetStyleboxTypeList())
        {
            foreach (var name in theme.GetStyleboxList(type))
            {
                if (theme.GetStylebox(name, type) is StyleBoxTexture box
                    && box.HasMeta(SpriteKey)
                    && sprites.GetTexture(box.GetMeta(SpriteKey).AsInt32()) is { } texture)
                {
                    box.Texture = texture;
                }
            }
        }
    }
}
