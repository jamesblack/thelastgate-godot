using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ColourPickerTests
{
    private List<int> chosen = [];
    private ColourPicker picker = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAPicker()
    {
        picker = GD.Load<PackedScene>("res://Procedural/colour_picker.tscn").Instantiate<ColourPicker>();
        Tree.Root.AddChild(picker);
        chosen = [];
        picker.ColourChosen += chosen.Add;
    }

    [AfterTest]
    public void FreeThePicker() => picker.Free();

    [TestCase]
    public void Click_ChoosesTheSwatch()
    {
        picker.GetNode<ClickArea>("Swatch3").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(picker.Chosen).IsEqual(3);
        AssertThat(chosen).ContainsExactly(3);
        AssertThat(Ring(3)).IsEqual("W");
        AssertThat(Ring(0)).IsEqual("G");
    }

    [TestCase]
    public void SettingChosen_MovesTheRingSilently()
    {
        picker.Chosen = 5;

        AssertThat(Ring(5)).IsEqual("W");
        AssertThat(Ring(0)).IsEqual("G");
        AssertThat(chosen).IsEmpty();
    }

    [TestCase]
    public void ThePicker_HasASwatchForEveryPaletteColour()
    {
        AssertThat(picker.GetChildCount()).IsEqual(NotePalette.Size);
    }

    private string Ring(int swatch) => picker.GetNode<ProceduralPanel>($"Swatch{swatch}/Look").Profile;
}
