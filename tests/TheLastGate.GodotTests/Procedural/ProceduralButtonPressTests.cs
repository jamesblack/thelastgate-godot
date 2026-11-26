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
public class ProceduralButtonPressTests
{
    private ProceduralButton button = null!;
    private int presses;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAButton()
    {
        button = GD.Load<PackedScene>("res://Procedural/text_button.tscn").Instantiate<ProceduralButton>();
        Tree.Root.AddChild(button);
        presses = 0;
        button.Pressed += () => presses++;
    }

    [AfterTest]
    public void FreeTheButton() => button.Free();

    [TestCase]
    public void ALeftClick_PressesIt()
    {
        Release(MapButton.Left);
        Release(MapButton.Right);

        AssertThat(presses).IsEqual(1);
    }

    [TestCase]
    public void WhenDisabled_ItFadesAndIgnoresClicks()
    {
        button.Disabled = true;

        Release(MapButton.Left);

        AssertThat(presses).IsEqual(0);
        AssertThat(button.Modulate.A).IsLess(1f);
        AssertThat(button.GetNode<HudLabel>("Label").Colour).IsEqual(TextColour.Dim);
    }

    [TestCase]
    public void Caption_SetsTheLabel()
    {
        button.Caption = "Create";

        AssertThat(button.GetNode<Label>("Label").Text).IsEqual("Create");
    }

    private void Release(MapButton which) =>
        button.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(which));
}
