using Godot;
using Godot.Collections;
using TheLastGate.Windows;

namespace TheLastGate.Hud;

[Tool]
public partial class ArchiveSprite : TextureRect, IHudContextUser
{
    private static readonly System.Collections.Generic.Dictionary<Texture2D, AtlasTexture> Trimmed = [];

    private HudContext? context;
    private int darkSpriteNumber;
    private int spriteNumber;
    private bool trimToDrawnArea;

    public ArchiveSprite() => AddToGroup(HudContext.Group);

    [Export]
    public int SpriteNumber
    {
        get => spriteNumber;
        set
        {
            spriteNumber = value;
            LoadSprite();
        }
    }

    [Export]
    public int DarkSpriteNumber
    {
        get => darkSpriteNumber;
        set
        {
            darkSpriteNumber = value;
            LoadSprite();
        }
    }

    [Export] public WindowBackground FadesAs { get; set; }

    // Many sprites sit in a corner of a larger canvas (the skill icons are 26 pixels drawn in a 32 pixel image), so
    // centring the whole image puts the drawing off centre. Trimmed, the drawn pixels are what gets centred.
    [Export]
    public bool TrimToDrawnArea
    {
        get => trimToDrawnArea;
        set
        {
            trimToDrawnArea = value;
            LoadSprite();
        }
    }

    public void ShowContext(HudContext shown)
    {
        context = shown;
        LoadSprite();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() != TextureRect.PropertyName.Texture)
        {
            return;
        }

        var usage = property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage;
        property["usage"] = (int)(usage | PropertyUsageFlags.ReadOnly);
    }

    // In the editor a scene opened on its own has no HUD to hand it sprites, so it shows the dark sprite from the
    // editor's own loader.
    public void LoadSprite()
    {
        if (context is { Sprites: { } sprites } shown)
        {
            Texture = Shown(
                sprites.GetTexture(shown.DarkMode && darkSpriteNumber != 0 ? darkSpriteNumber : spriteNumber));
            ApplyFade();
        }
        else if (EditorSprites.Shared is { } editorSprites)
        {
            Texture = Shown(editorSprites.GetTexture(darkSpriteNumber != 0 ? darkSpriteNumber : spriteNumber));
        }
    }

    private Texture2D? Shown(Texture2D? texture)
    {
        if (!trimToDrawnArea || texture == null)
        {
            return texture;
        }

        if (!Trimmed.TryGetValue(texture, out var trimmed))
        {
            trimmed = new AtlasTexture { Atlas = texture, Region = texture.GetImage().GetUsedRect() };
            Trimmed[texture] = trimmed;
        }

        return trimmed;
    }

    public void ApplyFade()
    {
        if (FadesAs == WindowBackground.None || context?.ClientSettings is not { } settings)
        {
            return;
        }

        SelfModulate = Colors.White with { A = settings.BackgroundOpacity[FadesAs] / (float)WindowBackgrounds.Solid };
    }
}
