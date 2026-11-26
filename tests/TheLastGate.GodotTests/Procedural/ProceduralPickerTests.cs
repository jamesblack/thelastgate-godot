using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.GodotTests.Hud;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralPickerTests
{
    private readonly List<int> chosen = [];
    private ProceduralPicker picker = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private ProceduralMenu Menu => picker.GetNode<ProceduralMenu>("Menu");

    [BeforeTest]
    public async Task ShowAPicker()
    {
        picker = GD.Load<PackedScene>("res://Procedural/picker.tscn").Instantiate<ProceduralPicker>();
        picker.Position = new Vector2(100, 100);
        picker.CustomMinimumSize = new Vector2(110, 0);
        Tree.Root.AddChild(picker);
        picker.SetChoices(["Unarmed", "Dagger", "Sword", "Two-Handed"]);
        chosen.Clear();
        picker.ItemChosen += chosen.Add;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeThePicker() => picker.Free();

    [TestCase]
    public void Release_OpensTheMenuUnderTheBox()
    {
        Open();

        AssertThat(Menu.IsOpen).IsTrue();
        AssertThat(Menu.GlobalPosition.Y).IsEqual(picker.GlobalPosition.Y + picker.Size.Y);
        AssertThat(Menu.GlobalPosition.X).IsEqual(picker.GlobalPosition.X);
    }

    [TestCase]
    public void Choosing_FiresItemChosenAndShowsIt()
    {
        Open();

        Menu.GetNode<ClickArea>("Frame/Items/Item2/Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(chosen).ContainsExactly(2);
        AssertThat(picker.Selected).IsEqual(2);
        AssertThat(picker.GetNode<Label>("%Value").Text).IsEqual("Sword");
        AssertThat(Menu.IsOpen).IsFalse();
    }

    [TestCase]
    public void Select_DoesNotFireItemChosen()
    {
        picker.Select(3);

        AssertThat(chosen).IsEmpty();
        AssertThat(picker.GetNode<Label>("%Value").Text).IsEqual("Two-Handed");
    }

    [TestCase]
    public void Disabled_DoesNotOpen()
    {
        picker.Disabled = true;

        Open();

        AssertThat(Menu.IsOpen).IsFalse();
        AssertThat(picker.GetNode<HudLabel>("%Value").Colour).IsEqual(TextColour.Dim);
    }

    [TestCase]
    public void Picker_ClosingClick_StaysOffTheWorld()
    {
        Open();

        AssertThat(ClickThrough.ReachesTheWorldAt(new Vector2(600, 400))).IsFalse();
        AssertThat(Menu.IsOpen).IsFalse();
    }

    [TestCase]
    public void Tooltip_IsTheStyledOne()
    {
        picker.Tooltip = "Fists use Hand to Hand";
        var click = picker.GetNode<ClickArea>("Click");

        AssertThat(click.TooltipText).IsEqual("Fists use Hand to Hand");
        var tip = click._MakeCustomTooltip(click.TooltipText);
        AssertThat(tip).IsInstanceOf<ProceduralTooltip>();
        ((Node)tip!).Free();
    }

    [TestCase]
    public void SettingTheSameState_KeepsTheHover()
    {
        picker.GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);

        picker.Disabled = false;

        AssertThat(picker.SelfModulate).IsEqual(ClickArea.Hovered);
    }

    private void Open() =>
        picker.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
}
