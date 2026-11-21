using GdUnit4;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class StatPanelTests
{
    [TestCase]
    public void LastMetaRow_LeavesTenOfEachListsFortyOneStatsInView()
    {
        AssertThat(StatPanel.LastMetaRow).IsEqual(31);
    }

    [TestCase]
    public void Choose_ListSkillsAgain_AddsTheMetaStatsOnTop()
    {
        var panel = new StatPanel();
        panel.Choose(StatListMode.Skills);

        AssertThat(panel.Mode).IsEqual(StatListMode.SkillsAndMeta);
    }

    [TestCase]
    public void Choose_ListSkillsFromSkillsAndMeta_GoesBackToSkillsAlone()
    {
        var panel = new StatPanel();
        panel.Choose(StatListMode.Skills);
        panel.Choose(StatListMode.Skills);

        AssertThat(panel.Mode).IsEqual(StatListMode.Skills);
    }

    [TestCase]
    public void Choose_ListSkillsFromOffenses_ShowsSkillsAlone()
    {
        var panel = new StatPanel();
        panel.Choose(StatListMode.Offenses);
        panel.Choose(StatListMode.Skills);

        AssertThat(panel.Mode).IsEqual(StatListMode.Skills);
    }

    [TestCase(StatListMode.Skills, StatListMode.Skills, true)]
    [TestCase(StatListMode.SkillsAndMeta, StatListMode.Skills, true)]
    [TestCase(StatListMode.Offenses, StatListMode.Skills, false)]
    [TestCase(StatListMode.Offenses, StatListMode.Offenses, true)]
    [TestCase(StatListMode.Defenses, StatListMode.Offenses, false)]
    [TestCase(StatListMode.Defenses, StatListMode.Defenses, true)]
    public void IsMarked_MarksTheButtonForTheList(StatListMode mode, StatListMode button, bool marked)
    {
        var panel = PanelIn(mode);

        AssertThat(panel.IsMarked(button)).IsEqual(marked);
    }

    [TestCase(StatListMode.Skills, false, false)]
    [TestCase(StatListMode.SkillsAndMeta, false, true)]
    [TestCase(StatListMode.Offenses, true, true)]
    [TestCase(StatListMode.Defenses, true, true)]
    public void Modes_ShowMetaStatsWhereTheSdlClientDoes(StatListMode mode, bool metaBelow, bool metaOnTop)
    {
        var panel = PanelIn(mode);

        AssertThat(panel.ShowsMetaStats).IsEqual(metaBelow);
        AssertThat(panel.ShowsMetaOnTop).IsEqual(metaOnTop);
    }

    [TestCase]
    public void ScrollTo_InOffenses_MovesTheMetaListAndKeepsTheSkillRow()
    {
        var panel = new StatPanel();
        panel.ScrollTo(5);
        panel.Choose(StatListMode.Offenses);
        panel.ScrollTo(50);

        AssertThat(panel.Row).IsEqual(31);
        AssertThat(panel.LastRow).IsEqual(31);
        AssertThat(panel.Skills.Row).IsEqual(5);
    }

    [TestCase]
    public void Row_BackInSkills_IsTheSkillRow()
    {
        var panel = new StatPanel();
        panel.ScrollTo(5);
        panel.Choose(StatListMode.Defenses);
        panel.ScrollBy(3);
        panel.Choose(StatListMode.Skills);

        AssertThat(panel.Row).IsEqual(5);
        AssertThat(panel.LastRow).IsEqual(45);
    }

    [TestCase]
    public void ScrollBy_StaysAtTheTop()
    {
        var panel = PanelIn(StatListMode.Offenses);
        panel.ScrollBy(-1);

        AssertThat(panel.MetaRow).IsEqual(0);
    }

    [TestCase]
    public void TopMeta_InSkills_IsNone()
    {
        AssertThat(new StatPanel().TopMeta(0)).IsNull();
    }

    [TestCase(StatListMode.SkillsAndMeta)]
    [TestCase(StatListMode.Offenses)]
    [TestCase(StatListMode.Defenses)]
    public void TopMeta_ShowsTheFirstSevenStatsUnscrolled(StatListMode mode)
    {
        var panel = PanelIn(mode);
        panel.ScrollBy(4);

        AssertThat(panel.TopMeta(6)).IsEqual(6);
    }

    [TestCase]
    public void BottomMeta_InOffenses_StartsAfterTheTopRows()
    {
        var panel = PanelIn(StatListMode.Offenses);
        panel.ScrollTo(2);

        AssertThat(panel.BottomMeta(1)).IsEqual(10);
    }

    [TestCase]
    public void BottomMeta_InDefenses_StartsAtTheDefenseStats()
    {
        var panel = PanelIn(StatListMode.Defenses);

        AssertThat(panel.BottomMeta(0)).IsEqual(48);
    }

    [TestCase]
    public void BottomMeta_ScrolledToTheEnd_ReachesTheLastStatOfEachList()
    {
        var offenses = PanelIn(StatListMode.Offenses);
        offenses.ScrollTo(StatPanel.LastMetaRow);
        var defenses = PanelIn(StatListMode.Defenses);
        defenses.ScrollTo(StatPanel.LastMetaRow);

        AssertThat(offenses.BottomMeta(9)).IsEqual(47);
        AssertThat(defenses.BottomMeta(9)).IsEqual(88);
    }

    [TestCase(StatListMode.Skills)]
    [TestCase(StatListMode.SkillsAndMeta)]
    public void BottomMeta_WithSkillsBelow_IsNone(StatListMode mode)
    {
        AssertThat(PanelIn(mode).BottomMeta(0)).IsNull();
    }

    [TestCase(StatListMode.Skills, "Makes the left panel display skill scores and experience points (default).")]
    [TestCase(
        StatListMode.Offenses,
        "Makes the left panel display Offense-related statistics, such as attack damage and critical hit rate.")]
    [TestCase(
        StatListMode.Defenses,
        "Makes the left panel display Defense-related statistics, such as eHP and regeneration rates.")]
    public void Help_MatchesTheSdlClient(StatListMode button, string help)
    {
        AssertThat(StatPanel.Help(button)).IsEqual(help);
    }

    private static StatPanel PanelIn(StatListMode mode)
    {
        var panel = new StatPanel();
        if (mode == StatListMode.SkillsAndMeta)
        {
            panel.Choose(StatListMode.Skills);
        }
        else if (mode != StatListMode.Skills)
        {
            panel.Choose(mode);
        }

        return panel;
    }
}
