using GdUnit4;
using TheLastGate.Hud;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class MoneyButtonsViewTests
{
    private MoneyButtonsView view = null!;

    [BeforeTest]
    public async Task ShowTheButtons() =>
        view = await HudScene.Show<MoneyButtonsView>("res://Hud/money_buttons_view.tscn");

    [AfterTest]
    public void FreeTheButtons() => view.Free();

    [TestCase]
    public void TheGoldButtons_TakeTenThousandThousandAndHundredGold()
    {
        AssertThat(view.Buttons.Select(button => button.Silver).ToList()).ContainsExactly(1000000, 100000, 10000);
    }

    [TestCase]
    public void EveryButton_LightsUpLikeTheStyleGuidesButtons()
    {
        AssertThat(view.Buttons.All(button => button.LightsUp)).IsTrue();
        AssertThat(view.TrashCan.LightsUp).IsTrue();
    }
}
