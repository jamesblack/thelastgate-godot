using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class StatRowTests
{
    private static readonly StatLine Raisable = new("Axe", TextColour.Yellow, 4, 109, true, 9);
    private static readonly MetaLine Offense = new("Attack Speed", TextColour.Green, "2.63", "%");

    private StatRow row = null!;

    [BeforeTest]
    public async Task ShowTheRow() => row = await HudScene.Show<StatRow>("res://Hud/stat_row.tscn");

    [AfterTest]
    public void FreeTheRow() => row.Free();

    [TestCase]
    public void AStatLine_ShowsTheStatColumns()
    {
        row.Show(Raisable, 0, true);

        AssertThat(Shown("%CurrentCell", "%RaiseArea", "%LowerArea", "%CostCell")).IsTrue();
        AssertThat(Hidden("%ValueCell", "%AffixCell")).IsTrue();
        AssertThat(row.GetNode<Label>("%Base").Text).IsEqual("4");
        AssertThat(row.GetNode<Label>("%Cost").Text).IsEqual("9");
    }

    [TestCase]
    public void AMetaLine_ShowsTheMetaColumns()
    {
        row.ShowMeta(Offense, 3);

        AssertThat(Shown("%ValueCell", "%AffixCell")).IsTrue();
        AssertThat(Hidden("%CurrentCell", "%RaiseArea", "%LowerArea", "%CostCell", "%Base")).IsTrue();
        AssertThat(row.GetNode<Label>("%MetaValue").Text).IsEqual("2.63");
    }

    [TestCase]
    public async Task TheColumns_KeepTheArtsWidthsInBothLayouts()
    {
        row.Show(Raisable, 0, true);
        await Frame();
        AssertThat(Widths("%NameCell", "%CurrentCell", "%RaiseArea", "%LowerArea", "%CostCell"))
            .ContainsExactly(131f, 23f, 13f, 13f, 48f);

        row.ShowMeta(Offense, 3);
        await Frame();
        AssertThat(Widths("%NameCell", "%ValueCell", "%AffixCell")).ContainsExactly(131f, 49f, 48f);
    }

    private static async Task Frame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    [TestCase]
    public void OnlyAShownSign_LightsUp()
    {
        row.Show(Raisable, 0, true);

        AssertThat(row.RaiseArea.LightsUp).IsTrue();
        AssertThat(row.LowerArea.LightsUp).IsFalse();
    }

    [TestCase]
    public void HoveringASign_LightsTheSignButNotItsCell()
    {
        row.Show(Raisable, 0, true);

        row.RaiseArea.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(row.RaiseArea.GetNode<CanvasItem>("Margin").Modulate).IsEqual(ClickArea.Hovered);
        AssertThat(row.RaiseArea.Modulate).IsEqual(Colors.White);
        AssertThat(row.GetNode<CanvasItem>("%RaiseCell").Modulate).IsEqual(Colors.White);
    }

    private bool Shown(params string[] names) => names.All(name => row.GetNode<CanvasItem>(name).Visible);

    private bool Hidden(params string[] names) => names.All(name => !row.GetNode<CanvasItem>(name).Visible);

    private List<float> Widths(params string[] names) =>
        names.Select(name => row.GetNode<Control>(name).Size.X).ToList();
}
