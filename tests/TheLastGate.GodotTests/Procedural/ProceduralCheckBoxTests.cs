using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralCheckBoxTests
{
    private static readonly Vector2 Inside = new(2, 2);

    private ProceduralCheckBox box = null!;
    private List<bool> toggles = [];

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowACheckBox()
    {
        box = GD.Load<PackedScene>("res://Procedural/check_box.tscn").Instantiate<ProceduralCheckBox>();
        Tree.Root.AddChild(box);
        box.Size = box.GetCombinedMinimumSize();
        box.Notification((int)Container.NotificationSortChildren);
        toggles = [];
        box.Toggled += toggles.Add;
    }

    [AfterTest]
    public void FreeTheCheckBox() => box.Free();

    [TestCase]
    public void SettingTheSameState_KeepsTheHover()
    {
        box.GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);

        box.Checked = false;
        box.Disabled = false;
        box.Text = box.Text;

        AssertThat(box.Modulate).IsEqual(ClickArea.Hovered);
    }

    [TestCase]
    public void Click_TogglesAndRaisesToggled()
    {
        ClickIt();

        AssertThat(box.Checked).IsTrue();
        AssertThat(toggles).ContainsExactly(true);
        AssertThat(box.GetNode<Control>("Row/Box/Mark").Visible).IsTrue();
    }

    [TestCase]
    public void Click_WhenDisabled_DoesNothing()
    {
        box.Disabled = true;

        ClickIt();

        AssertThat(box.Checked).IsFalse();
        AssertThat(toggles).IsEmpty();
    }

    [TestCase]
    public void SettingChecked_DoesNotRaiseToggled()
    {
        box.Checked = true;

        AssertThat(toggles).IsEmpty();
        AssertThat(box.GetNode<Control>("Row/Box/Mark").Visible).IsTrue();
    }

    [TestCase]
    public void Disabled_DimsTheText()
    {
        box.Disabled = true;

        AssertThat(box.GetNode<HudLabel>("Row/Label").Colour).IsEqual(TextColour.Dim);
    }

    [TestCase]
    public void Checking_KeepsTheBoxItsOwnSize()
    {
        var square = box.GetNode<Control>("Row/Box");
        var uncheckedSize = square.GetCombinedMinimumSize();

        box.Checked = true;

        AssertThat(square.GetCombinedMinimumSize()).IsEqual(uncheckedSize);
        AssertThat(uncheckedSize).IsEqual(new Vector2(11, 11));
    }

    private void ClickIt()
    {
        var click = box.GetNode<ClickArea>("Click");
        click._GuiInput(
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = Inside });
        click._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = Inside });
    }
}
