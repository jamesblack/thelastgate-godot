using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class OptionRowsTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void OptionRow_ClickTogglesTheBox()
    {
        var row = Show("res://Hud/option_row.tscn");
        var toggles = new List<bool>();
        row.GetNode<ProceduralCheckBox>("Line/Box").Toggled += toggles.Add;

        row._GuiInput(LeftPress());

        AssertThat(toggles).ContainsExactly(true);
        row.Free();
    }

    [TestCase]
    public void OptionRow_HoverFillsTheRow()
    {
        var row = Show("res://Hud/option_row.tscn");

        row.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(row.Fill).IsEqual(ProceduralFill.Selected);
        row.Free();
    }

    [TestCase]
    public void SliderRow_ClickOutsideTheSlider_DoesNothing()
    {
        var row = Show("res://Hud/slider_row.tscn");
        var slider = row.GetNode<ProceduralSlider>("Line/Slider");
        var before = slider.Value;

        row._GuiInput(LeftPress());

        AssertThat(slider.Value).IsEqual(before);
        row.Free();
    }

    [TestCase]
    public void KeybindRow_HoverFillsTheRow()
    {
        var row = Show("res://Hud/keybind_row.tscn");

        row.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(row.Fill).IsEqual(ProceduralFill.Selected);
        AssertThat(row.GetNode<KeyButton>("Line/Key")).IsNotNull();
        row.Free();
    }

    [TestCase]
    public void WarningMark_ShowsTheStyledTooltip()
    {
        var row = Show("res://Hud/option_row.tscn");
        var mark = row.GetNode<WarningMark>("Line/Warning");

        AssertThat(mark.Colour).IsEqual(TextColour.Red);
        var tip = mark._MakeCustomTooltip("Keys are typed into chat.");
        AssertThat(tip).IsInstanceOf<ProceduralTooltip>();
        ((Node)tip!).Free();
        row.Free();
    }

    private static SettingsRow Show(string path)
    {
        var row = GD.Load<PackedScene>(path).Instantiate<SettingsRow>();
        Tree.Root.AddChild(row);
        return row;
    }

    private static InputEventMouseButton LeftPress() =>
        new() { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(2, 2) };
}
