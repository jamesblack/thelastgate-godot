using Godot;

namespace TheLastGate.Assets;

public sealed class LauncherPartFonts(ILauncherFontSet fonts) : IFontSet
{
    public bool CentresLikeTheCurrentClient => false;

    public GameFont For(TextColour colour) => new(fonts.Regular, fonts.Size, HudTint(colour));

    public GameFont Caption(TextColour colour) => new(fonts.Bold, fonts.Size, HudTint(colour));

    private static Color HudTint(TextColour colour) => ClassicFonts.SavedCaption(colour).Tint;
}
