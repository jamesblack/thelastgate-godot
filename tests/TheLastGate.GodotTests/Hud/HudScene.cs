using Godot;

namespace TheLastGate.GodotTests.Hud;

public static class HudScene
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    public static async Task<T> Show<T>(string scenePath)
        where T : Node
    {
        var scene = GD.Load<PackedScene>(scenePath).Instantiate<T>();
        Tree.Root.AddChild(scene);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        return scene;
    }
}
