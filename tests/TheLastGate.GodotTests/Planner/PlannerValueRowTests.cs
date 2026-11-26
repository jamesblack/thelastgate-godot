using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Planner;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerValueRowTests
{
    private PlannerValueRow row = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowARow()
    {
        row = GD.Load<PackedScene>("res://Planner/planner_value_row.tscn").Instantiate<PlannerValueRow>();
        Tree.Root.AddChild(row);
    }

    [AfterTest]
    public void FreeTheRow() => row.Free();

    [TestCase]
    public void PlannedValue_ShowsTheDifferenceInBlue()
    {
        row.ShowPlanned("Hit Score", 297, 323);

        AssertThat(row.GetNode<Label>("%Value").Text).IsEqual("297");
        var planned = row.GetNode<HudLabel>("%Planned");
        AssertThat(planned.Text).IsEqual("323");
        AssertThat(planned.Colour).IsEqual(TextColour.White);
        var difference = row.GetNode<HudLabel>("%Difference");
        AssertThat(difference.Text).IsEqual("+26");
        AssertThat(difference.Colour).IsEqual(TextColour.Blue);
    }

    [TestCase]
    public void UnchangedValue_ShowsNoPlan()
    {
        row.ShowPlanned("Spell Aptitude", 190, 190);

        AssertThat(row.GetNode<Label>("%Planned").Text).IsEmpty();
        AssertThat(row.GetNode<Label>("%Difference").Text).IsEmpty();
    }

    [TestCase]
    public void Outline_RingsTheRowInGold()
    {
        row.ShowOutline(true);

        AssertThat(row.Profile).IsEqual("G");
    }
}
