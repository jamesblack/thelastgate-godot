using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;

namespace TheLastGate.Demo;

// Spike: the real options window with its sprite styleboxes swapped for flat ones drawn by the procedural shader.
[Tool]
public partial class ProceduralOptionsDemo : Control
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;
    private const string ShaderPath = "res://Demo/dark_procedural.gdshader";
    private const int BlueFill = 1;
    private const int RedFill = 0;

    private static readonly int[] WindowProfile = [0, 1, 0, 0, 1, 0, 0, 0];
    private static readonly int[] FooterProfile = [0, 1, 0, 0, 0, 0, 0, 0];
    private static readonly int[] ButtonProfile = [0, 2, 0, 0, 0, 0, 0, 0];
    private static readonly int[] NoProfile = [0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly string[] ButtonStyles = ["normal", "hover", "pressed", "hover_pressed", "disabled"];

    private SpriteLoader? ownSprites;

    public override void _Ready()
    {
        var shader = GD.Load<Shader>(ShaderPath);
        foreach (var node in FindChildren("*", "", true, false))
        {
            if (node is not Control control)
            {
                continue;
            }

            switch (control.ThemeTypeVariation.ToString())
            {
                case "DarkWindowFrame":
                    Restyle(control, "panel", ProceduralMaterial(shader, 0, NoProfile, 0, BlueFill));
                    break;
                case "DarkWindowPanel":
                    Restyle(control, "panel", ProceduralMaterial(shader, 0, WindowProfile, 6, RedFill));
                    break;
                case "DarkWindowFooter":
                    Restyle(control, "panel", ProceduralMaterial(shader, 0, FooterProfile, 3, BlueFill));
                    break;
                case "DarkButton":
                    RestyleButton(control, ProceduralMaterial(shader, 0, ButtonProfile, 3, RedFill));
                    break;
                case "DarkCloseButton":
                    RestyleButton(control, ProceduralMaterial(shader, 2, NoProfile, 1, RedFill));
                    break;
            }
        }

        if (Engine.IsEditorHint())
        {
            return;
        }

        ownSprites = new SpriteLoader(DataPaths.Root(), SpriteCacheBytes);
    }

    public override void _ExitTree() => ownSprites?.Dispose();

    private static ShaderMaterial ProceduralMaterial(Shader shader, int mode, int[] profile, int width, int fill)
    {
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("mode", mode);
        material.SetShaderParameter("profile", profile);
        material.SetShaderParameter("profile_width", width);
        material.SetShaderParameter("fill_kind", fill);
        return material;
    }

    private static void Restyle(Control control, string style, ShaderMaterial material)
    {
        control.AddThemeStyleboxOverride(style, Flat(control.GetThemeStylebox(style)));
        control.Material = material;
    }

    // A material paints everything its node draws, the button's text included, so the box is a panel drawn behind
    // the button and the button itself draws only its text.
    private static void RestyleButton(Control control, ShaderMaterial material)
    {
        foreach (var style in ButtonStyles)
        {
            var original = control.GetThemeStylebox(style);
            control.AddThemeStyleboxOverride(
                style,
                new StyleBoxEmpty
                {
                    ContentMarginLeft = original.ContentMarginLeft,
                    ContentMarginTop = original.ContentMarginTop,
                    ContentMarginRight = original.ContentMarginRight,
                    ContentMarginBottom = original.ContentMarginBottom,
                });
        }

        var background = new Panel
        {
            ShowBehindParent = true,
            MouseFilter = MouseFilterEnum.Ignore,
            Material = material,
        };
        background.AddThemeStyleboxOverride("panel", new StyleBoxFlat());
        control.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    private static StyleBoxFlat Flat(StyleBox original) =>
        new()
        {
            ContentMarginLeft = original.ContentMarginLeft,
            ContentMarginTop = original.ContentMarginTop,
            ContentMarginRight = original.ContentMarginRight,
            ContentMarginBottom = original.ContentMarginBottom,
        };
}
