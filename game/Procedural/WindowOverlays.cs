using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheLastGate.Procedural;

// Menus and prompts must draw over the whole of their window but under any window opened above it, so a window moves
// them into an Overlays node it adds as its own last child, where tree order puts them. They stay where they are in
// the scene, beside the controls they belong to.
public static class WindowOverlays
{
    public const string Group = "window_overlay";

    private const string OverlaysName = "Overlays";

    public static void Lift(Control window)
    {
        var overlays = window.GetNodeOrNull<Control>(OverlaysName) ?? AddOverlays(window);
        var found = new List<Control>();
        Collect(window, overlays, found);

        // Prompts go in first and menus after, so an open menu draws above any prompt.
        foreach (var overlay in found.OrderBy(overlay => overlay is ProceduralMenu ? 1 : 0))
        {
            overlay.Reparent(overlays, false);
        }
    }

    private static Control AddOverlays(Control window)
    {
        var overlays = new Control { Name = OverlaysName, MouseFilter = Control.MouseFilterEnum.Ignore };
        window.AddChild(overlays);
        return overlays;
    }

    // An overlay inside another one moves with it, so the walk doesn't go into an overlay it has found.
    private static void Collect(Node node, Node overlays, List<Control> found)
    {
        foreach (var child in node.GetChildren())
        {
            if (child == overlays)
            {
                continue;
            }

            if (child is Control control && control.IsInGroup(Group))
            {
                found.Add(control);
            }
            else
            {
                Collect(child, overlays, found);
            }
        }
    }
}
