using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

public static class FadeLayers
{
    public static void AssertNoStackedFills(Node root)
    {
        foreach (var backdrop in Backdrops(root).Where(Fills))
        {
            var stackedOn = Ancestors(backdrop).OfType<Control>().FirstOrDefault(Fills);
            AssertThat(stackedOn?.Name.ToString())
                .OverrideFailureMessage(
                    $"{backdrop.Name} fills its background over {stackedOn?.Name}, so it fades twice")
                .IsNull();
        }
    }

    public static float EffectiveAlpha(CanvasItem item) =>
        item.SelfModulate.A * Ancestors(item)
            .Prepend(item)
            .OfType<CanvasItem>()
            .Aggregate(1f, (alpha, node) => alpha * node.Modulate.A);

    private static bool Fills(Control control) =>
        control switch
        {
            ProceduralPanel panel => panel.FadesAs != WindowBackground.None && panel.Fill != ProceduralFill.None,
            WindowBackdrop backdrop => backdrop.GetThemeStylebox("panel") switch
            {
                StyleBoxFlat flat => flat.DrawCenter && flat.BgColor.A > 0,
                StyleBoxTexture texture => texture.DrawCenter,
                _ => false,
            },
            _ => false,
        };

    private static IEnumerable<Control> Backdrops(Node root) =>
        root.FindChildren("*", nameof(Container), true, false)
            .OfType<Control>()
            .Where(control => control is WindowBackdrop or ProceduralPanel)
            .Concat(root is WindowBackdrop or ProceduralPanel ? [(Control)root] : []);

    private static IEnumerable<Node> Ancestors(Node node)
    {
        for (var parent = node.GetParent(); parent != null; parent = parent.GetParent())
        {
            yield return parent;
        }
    }
}
