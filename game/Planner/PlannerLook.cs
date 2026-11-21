using System.Linq;
using Godot;
using TheLastGate.Assets;

namespace TheLastGate.Planner;

// The planner's windows are framed by the game's window sprite.
public static class PlannerLook
{
    private const int FrameSprite = 18128;

    private static readonly Color LightBackground = Color.Color8(42, 15, 10, 240);
    private static readonly Color DarkBackground = Color.Color8(23, 0, 5);

    public static void ShowBackground(Control window, bool darkMode) =>
        ((StyleBoxFlat)window.GetThemeStylebox("panel")).BgColor = darkMode ? DarkBackground : LightBackground;

    public static void ApplyFrameSprite(Control window, SpriteLoader? sprites)
    {
        if (sprites?.GetTexture(FrameSprite) is not { } texture)
        {
            return;
        }

        foreach (var frame in window.FindChildren("Frame", nameof(NinePatchRect), true, false).Cast<NinePatchRect>())
        {
            frame.Texture = texture;
        }
    }
}
