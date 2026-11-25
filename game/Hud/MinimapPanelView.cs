using Godot;

namespace TheLastGate.Hud;

public partial class MinimapPanelView : Control
{
    public ClickArea Map => GetNode<ClickArea>("%Minimap");

    public ClickArea ZoomIn => GetNode<ClickArea>("%MinimapZoomIn");

    public ClickArea ZoomOut => GetNode<ClickArea>("%MinimapZoomOut");

    public void ShowLocation(string location) => GetNode<Label>("%Location").Text = location;

    public void ShowPicture(Texture2D picture) => GetNode<TextureRect>("%Picture").Texture = picture;
}
