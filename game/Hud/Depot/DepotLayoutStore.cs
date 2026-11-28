using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TheLastGate.Characters;
using static TheLastGate.Hud.Depot.DepotLayout;

namespace TheLastGate.Hud.Depot;

public static class DepotLayoutStore
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Save(DepotLayouts layouts)
    {
        var seasons = Enum.GetValues<Season>()
            .ToDictionary(
                season => season.ToString(),
                season => ToFile(layouts.For(season)));
        return JsonSerializer.Serialize(new FileModel(CurrentVersion, seasons), Options);
    }

    public static DepotLayouts? Load(string json)
    {
        FileModel? file;
        try
        {
            file = JsonSerializer.Deserialize<FileModel>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (file is not { Version: CurrentVersion, Seasons: { } seasons })
        {
            return null;
        }

        var layouts = new DepotLayouts();
        foreach (var season in Enum.GetValues<Season>())
        {
            if (seasons.TryGetValue(season.ToString(), out var model) && model != null)
            {
                layouts.Set(season, Repaired(model));
            }
        }

        return layouts;
    }

    private static SeasonModel ToFile(DepotLayout layout) => new(
        layout.NextCategoryId,
        layout.Categories.Cast<SavedCategory?>().ToList(),
        layout.Pages.Cast<SavedPage?>().ToList());

    private static DepotLayout Repaired(SeasonModel model)
    {
        var categories = new List<SavedCategory>();
        var ids = new HashSet<int>();
        foreach (var category in model.Categories ?? [])
        {
            if (category is { Id: > 0 } && ids.Add(category.Id))
            {
                categories.Add(category with { Name = category.Name ?? "" });
            }
        }

        for (var index = 0; index < categories.Count; index++)
        {
            if (categories[index].ParentId != 0 && !ids.Contains(categories[index].ParentId))
            {
                categories[index] = categories[index] with { ParentId = 0 };
            }
        }

        CutCycles(categories);

        var pages = new List<SavedPage>();
        var seen = new HashSet<int>();
        foreach (var page in model.Pages ?? [])
        {
            if (page == null || !seen.Add(PageId(page.CharacterNumber, page.Page)))
            {
                continue;
            }

            if (ids.Contains(page.ParentId))
            {
                pages.Add(page with { Name = page.Name ?? "" });
            }
            else if (HomeOf(categories, page.CharacterNumber) is { } home)
            {
                pages.Add(page with { ParentId = home, Name = page.Name ?? "" });
            }
        }

        return FromSaved(categories, pages, model.NextId);
    }

    private static int? HomeOf(List<SavedCategory> categories, int characterNumber) =>
        categories.FirstOrDefault(category => category.CharacterNumber == characterNumber)?.Id
        ?? categories.FirstOrDefault()?.Id;

    private static void CutCycles(List<SavedCategory> categories)
    {
        var parentOf = categories.ToDictionary(category => category.Id, category => category.ParentId);
        var fileOrder = categories.Select((category, index) => (category.Id, index)).ToDictionary();
        foreach (var start in categories)
        {
            var path = new List<int>();
            var current = start.Id;
            while (current != 0 && !path.Contains(current))
            {
                path.Add(current);
                current = parentOf[current];
            }

            if (current != 0)
            {
                var members = path.Skip(path.IndexOf(current));
                parentOf[members.MinBy(member => fileOrder[member])] = 0;
            }
        }

        for (var index = 0; index < categories.Count; index++)
        {
            categories[index] = categories[index] with { ParentId = parentOf[categories[index].Id] };
        }
    }

    private sealed record FileModel(int Version, Dictionary<string, SeasonModel>? Seasons);

    private sealed record SeasonModel(int NextId, List<SavedCategory?>? Categories, List<SavedPage?>? Pages);
}
