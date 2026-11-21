using GdUnit4;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class SkillScrollTests
{
    [TestCase]
    public void LastRow_LeavesTenOfTheFiftyFiveSkillsInView()
    {
        AssertThat(SkillScroll.LastRow).IsEqual(45);
    }

    [TestCase(-5, 0)]
    [TestCase(3, 3)]
    [TestCase(60, 45)]
    public void ScrollTo_StaysWithinTheList(int row, int shownRow)
    {
        var scroll = new SkillScroll();
        scroll.ScrollTo(row);

        AssertThat(scroll.Row).IsEqual(shownRow);
    }

    [TestCase]
    public void ScrollBy_MovesFromTheCurrentRow()
    {
        var scroll = new SkillScroll();
        scroll.ScrollTo(10);
        scroll.ScrollBy(-1);

        AssertThat(scroll.Row).IsEqual(9);
    }

    [TestCase]
    public void PositionShownIn_CountsFromTheTopRow()
    {
        var scroll = new SkillScroll();
        scroll.ScrollTo(4);

        AssertThat(scroll.PositionShownIn(2)).IsEqual(6);
    }
}
