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

    public static string CaptionVariation(TextColour colour) => "HudCaption" + colour;

    // Every label re-lays out its text on each changed signal, so the writes are made silently and announced once.
    public static void Apply(Theme theme, IFontSet fonts)
    {
        bool changed;
        theme.SetBlockSignals(true);
        try
        {
            changed = WriteAll(theme, fonts);
        }
        finally
        {
            theme.SetBlockSignals(false);
        }

        if (changed)
        {
            theme.EmitChanged();
        }
    }

    private static bool WriteAll(Theme theme, IFontSet fonts)
    {
        var changed = false;
        foreach (var colour in Enum.GetValues<TextColour>())
        {
            changed |= Write(theme, Variation(colour), fonts.For(colour));
            changed |= Write(theme, CaptionVariation(colour), fonts.Caption(colour));
        }

        var white = fonts.For(TextColour.White);
        if (theme.DefaultFont != white.Font)
        {
            theme.DefaultFont = white.Font;
            changed = true;
        }

        if (theme.DefaultFontSize != white.Size)
        {
            theme.DefaultFontSize = white.Size;
            changed = true;
        }

        return changed;
    }

    private static bool Write(Theme theme, string variation, GameFont font)
    {
        var changed = false;
        if (theme.GetFont(FontName, variation) != font.Font)
        {
            theme.SetFont(FontName, variation, font.Font);
            changed = true;
        }

        if (theme.GetFontSize(FontSizeName, variation) != font.Size)
        {
            theme.SetFontSize(FontSizeName, variation, font.Size);
            changed = true;
        }

        if (theme.GetColor(FontColourName, variation) != font.Tint)
        {
            theme.SetColor(FontColourName, variation, font.Tint);
            changed = true;
        }

        return changed;
    }
}
