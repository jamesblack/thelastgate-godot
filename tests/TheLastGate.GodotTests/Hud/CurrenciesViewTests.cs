using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class CurrenciesViewTests
{
    private CurrenciesView view = null!;

    [BeforeTest]
    public async Task ShowTheBox() => view = await HudScene.Show<CurrenciesView>("res://Hud/currencies_view.tscn");

    [AfterTest]
    public void FreeTheBox() => view.Free();

    [TestCase]
    public void Show_WritesMoneyAndHidesTheNamesOfEmptyPointRows()
    {
        view.Show(new PlayerState());

        AssertThat(view.GetNode<Label>("%Money").Text).IsEqual("0G  0S");
        foreach (var row in (string[])["Stronghold", "Casino", "Contract"])
        {
            AssertThat(view.GetNode<Label>($"%{row}Name").Text).IsEqual("???");
            AssertThat(view.GetNode<Label>($"%{row}").Text).IsEqual("0");
        }
    }

    [TestCase]
    public void TheRows_ShareTheFramesHeightEvenly()
    {
        var rows = view.GetNode("Frame/Rows").GetChildren().Cast<Control>().ToList();

        AssertThat(rows.Select(row => row.Size.Y).Distinct().ToList()).ContainsExactly(14f);
    }
}
