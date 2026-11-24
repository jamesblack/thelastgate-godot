using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralButtonTests
{
    private const int Padding = 2;

    private static readonly Vector2 Inside = new(1, 1);
    private static readonly Vector2 Outside = new(-5, -5);

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void TheTextButton_PadsItsLabelByTwoPixels()
    {
        var button = Show<ProceduralButton>("res://Procedural/text_button.tscn");

        AssertThat(button.Padding).IsEqual(Padding);
        button.Free();
    }

    [TestCase]
    public void TheTextButton_LightsUpWhileTheMouseIsOverIt()
    {
        var button = Show<ProceduralButton>("res://Procedural/text_button.tscn");
        var click = button.GetNode<ClickArea>("Click");

        click.EmitSignal(Control.SignalName.MouseEntered);
        AssertThat(button.Modulate).IsEqual(ClickArea.Hovered);

        click.EmitSignal(Control.SignalName.MouseExited);
        AssertThat(button.Modulate).IsEqual(Colors.White);
        button.Free();
    }

    [TestCase]
    public void TheTextButton_LightsUpMoreWhileHeld()
    {
        var button = Show<ProceduralButton>("res://Procedural/text_button.tscn");
        button.Size = button.GetCombinedMinimumSize();
        button.Notification((int)Container.NotificationSortChildren);
        var click = button.GetNode<ClickArea>("Click");
        click.EmitSignal(Control.SignalName.MouseEntered);

        click._GuiInput(Click(true, Inside));
        AssertThat(button.Modulate).IsEqual(ClickArea.Held);

        click._GuiInput(Click(false, Inside));
        AssertThat(button.Modulate).IsEqual(ClickArea.Hovered);
        button.Free();
    }

    [TestCase]
    public void LettingGoOutsideTheButton_PutsItBack()
    {
        var button = Show<ProceduralButton>("res://Procedural/text_button.tscn");
        button.Size = button.GetCombinedMinimumSize();
        button.Notification((int)Container.NotificationSortChildren);
        var click = button.GetNode<ClickArea>("Click");
        click.EmitSignal(Control.SignalName.MouseEntered);

        click._GuiInput(Click(true, Inside));
        click._GuiInput(Click(false, Outside));

        AssertThat(button.Modulate).IsEqual(Colors.White);
        button.Free();
    }

    // ClickArea is not a tool script, so in the editor the button's click area is a plain Control.
    [TestCase]
    public void AButton_LoadsWhenItsClickAreaIsAPlainControl()
    {
        var button = new ProceduralButton();
        button.AddChild(new Control { Name = "Click" });
        Tree.Root.AddChild(button);

        AssertThat(button.Modulate).IsEqual(Colors.White);
        button.Free();
    }

    [TestCase]
    public async Task TheCloseButton_LightsUpWhileHoveredAndMoreWhileHeld()
    {
        var button = Show<ClickArea>("res://Procedural/close_button.tscn");
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        button.EmitSignal(Control.SignalName.MouseEntered);
        AssertThat(button.Modulate).IsEqual(ClickArea.Hovered);

        button._GuiInput(Click(true, Inside));
        AssertThat(button.Modulate).IsEqual(ClickArea.Held);

        button._GuiInput(Click(false, Inside));
        button.EmitSignal(Control.SignalName.MouseExited);
        AssertThat(button.Modulate).IsEqual(Colors.White);
        button.Free();
    }

    [TestCase]
    public void AClickArea_DoesNotLightUpUnlessAskedTo()
    {
        var area = new ClickArea();
        Tree.Root.AddChild(area);

        area.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(area.Modulate).IsEqual(Colors.White);
        area.Free();
    }

    private static InputEventMouseButton Click(bool pressed, Vector2 position) =>
        new() { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = position };

    private static T Show<T>(string path) where T : Node
    {
        var button = GD.Load<PackedScene>(path).Instantiate<T>();
        Tree.Root.AddChild(button);
        return button;
    }
}
