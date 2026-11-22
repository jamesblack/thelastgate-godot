using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using TheLastGate.Config;
using TheLastGate.Map;

namespace TheLastGate.Assets;

public sealed class SpriteLoader : IDisposable
{
    private const string SpriteFolder = "res://sprites/";

    private readonly Dictionary<int, uint> averageColours = [];
    private readonly SpriteCache<Texture2D> cache;
    private readonly IReadOnlyDictionary<int, string> overrides;
    private readonly string uiFolder;
    private readonly Dictionary<int, Texture2D?> uiPictures = [];

    public SpriteLoader(string dataPath, long cacheBudgetBytes)
    {
        SpritePack.Mount(dataPath);
        overrides = SpriteOverrides.Find(Path.Combine(dataPath, DataLayout.Overrides), DataPaths.PlayerOverrides());
        uiFolder = Path.Combine(dataPath, DataLayout.Ui);
        cache = new SpriteCache<Texture2D>(cacheBudgetBytes, ReleaseTexture);
    }

    public IReadOnlyDictionary<int, int> Swaps { get; set; } = new Dictionary<int, int>();

    public void Dispose()
    {
        cache.Clear();
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

        return ResourceLoader.Exists(ResourcePath(spriteNumber)) ? SpriteSource.Resource : SpriteSource.None;
    }

    public Texture2D? GetTexture(int shownNumber)
    {
        var spriteNumber = Swaps.GetValueOrDefault(shownNumber, shownNumber);
        if (cache.TryGet(spriteNumber, out var cachedTexture))
        {
            return cachedTexture;
        }

        var texture = FindSource(spriteNumber) == SpriteSource.Resource
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
}
