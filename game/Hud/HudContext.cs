using TheLastGate.Assets;
using TheLastGate.Config;

namespace TheLastGate.Hud;

// What HudView hands down to the nodes inside it that draw with its fonts, sprites, settings or dark mode.
public sealed record HudContext(IFontSet? Fonts, SpriteLoader? Sprites, ClientSettings? ClientSettings, bool DarkMode)
{
    public const string Group = "hud_context";
}

// A node in HudContext.Group is shown the context when it enters the HUD and whenever the context changes.
public interface IHudContextUser
{
    void ShowContext(HudContext context);
}
