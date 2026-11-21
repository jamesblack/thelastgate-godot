using Godot;
using Godot.Collections;

namespace TheLastGate.Hud;

[Tool]
public partial class UiPicture : TextureRect, IHudContextUser
{
    private HudContext? context;
    private int darkPictureNumber;
    private int pictureNumber;

    public UiPicture() => AddToGroup(HudContext.Group);

    [Export]
    public int PictureNumber
    {
        get => pictureNumber;
        set
        {
            pictureNumber = value;
            LoadPicture();
        }
    }

    [Export]
    public int DarkPictureNumber
    {
        get => darkPictureNumber;
        set
        {
            darkPictureNumber = value;
            LoadPicture();
        }
    }

    public void ShowContext(HudContext shown)
    {
        context = shown;
        LoadPicture();
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

    private void LoadPicture()
    {
        if (context is not { Sprites: { } sprites } shown)
        {
            return;
        }

        Texture = sprites.GetUiPicture(shown.DarkMode && darkPictureNumber != 0 ? darkPictureNumber : pictureNumber);
    }
}
