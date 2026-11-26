using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class KeyButtonTests
{
    private KeyButton button = null!;
    private int presses;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAButton()
    {
        button = GD.Load<PackedScene>("res://Procedural/key_button.tscn").Instantiate<KeyButton>();
        button.Caption = "F8";
        Tree.Root.AddChild(button);
        presses = 0;
        button.Pressed += () => presses++;
    }

    [AfterTest]
    public void FreeTheButton() => button.Free();

    [TestCase]
    public void Release_FiresPressed()
    {
        Release(MapButton.Left);
        Release(MapButton.Right);

        AssertThat(presses).IsEqual(1);
    }

    [TestCase]
    public void Caption_ShowsTheKey()
    {
        var label = button.GetNode<HudLabel>("Label");

        AssertThat(label.Text).IsEqual("F8");
        AssertThat(label.Colour).IsEqual(TextColour.Yellow);
        AssertThat(button.Fill).IsEqual(ProceduralFill.Blue);
    }

    [TestCase]
    public void Waiting_LightsTheBox()
    {
        button.Waiting = true;

        AssertThat(button.Fill).IsEqual(ProceduralFill.Selected);
        AssertThat(button.GetNode<HudLabel>("Label").Colour).IsEqual(TextColour.White);
    }

    [TestCase]
    public void Tooltip_IsTheStyledOne()
    {
        button.Tooltip = "ESC to cancel.";
        var click = button.GetNode<ClickArea>("Click");

        AssertThat(click.TooltipText).IsEqual("ESC to cancel.");
        var tip = click._MakeCustomTooltip(click.TooltipText);
        AssertThat(tip).IsInstanceOf<ProceduralTooltip>();
        ((Node)tip!).Free();
    }

    [TestCase]
    public void SettingTheSameState_KeepsTheHover()
    {
        button.GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);

        button.Waiting = false;
        button.Caption = "F8";

        AssertThat(button.Modulate).IsEqual(ClickArea.Hovered);
    }

    private void Release(MapButton which) =>
        button.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(which));
}
