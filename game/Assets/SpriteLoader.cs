using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using TheLastGate.Config;
using TheLastGate.Map;

namespace TheLastGate.Assets;

public sealed class SpriteLoader : IDisposable
{
    private const string SpriteFolder = "res://sprites/";
    private const string SheetFolder = SpriteFolder + "characters/";

    private readonly Dictionary<int, uint> averageColours = [];
    private readonly SpriteCache<Texture2D> cache;
    private readonly Dictionary<LoadedSheet, bool[]> frameMasks = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int, LoadedSheet> loadedSheets = [];
    private readonly IReadOnlyDictionary<int, string> overrides;
    private readonly CharacterSheet[] sheets;
    private readonly string uiFolder;
    private readonly Dictionary<int, Texture2D?> uiPictures = [];

    public SpriteLoader(string dataPath, long cacheBudgetBytes)
    {
        SpritePack.Mount(dataPath);
        overrides = SpriteOverrides.Find(Path.Combine(dataPath, DataLayout.Overrides), DataPaths.PlayerOverrides());
        uiFolder = Path.Combine(dataPath, DataLayout.Ui);
        sheets = FindSheets();
        cache = new SpriteCache<Texture2D>(cacheBudgetBytes, ReleaseTexture);
    }

    public IReadOnlyDictionary<int, int> Swaps { get; set; } = new Dictionary<int, int>();

    public void Dispose()
    {
        cache.Clear();
        foreach (var sheet in loadedSheets.Values)
        {
            if (sheet.Texture != null)
            {
                ReleaseTexture(sheet.Texture);
            }
        }

        loadedSheets.Clear();
        frameMasks.Clear();
        foreach (var picture in uiPictures.Values)
        {
            picture?.Dispose();
        }

        uiPictures.Clear();
    }

    // A loose override comes first, then the sprite resource.
    public SpriteSource FindSource(int spriteNumber)
    {
        if (overrides.ContainsKey(spriteNumber))
        {
            return SpriteSource.Override;
        }

        if (FindSheetCell(spriteNumber) is { } sheetCell)
        {
            return sheetCell.Sheet.Texture == null ? SpriteSource.None : SpriteSource.Resource;
        }

        return ResourceLoader.Exists(ResourcePath(spriteNumber)) ? SpriteSource.Resource : SpriteSource.None;
    }

    // An empty cell of a sheet still loads, as a transparent frame. This tells it apart, at the cost of reading the
    // sheet's pixels once.
    public bool HasFrame(int spriteNumber) =>
        FindSheetCell(spriteNumber) is { } sheetCell
            ? FramesOn(sheetCell)[sheetCell.Offset]
            : FindSource(spriteNumber) != SpriteSource.None;

    public Texture2D? GetTexture(int shownNumber)
    {
        var spriteNumber = Swaps.GetValueOrDefault(shownNumber, shownNumber);
        if (cache.TryGet(spriteNumber, out var cachedTexture))
        {
            return cachedTexture;
        }

        var texture = FindSheetCell(spriteNumber) is { } sheetCell
            ? SheetFrame(sheetCell)
            : FindSource(spriteNumber) == SpriteSource.Resource
                ? ResourceLoader.Load<Texture2D>(ResourcePath(spriteNumber))
                : LoadImage(spriteNumber) is { } image
                    ? ImageTexture.CreateFromImage(image)
                    : null;
        if (texture == null)
        {
            return null;
        }

        cache.Add(
            spriteNumber,
            texture,
            (long)texture.GetWidth() * texture.GetHeight() * SpritePixels.Rgba8BytesPerPixel);
        return texture;
    }

    // Kept apart from the texture cache, so evicting a texture never costs a colour the explored map asks for often.
    public uint AverageColour(int shownNumber)
    {
        var spriteNumber = Swaps.GetValueOrDefault(shownNumber, shownNumber);
        if (!averageColours.TryGetValue(spriteNumber, out var colour))
        {
            colour = LoadImage(spriteNumber) is { } image ? SpriteColours.Average(image.GetData()) : 0;
            averageColours[spriteNumber] = colour;
        }

        return colour;
    }

    public Texture2D? GetUiPicture(int pictureNumber)
    {
        if (uiPictures.TryGetValue(pictureNumber, out var known))
        {
            return known;
        }

        var path = Path.Combine(uiFolder, pictureNumber.ToString("00000", CultureInfo.InvariantCulture) + ".png");
        var image = new Image();
        Texture2D? picture = null;
        if (File.Exists(path) && image.LoadPngFromBuffer(File.ReadAllBytes(path)) == Error.Ok)
        {
            picture = ImageTexture.CreateFromImage(image);
        }
        else
        {
            GD.PushWarning($"UI picture {pictureNumber} couldn't be loaded from {path}");
        }

        uiPictures[pictureNumber] = picture;
        return picture;
    }

    private Image? LoadImage(int spriteNumber)
    {
        try
        {
            if (FindSheetCell(spriteNumber) is { } sheetCell)
            {
                return SheetImage(sheetCell);
            }

            return FindSource(spriteNumber) switch
            {
                SpriteSource.Override => DecodeOverride(spriteNumber, overrides[spriteNumber]),
                SpriteSource.Resource => ResourceImage(spriteNumber),
                _ => null,
            };
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            GD.PushError($"Sprite {spriteNumber} couldn't be read: {exception.Message}");
            return null;
        }
    }

    private static CharacterSheet[] FindSheets() =>
        ResourceLoader.ListDirectory(SheetFolder)
            .Select(CharacterSheet.FromFileName)
            .OfType<CharacterSheet>()
            .ToArray();

    // An override that doesn't fit its cell is served on its own, so it isn't treated as a cell of the sheet.
    private SheetCell? FindSheetCell(int spriteNumber)
    {
        foreach (var sheet in sheets)
        {
            if (sheet.TryGetOffset(spriteNumber, out var offset))
            {
                var loaded = LoadSheet(sheet);
                return loaded.OwnTextureOffsets.Contains(offset) ? null : new SheetCell(loaded, offset);
            }
        }

        return null;
    }

    private LoadedSheet LoadSheet(CharacterSheet sheet)
    {
        if (loadedSheets.TryGetValue(sheet.BaseSprite, out var known))
        {
            return known;
        }

        var imported = ResourceLoader.Load<Texture2D>(SheetFolder + sheet.FileName);
        if (imported == null)
        {
            GD.PushError($"The character sheet {sheet.FileName} couldn't be loaded, so its frames won't show.");
            var unreadable = new LoadedSheet(null, 0, 0, []);
            loadedSheets[sheet.BaseSprite] = unreadable;
            return unreadable;
        }

        var cellWidth = imported.GetWidth() / CharacterSheet.Columns;
        var cellHeight = imported.GetHeight() / CharacterSheet.Rows;
        var fittingOverrides = new Dictionary<int, Image>();
        var ownTextureOffsets = new HashSet<int>();
        for (var offset = 0; offset < CharacterSheet.Slots; offset++)
        {
            var spriteNumber = sheet.BaseSprite + offset;
            if (!overrides.ContainsKey(spriteNumber))
            {
                continue;
            }

            if (LoadOverrideImage(spriteNumber) is { } frame
                && frame.GetWidth() == cellWidth
                && frame.GetHeight() == cellHeight)
            {
                fittingOverrides[offset] = frame;
            }
            else
            {
                ownTextureOffsets.Add(offset);
            }
        }

        var texture = fittingOverrides.Count == 0
            ? imported
            : DrawOverrides(sheet, imported, cellWidth, cellHeight, fittingOverrides);
        var loaded = new LoadedSheet(texture, cellWidth, cellHeight, ownTextureOffsets);
        loadedSheets[sheet.BaseSprite] = loaded;
        return loaded;
    }

    // Reading a sheet's pixels back costs several milliseconds, so it happens only for a sheet with overrides.
    private static Texture2D DrawOverrides(
        CharacterSheet sheet,
        Texture2D imported,
        int cellWidth,
        int cellHeight,
        Dictionary<int, Image> fittingOverrides)
    {
        if (imported.GetImage() is not { } image)
        {
            GD.PushError($"The character sheet {sheet.FileName} couldn't be read, so its overrides won't show.");
            return imported;
        }

        image.Convert(Image.Format.Rgba8);
        var pixels = image.GetData();
        foreach (var (offset, frame) in fittingOverrides)
        {
            SheetCells.Blit(pixels, image.GetWidth(), offset, cellWidth, cellHeight, frame.GetData());
        }

        return ImageTexture.CreateFromImage(
            Image.CreateFromData(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, pixels));
    }

    private bool[] FramesOn(SheetCell sheetCell)
    {
        if (frameMasks.TryGetValue(sheetCell.Sheet, out var known))
        {
            return known;
        }

        var image = sheetCell.Sheet.Texture?.GetImage();
        image?.Convert(Image.Format.Rgba8);
        var used = image == null
            ? new bool[CharacterSheet.Slots]
            : SheetCells.UsedCells(image.GetData(), image.GetWidth(), image.GetHeight());
        frameMasks[sheetCell.Sheet] = used;
        return used;
    }

    private static AtlasTexture? SheetFrame(SheetCell sheetCell)
    {
        var sheet = sheetCell.Sheet;
        if (sheet.Texture == null)
        {
            return null;
        }

        var (cellX, cellY) = SheetCells.CellOrigin(sheetCell.Offset, sheet.CellWidth, sheet.CellHeight);
        return new AtlasTexture
        {
            Atlas = sheet.Texture,
            Region = new Rect2(cellX, cellY, sheet.CellWidth, sheet.CellHeight),
        };
    }

    private static Image? SheetImage(SheetCell sheetCell)
    {
        var sheet = sheetCell.Sheet;
        if (sheet.Texture?.GetImage() is not { } image)
        {
            return null;
        }

        image.Convert(Image.Format.Rgba8);
        var frame = SheetCells.Extract(
            image.GetData(),
            image.GetWidth(),
            sheetCell.Offset,
            sheet.CellWidth,
            sheet.CellHeight);
        return Image.CreateFromData(sheet.CellWidth, sheet.CellHeight, false, Image.Format.Rgba8, frame);
    }

    private Image? LoadOverrideImage(int spriteNumber)
    {
        try
        {
            return DecodeOverride(spriteNumber, overrides[spriteNumber]);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            GD.PushError($"Sprite {spriteNumber} couldn't be read: {exception.Message}");
            return null;
        }
    }

    private static string ResourcePath(int spriteNumber) =>
        SpriteFolder + spriteNumber.ToString("00000", CultureInfo.InvariantCulture) + ".png";

    private static Image? ResourceImage(int spriteNumber)
    {
        var image = ResourceLoader.Load<Texture2D>(ResourcePath(spriteNumber))?.GetImage();
        image?.Convert(Image.Format.Rgba8);
        return image;
    }

    // A resource texture is shared with Godot's own resource cache, so only textures made here are disposed.
    private static void ReleaseTexture(Texture2D texture)
    {
        if (texture is ImageTexture)
        {
            texture.Dispose();
        }
    }

    private static Image? DecodeOverride(int spriteNumber, string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);
        return Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? DecodePng(spriteNumber, fileBytes)
            : Decode(spriteNumber, image => image.LoadBmpFromBuffer(fileBytes));
    }

    private static Image? DecodePng(int spriteNumber, byte[] pngBytes) =>
        Decode(spriteNumber, image => image.LoadPngFromBuffer(pngBytes));

    private static Image? Decode(int spriteNumber, Func<Image, Error> load)
    {
        var image = new Image();
        var result = load(image);
        if (result != Error.Ok)
        {
            GD.PushWarning($"Sprite {spriteNumber} couldn't be decoded: {result}");
            return null;
        }

        image.Convert(Image.Format.Rgba8);
        var pixels = image.GetData();
        SpritePixels.ApplyMagentaKey(pixels);
        return Image.CreateFromData(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, pixels);
    }

    private sealed record LoadedSheet(
        Texture2D? Texture,
        int CellWidth,
        int CellHeight,
        HashSet<int> OwnTextureOffsets);

    private readonly record struct SheetCell(LoadedSheet Sheet, int Offset);
}
