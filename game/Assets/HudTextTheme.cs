using System;
using Godot;

namespace TheLastGate.Assets;

public static class HudTextTheme
{
    public const string Path = "res://themes/HudTextTheme.tres";

    private const string FontName = "font";
    private const string FontSizeName = "font_size";
    private const string FontColourName = "font_color";

    public static Theme Load() => GD.Load<Theme>(Path);

    public static string Variation(TextColour colour) => "Hud" + colour;

    // Every change redraws all the text that uses the theme, so only what differs is written.
    public static void Apply(Theme theme, IFontSet fonts)
    {
        foreach (var colour in Enum.GetValues<TextColour>())
        {
            var font = fonts.For(colour);
            var variation = Variation(colour);
            if (theme.GetFont(FontName, variation) != font.Font)
            {
                theme.SetFont(FontName, variation, font.Font);
            }

            if (theme.GetFontSize(FontSizeName, variation) != font.Size)
            {
                theme.SetFontSize(FontSizeName, variation, font.Size);
            }

            if (theme.GetColor(FontColourName, variation) != font.Tint)
            {
                theme.SetColor(FontColourName, variation, font.Tint);
            }
        }

        var white = fonts.For(TextColour.White);
        if (theme.DefaultFont != white.Font)
        {
            theme.DefaultFont = white.Font;
        }

        if (theme.DefaultFontSize != white.Size)
        {
            theme.DefaultFontSize = white.Size;
        }
    }
}
