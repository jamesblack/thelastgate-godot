using GdUnit4;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class OptionsWindowTests
{
    private OptionsWindow window = new();

    [BeforeTest]
    public void CloseTheWindow() => window = new OptionsWindow();

    [TestCase]
    public void Toggle_OpensOnTheGeneralTabAndClosesAgain()
    {
        window.Toggle();
        AssertThat(window.IsOpen).IsTrue();
        AssertThat(window.Tab).IsEqual(OptionsTab.General);

        window.Toggle();
        AssertThat(window.IsOpen).IsFalse();
    }

    [TestCase(OptionsTab.Audio)]
    [TestCase(OptionsTab.Display)]
    [TestCase(OptionsTab.Keybinds)]
    [TestCase(OptionsTab.Experimental)]
    public void Show_SwitchesTab(OptionsTab tab)
    {
        window.Toggle();

        window.Show(tab);

        AssertThat(window.Tab).IsEqual(tab);
    }

    [TestCase]
    public void Tabs_KeepTheCurrentClientsThreeThenTheGodotDisplayAndExperimentalTabs()
    {
        AssertThat(Enum.GetValues<OptionsTab>())
            .ContainsExactly(
                OptionsTab.General,
                OptionsTab.Audio,
                OptionsTab.Keybinds,
                OptionsTab.Display,
                OptionsTab.Experimental);
    }

    [TestCase]
    public void ReopeningTheWindow_KeepsTheLastTab()
    {
        window.Toggle();
        window.Show(OptionsTab.Audio);
        window.Close();

        window.Toggle();

        AssertThat(window.Tab).IsEqual(OptionsTab.Audio);
    }

    [TestCase]
    public void Close_SaysWhetherTheWindowWasOpen()
    {
        window.Toggle();

        AssertThat(window.Close()).IsTrue();
        AssertThat(window.IsOpen).IsFalse();
        AssertThat(window.Close()).IsFalse();
    }
}
