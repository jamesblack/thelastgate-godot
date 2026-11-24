using Godot;

namespace TheLastGate.Assets;

public sealed class ClassicFonts : IFontSet
{
    // Its own copy of the theme file: the shared theme takes whichever font set the game swaps in.
    private static readonly Theme Saved = ResourceLoader.Load<Theme>(
        HudTextTheme.Path,
        null,
        ResourceLoader.CacheMode.Ignore);

    public bool CentresLikeTheCurrentClient => true;

    public GameFont For(TextColour colour)
    {
        var variation = HudTextTheme.Variation(colour);
        return new GameFont(
            Saved.GetFont("font", variation),
            Saved.GetFontSize("font_size", variation),
            Saved.GetColor("font_color", variation));
    }
}
