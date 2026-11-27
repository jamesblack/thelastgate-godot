using GdUnit4;
using Godot;
using TheLastGate.Launcher;
using TheLastGate.Procedural;
using TheLastGate.Updates;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Launcher;

[TestSuite]
[RequireGodotRuntime]
public class RollbackWindowViewTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void Reopening_LetsTheRollBackButtonBePressedAgain()
    {
        var window = AutoFree(
            GD.Load<PackedScene>("res://Launcher/rollback_window.tscn").Instantiate<RollbackWindowView>())!;
        Tree.Root.AddChild(window);
        var button = window.GetNode<ProceduralButton>("%ActionButton");
        button.Disabled = true;
        var current = new Version(1, 4, 0);
        var catalog = new UpdateCatalog(current, [new AvailableRelease(new Version(1, 3, 0), "", 100)]);

        window.Open(current, catalog, (_, _, _) => Task.CompletedTask, _ => { });

        AssertThat(button.Disabled).IsFalse();
    }
}
