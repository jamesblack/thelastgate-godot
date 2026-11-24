using Godot;
using TheLastGate.Config;
using TheLastGate.Windows;

namespace TheLastGate.Hud;

// SelfModulate fades only the backdrop's own stylebox, so the controls inside it stay solid.
[Tool]
public partial class WindowBackdrop : PanelContainer, IHudContextUser
{
    public WindowBackdrop() => AddToGroup(HudContext.Group);

    [Export] public WindowBackground FadesAs { get; set; }

    public void ShowContext(HudContext context) =>
        SelfModulate = Colors.White with { A = Alpha(context.ClientSettings, FadesAs) };

    public static float Alpha(ClientSettings? settings, WindowBackground fadesAs) =>
        fadesAs == WindowBackground.None || settings == null
            ? 1f
            : settings.BackgroundOpacity[fadesAs] / (float)WindowBackgrounds.Solid;
}
