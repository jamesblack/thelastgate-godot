using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public static class ChangelogText
{
    private const string LatestText = "You have the latest version.";

    public static string Bbcode(IEnumerable<AvailableRelease> releases)
    {
        var text = new StringBuilder();
        foreach (var release in releases)
        {
            var notes = release.Notes;
            if (text.Length > 0)
            {
                text.Append('\n');
            }

            Line(
                text,
                TextColour.White,
                notes.Date is { } date
                    ? $"Version {release.Version} ({date:yyyy-MM-dd})"
                    : $"Version {release.Version}");
            foreach (var section in notes.Sections)
            {
                if (section.Title.Length > 0)
                {
                    Line(text, TextColour.Grey, section.Title);
                }

                foreach (var item in section.Items)
                {
                    Line(text, TextColour.Yellow, "- " + item);
                }
            }
        }

        if (text.Length == 0)
        {
            Line(text, TextColour.Grey, LatestText);
        }

        return text.ToString().TrimEnd('\n');
    }

    public static void Fill(RichTextLabel label, IEnumerable<AvailableRelease> releases, ILauncherFontSet fonts)
    {
        label.AddThemeFontOverride("normal_font", fonts.Regular);
        label.AddThemeFontSizeOverride("normal_font_size", fonts.Size);
        label.Text = Bbcode(releases.ToList());
    }

    private static void Line(StringBuilder text, TextColour colour, string line) =>
        text.Append("[color=#")
            .Append(ClassicFonts.SavedCaption(colour).Tint.ToHtml(false))
            .Append(']')
            .Append(line.Replace("[", "[lb]"))
            .Append("[/color]\n");
}
