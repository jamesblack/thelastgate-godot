using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class HudCaptionTests
{
    [TestCase]
    public void ACaption_UsesTheCaptionVariationOfItsColour()
    {
        var label = new HudLabel { Colour = TextColour.Grey, Caption = true };

        AssertThat(label.ThemeTypeVariation.ToString()).IsEqual("HudCaptionGrey");

        label.Caption = false;
        AssertThat(label.ThemeTypeVariation.ToString()).IsEqual("HudGrey");
        label.Free();
    }

    [TestCase]
    public async Task TheGearCaptions_AreCaptions()
    {
        var gear = await HudScene.Show<EquipmentView>("res://Hud/equipment_view.tscn");

        var captions = gear.FindChildren("*Caption", "", true, false).OfType<HudLabel>().ToList();
        AssertThat(captions.Count).IsEqual(12);
        AssertThat(captions.All(caption => caption.Caption)).IsTrue();
        gear.Free();
    }

    [TestCase]
    public void TheSmithSlotCaptions_AreCaptionsButItsSignsAreNot()
    {
        var smith = GD.Load<PackedScene>("res://Hud/Smith/smith_window.tscn").Instantiate();

        var labels = smith.FindChildren("*", "Label", true, false).OfType<HudLabel>().ToList();
        AssertThat(labels.Where(label => label.Caption).Select(label => label.Text))
            .ContainsExactlyInAnyOrder("Input", "Stone", "Item", "Out");
        AssertThat(labels.Single(label => label.Text == "+").Caption).IsFalse();
        smith.Free();
    }
}
