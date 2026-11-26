using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralButtonToggleTests
{
    private ProceduralButton button = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAButton()
    {
        button = GD.Load<PackedScene>("res://Procedural/text_button.tscn").Instantiate<ProceduralButton>();
        Tree.Root.AddChild(button);
    }

    [AfterTest]
    public void FreeTheButton() => button.Free();

    [TestCase]
    public void On_UsesSelectedFillAndYellowText()
    {
        button.On = true;

        AssertThat(button.Fill).IsEqual(ProceduralFill.Selected);
        AssertThat(button.GetNode<HudLabel>("Label").Colour).IsEqual(TextColour.Yellow);
    }

    [TestCase]
    public void Off_UsesRedFillAndGreyText()
    {
        button.On = true;

        button.On = false;

        AssertThat(button.Fill).IsEqual(ProceduralFill.Red);
        AssertThat(button.GetNode<HudLabel>("Label").Colour).IsEqual(TextColour.Grey);
    }
}
