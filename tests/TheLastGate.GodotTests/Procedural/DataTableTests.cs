using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class DataTableTests
{
    private static readonly TableColumn[] Columns =
    [
        new("Currency", 1.3f),
        new("Start", Group: "This session"),
        new("Net", Group: "This session"),
        new("Net", Group: "Per hour"),
    ];

    private static readonly TableRow[] Rows =
    [
        new(
        [
            new TableCell("Gold"), new TableCell("52,000"), new TableCell("+6,600", CellTone.Positive),
            new TableCell("+4,400", CellTone.Positive),
        ]),
        new(
        [
            new TableCell("Stronghold"), new TableCell("340"), new TableCell("-35", CellTone.Negative),
            new TableCell("0", CellTone.Zero),
        ]),
    ];

    private DataTable table = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowATable()
    {
        table = new DataTable { Size = new Vector2(400, 0) };
        Tree.Root.AddChild(table);
        table.SetColumns(Columns);
        table.ShowRows(Rows);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheTable() => table.Free();

    [TestCase]
    public void TheGroupRow_CaptionsEachGroupInWhite()
    {
        var captions = table.GetNode("Groups")
            .FindChildren("*", nameof(Label), true, false)
            .Cast<HudLabel>()
            .Where(label => label.Text.Length > 0)
            .ToList();

        AssertThat(captions.Select(label => label.Text)).ContainsExactly("This session", "Per hour");
        AssertThat(captions.All(label => label.Colour == TextColour.White)).IsTrue();
    }

    [TestCase]
    public void TheHeaderRow_IsWhiteWithARuleUnderIt()
    {
        var headers = Headers();

        AssertThat(headers.Select(label => label.Text)).ContainsExactly("Currency", "Start", "Net", "Net");
        AssertThat(headers.All(label => label.Colour == TextColour.White)).IsTrue();
        AssertThat(table.GetNode<CanvasItem>("HeaderRule").Visible).IsTrue();
    }

    [TestCase]
    public void Tones_ColourTheNumbers()
    {
        var stronghold = Cells(1);

        AssertThat(stronghold[0].Colour).IsEqual(TextColour.Yellow);
        AssertThat(stronghold[2].Colour).IsEqual(TextColour.Red);
        AssertThat(stronghold[3].Colour).IsEqual(TextColour.Dim);
        AssertThat(Cells(0)[2].Colour).IsEqual(TextColour.Green);
    }

    [TestCase]
    public void TheNumbers_LineUpUnderTheirHeaders()
    {
        var headers = Headers();
        var gold = Cells(0);

        for (var column = 0; column < headers.Count; column++)
        {
            AssertThat(gold[column].GetGlobalRect().End.X).IsEqual(headers[column].GetGlobalRect().End.X);
        }
    }

    [TestCase]
    public void RowsAfterTheFirst_AreRuledApart()
    {
        var rows = table.GetNode("Body").GetChildren().Cast<SettingsRow>().ToList();

        AssertThat(rows[0].RuleAbove).IsFalse();
        AssertThat(rows[1].RuleAbove).IsTrue();
    }

    [TestCase]
    public void ARefresh_KeepsTheHoveredRowLit()
    {
        var row = table.GetNode("Body").GetChild<SettingsRow>(0);
        row.EmitSignal(Control.SignalName.MouseEntered);

        table.ShowRows(Rows);

        AssertThat(row.Fill).IsEqual(ProceduralFill.Selected);
    }

    private List<HudLabel> Headers() =>
        table.GetNode("Headers/Cells").GetChildren().OfType<HudLabel>().ToList();

    private List<HudLabel> Cells(int row) =>
        table.GetNode("Body").GetChild(row).FindChildren("*", nameof(Label), true, false).Cast<HudLabel>().ToList();
}
