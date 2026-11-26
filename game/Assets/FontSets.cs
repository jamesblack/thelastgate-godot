using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace TheLastGate.Assets;

public sealed class FontSets
{
    private readonly ClassicFonts classic = new();
    private readonly Dictionary<string, TrueTypeFonts> sets = [];

    public FontSets(FontCatalogue catalogue, string dataRoot)
    {
        foreach (var problem in catalogue.Problems)
        {
            GD.PushWarning($"Font list: {problem}");
        }

        var available = new List<FontEntry>();
        foreach (var entry in catalogue.Fonts)
        {
            if (Load(Path.Combine(dataRoot, DataLayout.Fonts, entry.File), entry) is not { } font)
            {
                continue;
            }

            sets[entry.Id] = new TrueTypeFonts(entry, font);
            available.Add(entry);
        }

        Available = available;
    }

    public IReadOnlyList<FontEntry> Available { get; }

    public static FontCatalogue ReadCatalogue(string dataRoot)
    {
        var path = Path.Combine(dataRoot, DataLayout.Fonts, DataLayout.FontCatalogueFile);
        try
        {
            return FontCatalogue.Parse(File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning(
                $"Only the classic font is offered, because the font list couldn't be read: {exception.Message}");
            return FontCatalogue.Empty;
        }
    }

    public IFontSet For(string? savedId) =>
        FontChoice.Resolve(savedId, Available) is { } entry ? sets[entry.Id] : classic;

    // A font left in Git LFS without being pulled is a short text file, which fails to load here.
    private static Font? Load(string path, FontEntry entry)
    {
        if (!File.Exists(path))
        {
            GD.PushWarning($"The font \"{entry.Name}\" was left out, because {entry.File} is missing.");
            return null;
        }

        var file = new FontFile();
        if (file.LoadDynamicFont(path) != Error.Ok)
        {
            GD.PushWarning($"The font \"{entry.Name}\" was left out, because {entry.File} couldn't be loaded.");
            return null;
        }

        file.GenerateMipmaps = false;
        file.Antialiasing = entry.Smooth ? TextServer.FontAntialiasing.Gray : TextServer.FontAntialiasing.None;
        if (!entry.Smooth)
        {
            file.Hinting = TextServer.Hinting.None;
            file.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
        }

        return file;
    }
}
