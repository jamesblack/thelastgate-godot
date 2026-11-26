using GdUnit4;
using Godot;
using TheLastGate.Map;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class WorldMapCanvasTests
{
    private static readonly Vector2 Middle = new(200, 150);

    private WorldMapCanvas canvas = null!;
    private WorldMapWindow window = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowACanvas()
    {
        window = new WorldMapWindow { Follow = true };
        canvas = new WorldMapCanvas { Window = window, Size = new Vector2(400, 300) };
        Tree.Root.AddChild(canvas);
    }

    [AfterTest]
    public void FreeTheCanvas() => canvas.Free();

    [TestCase(MouseButton.Right)]
    [TestCase(MouseButton.Middle)]
    public void DraggingWith_Pans(MouseButton button)
    {
        canvas._GuiInput(new InputEventMouseButton { ButtonIndex = button, Pressed = true, Position = Middle });
        canvas._GuiInput(new InputEventMouseMotion { Position = Middle, Relative = new Vector2(40, 0) });
        canvas._GuiInput(new InputEventMouseButton { ButtonIndex = button, Position = Middle });

        AssertThat(window.Follow).IsFalse();
    }
}
