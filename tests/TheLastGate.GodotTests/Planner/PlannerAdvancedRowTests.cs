using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Planner;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerAdvancedRowTests
{
    private readonly List<int> counts = [];
    private PlannerAdvancedRow row = null!;
    private int toggles;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private LineEdit Count => row.GetNode<LineEdit>("%Count");

    private ProceduralCheckBox Tick => row.GetNode<ProceduralCheckBox>("%Tick");

    [BeforeTest]
    public void ShowARow()
    {
        row = GD.Load<PackedScene>("res://Planner/planner_advanced_row.tscn").Instantiate<PlannerAdvancedRow>();
        Tree.Root.AddChild(row);
        counts.Clear();
        toggles = 0;
        row.CountChosen += counts.Add;
        row.Toggled += () => toggles++;
    }

    [AfterTest]
    public void FreeTheRow() => row.Free();

    [TestCase]
    public void Count_WhileTyping_KeepsTheTypedText()
    {
        row.ShowCount(1, true, "");
        Count.Edit();
        Count.Text = "12";

        row.ShowCount(5, true, "");

        AssertThat(Count.Text).IsEqual("12");
    }

    [TestCase]
    public void Count_KeepsOnlyDigits()
    {
        row.ShowCount(1, true, "");
        Count.Text = "1a2";

        Count.EmitSignal(LineEdit.SignalName.TextChanged, "1a2");

        AssertThat(Count.Text).IsEqual("12");
        AssertThat(counts).ContainsExactly(12);
    }

    [TestCase]
    public void Count_ShowsTheStyledTooltip()
    {
        row.ShowCount(1, true, "Copies on your gear.");

        AssertThat(Count.TooltipText).IsEqual("Copies on your gear.");
        var tip = Count._MakeCustomTooltip(Count.TooltipText);
        AssertThat(tip).IsInstanceOf<ProceduralTooltip>();
        ((Node)tip!).Free();
    }

    [TestCase]
    public void AlwaysOn_IsTickedAndDisabled()
    {
        row.ShowAlwaysOn("Rigor");

        AssertThat(Tick.Checked).IsTrue();
        AssertThat(Tick.Disabled).IsTrue();
        AssertThat(Tick.Text).IsEqual("Rigor");
    }

    [TestCase]
    public void NotReadySpell_IsDisabledWithADimEffect()
    {
        row.Show("Bless", false, false);
        row.ShowEffect("+3% Willpower");

        AssertThat(Tick.Disabled).IsTrue();
        AssertThat(row.GetNode<HudLabel>("%Effect").Colour).IsEqual(TextColour.Dim);
    }

    [TestCase]
    public void Note_HidesTheTick()
    {
        row.ShowNote("Needs Bless to be cast");

        AssertThat(Tick.Visible).IsFalse();
        AssertThat(row.GetNode<Label>("%Note").Text).IsEqual("Needs Bless to be cast");
    }

    [TestCase]
    public void Toggling_FiresToggled()
    {
        row.Show("Bless", false, true);

        Tick.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(toggles).IsEqual(1);
    }

    [TestCase]
    public void AnEmptyEffect_TakesNoRoom()
    {
        row.Show("The Fool", false, true);

        AssertThat(row.GetNode<CanvasItem>("%Effect").Visible).IsFalse();
    }
}
