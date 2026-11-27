using GdUnit4;
using Godot;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class SettingsRowTests
{
    private SettingsRow row = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowARow()
    {
        row = GD.Load<PackedScene>("res://Hud/option_row.tscn").Instantiate<SettingsRow>();
        Tree.Root.AddChild(row);
        row.Size = new Vector2(300, 30);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheRow() => row.Free();

    [TestCase]
    public void DeselectingTheRowUnderThePointer_KeepsItLit()
    {
        row.EmitSignal(Control.SignalName.MouseEntered);
        row.Selected = true;

        row.Selected = false;

        AssertThat(row.Fill).IsEqual(ProceduralFill.Selected);
    }

    [TestCase]
    public void DeselectingARowThePointerLeft_ClearsIt()
    {
        row.Selected = true;

        row.Selected = false;

        AssertThat(row.Fill).IsEqual(ProceduralFill.None);
    }

    [TestCase]
    public void RuleAbove_DrawsAFadedLineInTheGapAbove()
    {
        row.RuleAbove = true;

        var rule = row.GetNode<ProceduralLine>("Rule");
        AssertThat(rule.Visible).IsTrue();
        AssertThat(rule.Points[0].Y).IsLess(0f);
        AssertThat(rule.Points[^1].X).IsEqual(300f);
        AssertThat(rule.Modulate.A).IsLess(1f);

        row.RuleAbove = false;
        AssertThat(rule.Visible).IsFalse();
    }

    [TestCase]
    public void LastChild_EndsTheTreeLineInItsRow()
    {
        row.Tree = SettingsTree.Last;

        var line = row.GetNode<ProceduralLine>("TreeLine");
        AssertThat(line.Visible).IsTrue();
        AssertThat(line.Points.Max(point => point.Y)).IsLess(30f);
    }

    [TestCase]
    public void BranchingChild_CarriesTheLineIntoTheNextRow()
    {
        row.Tree = SettingsTree.Branch;

        var line = row.GetNode<ProceduralLine>("TreeLine");
        AssertThat(line.Points.Max(point => point.Y)).IsGreater(30f);
    }

    [TestCase]
    public void NoTree_HidesTheLine()
    {
        row.Tree = SettingsTree.Last;
        row.Tree = SettingsTree.None;

        AssertThat(row.GetNode<CanvasItem>("TreeLine").Visible).IsFalse();
    }
}
