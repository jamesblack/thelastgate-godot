using System;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Hud.SkillTree;

public sealed class SkillTreeWindow
{
    private const int GlowPeriodMilliseconds = 16;
    private const int GlowBrightestAt = 8;

    public State.SkillTree? Open { get; private set; }

    public static bool ButtonShows(int classPoints) => SkillTrees.Total(classPoints) > 0;

    // The current client's glow changes nearly every frame, so it reads as a flicker rather than a pulse.
    public static int GlowLight(long milliseconds)
    {
        var fromBrightest = Math.Abs(GlowBrightestAt - (int)(milliseconds % GlowPeriodMilliseconds));
        return Math.Min(SpriteLighting.DarkestLight, fromBrightest * 2);
    }

    public static string Counter(int treeValue) => $"{SkillTrees.Unspent(treeValue)}/{SkillTrees.Total(treeValue)}";

    // The server writes its descriptions as format strings, so a percent sign arrives doubled.
    public static string ShownText(string text) => text.Replace("%%", "%");

    public bool ButtonGlows(int classPoints, int contractPoints) =>
        (SkillTrees.Unspent(classPoints) > 0 && Open != State.SkillTree.Class)
        || (SkillTrees.Unspent(contractPoints) > 0 && Open != State.SkillTree.Contract);

    // Any modifier asks for the contract tree. Asking for the open tree, or one without points, closes the window.
    public void Toggle(MapKeys keys, int classPoints, int contractPoints)
    {
        var (tree, points) = keys == MapKeys.None
            ? (State.SkillTree.Class, classPoints)
            : (State.SkillTree.Contract, contractPoints);
        Open = SkillTrees.Total(points) > 0 && Open != tree ? tree : null;
    }

    public bool Close()
    {
        var wasOpen = Open != null;
        Open = null;
        return wasOpen;
    }

    public byte[]? Click(int node, MapButton button, MapKeys keys)
    {
        if (Open is not { } tree)
        {
            return null;
        }

        return button switch
        {
            MapButton.Left when keys == MapKeys.None => SkillTreePackets.Learn(tree, node),
            MapButton.Left => SkillTreePackets.Unlearn(tree, node),
            MapButton.Right => SkillTreePackets.Describe(tree, node),
            _ => null,
        };
    }
}
