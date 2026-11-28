using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TheLastGate.Assets;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public sealed record ArtSprite(string Sheet, PixelRect Region, PixelRect Used);

public static partial class ArtManifest
{
    public const string Extension = ".json";
    private const string ReservedName = "characters";

    public static string? SheetName(string fileName)
    {
        var match = ManifestFileName().Match(fileName);
        var name = match.Groups["name"].Value;
        return match.Success && name != ReservedName && !OnlyDigits().IsMatch(name) ? name : null;
    }

    public static IReadOnlyDictionary<int, ArtSprite> Parse(string sheet, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var sprites = new Dictionary<int, ArtSprite>();
            foreach (var entry in document.RootElement.GetProperty("sprites").EnumerateObject())
            {
                if (!SpriteKey().IsMatch(entry.Name))
                {
                    throw new InvalidDataException($"{sheet}: \"{entry.Name}\" isn't a sprite number");
                }

                sprites[int.Parse(entry.Name, CultureInfo.InvariantCulture)] = new ArtSprite(
                    sheet,
                    ReadRect(entry.Value),
                    ReadRect(entry.Value.GetProperty("used")));
            }

            return sprites;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException)
        {
            throw new InvalidDataException($"{sheet}: the manifest can't be read: {exception.Message}", exception);
        }
    }

    private static PixelRect ReadRect(JsonElement element) =>
        new(
            element.GetProperty("x").GetInt32(),
            element.GetProperty("y").GetInt32(),
            element.GetProperty("w").GetInt32(),
            element.GetProperty("h").GetInt32());

    [GeneratedRegex(@"^(?<name>[a-z0-9]+(?:-[a-z0-9]+)*)\.json$")]
    private static partial Regex ManifestFileName();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex OnlyDigits();

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex SpriteKey();
}
