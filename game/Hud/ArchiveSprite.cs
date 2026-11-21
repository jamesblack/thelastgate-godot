using Godot;
using Godot.Collections;
using TheLastGate.Windows;

namespace TheLastGate.Hud;

[Tool]
public partial class ArchiveSprite : TextureRect, IHudContextUser
{
    private HudContext? context;
    private int darkSpriteNumber;
    private int spriteNumber;

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

    public void LoadSprite()
    {
        if (context is not { Sprites: { } sprites } shown)
        {
            return;
        }

        Texture = sprites.GetTexture(shown.DarkMode && darkSpriteNumber != 0 ? darkSpriteNumber : spriteNumber);
        ApplyFade();
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
