using Godot;
using TheLastGate.Assets;

namespace TheLastGate.World;

public partial class SpriteProbeView : PanelContainer
{
    private Label readout = null!;

    public WorldView World { get; set; } = null!;

    public static bool IsAvailable => OS.HasFeature("editor");

    public override void _Ready() => readout = GetNode<Label>("%Readout");

    public void UseFont(GameFont font)
    {
        readout.AddThemeFontOverride("font", font.Font);
        readout.AddThemeFontSizeOverride("font_size", font.Size);
        readout.AddThemeColorOverride("font_color", font.Tint);
    }

    public override void _Process(double delta)
    {
        var probed = World.ProbeUnder(GetViewport().GetMousePosition());
        Visible = probed != null;
        if (probed is { } tile)
        {
            readout.Text = $"floor {Number(tile.Floor)}   object {Number(tile.Object)}   "
                + $"character {Number(tile.Character)}   light {tile.Light}";
        }
    }

    private static string Number(int spriteNumber) => spriteNumber == 0 ? "-" : spriteNumber.ToString();
}
