using Godot;

namespace TheLastGate.GodotTests.Hud;

// The world view takes clicks in _UnhandledInput, so a window keeps a click off the world only if no click reaches it.
public static partial class ClickThrough
{
    private static readonly Vector2 InsideTheRings = new(2, 2);

    public static bool ReachesTheWorld(Control frame)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var world = new UnhandledClicks();
        tree.Root.AddChild(world);
        var point = frame.GlobalPosition + InsideTheRings;
        foreach (var pressed in new[] { true, false })
        {
            tree.Root.PushInput(
                new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left, Pressed = pressed, Position = point, GlobalPosition = point,
                },
                true);
        }

        var reached = world.Clicks > 0;
        world.Free();
        return reached;
    }

    private sealed partial class UnhandledClicks : Node
    {
        public int Clicks { get; private set; }

        public override void _UnhandledInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton)
            {
                Clicks++;
            }
        }
    }
}
