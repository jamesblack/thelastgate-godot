using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TheLastGate.Assets;

public sealed record FontCatalogue(IReadOnlyList<FontEntry> Fonts, IReadOnlyList<string> Problems)
{
    public static readonly FontCatalogue Empty = new([], []);

    public static FontCatalogue Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return new FontCatalogue([], [$"The font list isn't valid JSON: {exception.Message}"]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new FontCatalogue([], ["The font list should be a JSON array of fonts."]);
            }

            var fonts = new List<FontEntry>();
            var problems = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (Entry(element, out var problem) is not { } entry)
                {
                    problems.Add(problem);
                }
                else if (entry.Id == FontChoice.ClassicId)
                {
                    problems.Add($"\"{entry.Id}\" is the built-in font's id, so the entry was left out.");
                }
                else if (fonts.Any(font => font.Id == entry.Id))
                {
                    problems.Add($"\"{entry.Id}\" is listed twice; only the first is used.");
                }
                else
                {
                    fonts.Add(entry);
                }
            }

            return new FontCatalogue(fonts, problems);
        }
    }

    private static FontEntry? Entry(JsonElement element, out string problem)
    {
        var id = Text(element, "id") ?? "(no id)";
        var name = Text(element, "name");
        var file = Text(element, "file");
        var size = Number(element, "size");
        var offset = Number(element, "offset");
        var smooth = Flag(element, "smooth");
        if (name == null || file == null || size == null || offset == null || smooth == null)
        {
            problem = $"\"{id}\" is missing a field, so it was left out.";
            return null;
        }

        if (size <= 0)
        {
            problem = $"\"{id}\" has a size below 1, so it was left out.";
            return null;
        }

        if (Path.GetFileName(file) != file)
        {
            problem = $"\"{id}\" names a file outside the fonts folder, so it was left out.";
            return null;
        }

        problem = "";
        return new FontEntry(id, name, file, size.Value, offset.Value, smooth.Value);
    }

    private static string? Text(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? Number(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.TryGetInt32(out var number)
            ? number
            : null;

    private static bool? Flag(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
}
