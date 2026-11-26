using System.Collections.Generic;
using Godot;

namespace TheLastGate.Assets;

// White letters tinted to each classic colour, which the captions already carry, so a colour reads the same in every
// font. The line is fitted to the classic 9 px line, so text lines up wherever the HUD expects classic rows.
public sealed class TrueTypeFonts(FontEntry entry, Font file) : IFontSet
{
    private readonly Font fitted = Fitted(file, entry.Size, entry.Offset);
    private readonly Dictionary<TextColour, GameFont> fonts = [];

    public bool CentresLikeTheCurrentClient => false;

    public GameFont For(TextColour colour)
    {
        if (!fonts.TryGetValue(colour, out var known))
        {
            known = new GameFont(fitted, entry.Size, ClassicFonts.SavedCaption(colour).Tint);
            fonts[colour] = known;
        }

        return known;
    }

    public GameFont Caption(TextColour colour) => ClassicFonts.SavedCaption(colour);

    private static FontVariation Fitted(Font file, int size, int offset)
    {
        var spacing = LineFit.Fit(file.GetHeight(size), LineFit.ClassicLineHeight, offset);
        return new FontVariation { BaseFont = file, SpacingTop = spacing.Top, SpacingBottom = spacing.Bottom };
    }
}
