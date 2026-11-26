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
public class GlyphButtonTests
{
    private GlyphButton button = null!;
    private int presses;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAButton()
    {
        button = GD.Load<PackedScene>("res://Procedural/glyph_button.tscn").Instantiate<GlyphButton>();
        button.Glyph = "+";
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
    public void DisabledGlyph_IgnoresClicks()
    {
        button.Disabled = true;

        Release(MapButton.Left);

        AssertThat(presses).IsEqual(0);
    }

    [TestCase]
    public void Glyph_UsesTheCaptionFont()
    {
        var label = button.GetNode<HudLabel>("%Glyph");

        AssertThat(label.Caption).IsTrue();
        AssertThat(label.Text).IsEqual("+");
    }

    [TestCase]
    public void Box_HasOneGoldRing() => AssertThat(button.Profile).IsEqual("G");

    [TestCase]
    public void Disabled_DimsTheGlyph()
    {
        button.Disabled = true;

        AssertThat(button.GetNode<HudLabel>("%Glyph").Colour).IsEqual(TextColour.Dim);
        AssertThat(button.Modulate.A).IsEqual(0.6f);
    }

    [TestCase]
    public void SettingTheSameState_KeepsTheHover()
    {
        button.GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);

        button.Disabled = false;

        AssertThat(button.Modulate).IsEqual(ClickArea.Hovered);
    }

    private void Release(MapButton which) =>
        button.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(which));
}
