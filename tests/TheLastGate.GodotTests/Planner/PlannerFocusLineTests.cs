using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Planner;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerFocusLineTests
{
    private PlannerFocusLine line = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowALine()
    {
        line = GD.Load<PackedScene>("res://Planner/planner_focus_line.tscn").Instantiate<PlannerFocusLine>();
        Tree.Root.AddChild(line);
    }

    [AfterTest]
    public void FreeTheLine() => line.Free();

    [TestCase(FocusTone.Heading, TextColour.White)]
    [TestCase(FocusTone.Normal, TextColour.Yellow)]
    [TestCase(FocusTone.SkillRaise, TextColour.Green)]
    [TestCase(FocusTone.AttributeRaise, TextColour.Blue)]
    [TestCase(FocusTone.OtherRoute, TextColour.Grey)]
    public void Tones_MapToTextColours(FocusTone tone, TextColour colour)
    {
        line.Show(new FocusLine("Cheapest +1", "4800 exp", false, tone));

        AssertThat(line.GetNode<HudLabel>("%Text").Colour).IsEqual(colour);
        AssertThat(line.GetNode<HudLabel>("%Value").Colour).IsEqual(TextColour.Grey);
    }

    [TestCase]
    public void IndentedLine_SitsFurtherIn()
    {
        line.Show(new FocusLine("Two-Handed +1", null, false, FocusTone.SkillRaise));
        var flush = line.GetNode<Control>("%Indent").CustomMinimumSize.X;

        line.Show(new FocusLine("Two-Handed +1", null, true, FocusTone.SkillRaise));

        AssertThat(line.GetNode<Control>("%Indent").CustomMinimumSize.X).IsEqual(flush + 8);
    }
}
