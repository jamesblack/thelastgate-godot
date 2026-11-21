using GdUnit4;
using TheLastGate.Planner;
using TheLastGate.Rules;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class PlannerWindowTests
{
    private PlannerWindow window = new();

    [BeforeTest]
    public void CloseThePlanner() => window = new PlannerWindow();

    [TestCase]
    public void Toggle_OpensAndCloses()
    {
        window.Toggle();
        AssertThat(window.IsOpen).IsTrue();

        window.Toggle();
        AssertThat(window.IsOpen).IsFalse();
    }

    [TestCase]
    public void Toggle_TellsWhetherItOpened()
    {
        AssertThat(window.Toggle()).IsTrue();
        AssertThat(window.Toggle()).IsFalse();
    }

    [TestCase]
    public void Close_ShutsTheAdvancedWindowToo()
    {
        window.Toggle();
        window.AdvancedOpen = true;

        AssertThat(window.Close()).IsTrue();

        AssertThat(window.IsOpen).IsFalse();
        AssertThat(window.AdvancedOpen).IsFalse();
        AssertThat(window.Close()).IsFalse();
    }

    [TestCase]
    public void ToggleFocus_FocusesAndClearsTheSameTarget()
    {
        window.ToggleFocus(PlannerFocus.OnSkill(3));
        AssertThat(window.Focus).IsEqual(PlannerFocus.OnSkill(3));

        window.ToggleFocus(PlannerFocus.OnSkill(3));
        AssertThat(window.Focus).IsNull();
    }

    [TestCase]
    public void ToggleFocus_MovesBetweenSkillsAndCombatStats()
    {
        window.ToggleFocus(PlannerFocus.OnSkill(3));

        window.ToggleFocus(PlannerFocus.OnStat(CombatStat.Armor));

        AssertThat(window.Focus).IsEqual(PlannerFocus.OnStat(CombatStat.Armor));
    }

    [TestCase]
    public void ClearFocus_ForgetsTheFocus()
    {
        window.ToggleFocus(PlannerFocus.OnStat(CombatStat.Hit));

        window.ClearFocus();

        AssertThat(window.Focus).IsNull();
    }

    [TestCase]
    public void TheHeader_SaysWhatIsLeftAndWhatIsPlanned()
    {
        AssertThat(PlannerWindow.Header(1200, 300)).IsEqual("Exp left 1200    Planned 300");
    }
}
