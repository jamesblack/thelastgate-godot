using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class SkillListTests
{
    private SkillList list = null!;

    [BeforeTest]
    public async Task ShowTheList() => list = await HudScene.Show<SkillList>("res://Hud/skills_view.tscn");

    [AfterTest]
    public void FreeTheList() => list.Free();

    [TestCase]
    public void TheList_ShowsTenRows()
    {
        AssertThat(list.Rows.Count()).IsEqual(SkillScroll.VisibleRows);
    }

    [TestCase]
    public void ShowUpdate_HidesTheFooterAndWritesThePointsLeft()
    {
        list.ShowUpdate(true, 6901);
        AssertThat(list.GetNode<CanvasItem>("%UpdateFooter").Visible).IsTrue();
        AssertThat(list.GetNode<Label>("%UpdatePoints").Text).IsEqual("6901");

        list.ShowUpdate(false, 0);
        AssertThat(list.GetNode<CanvasItem>("%UpdateFooter").Visible).IsFalse();
    }

    [TestCase]
    public void TheFrame_JoinsOnlyTheFootersShown()
    {
        var frame = list.GetNode<ProceduralPanel>("%Frame");
        var update = new Rect2(131, 142, 101, 16);
        var planner = new Rect2(0, 142, 60, 16);

        list.ShowUpdate(true, 0);
        list.ShowPlannerButton(false);
        AssertThat(frame.JoinedWith.ToArray()).ContainsExactly(update);

        list.ShowPlannerButton(true);
        AssertThat(frame.JoinedWith.ToArray()).ContainsExactly(planner, update);

        list.ShowUpdate(false, 0);
        list.ShowPlannerButton(false);
        AssertThat(frame.JoinedWith.Count).IsEqual(0);
    }

    [TestCase]
    public void ShowPlannerButton_ShowsTheLeftFooter()
    {
        list.ShowPlannerButton(true);
        AssertThat(list.GetNode<CanvasItem>("%PlannerFooter").Visible).IsTrue();

        list.ShowPlannerButton(false);
        AssertThat(list.GetNode<CanvasItem>("%PlannerFooter").Visible).IsFalse();
    }

    [TestCase]
    public void TheUpdateAndPlannerButtons_LightUp()
    {
        AssertThat(list.UpdateButton.LightsUp).IsTrue();
        AssertThat(list.PlannerButton.LightsUp).IsTrue();
    }

    [TestCase]
    public void TheWheelOverTheRows_ScrollsTheList()
    {
        list.GetNode<Control>("%Frame")
            .EmitSignal(
                Control.SignalName.GuiInput,
                new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });

        AssertThat(list.Panel.Row).IsEqual(1);
        AssertThat(list.GetNode<ScrollBar>("%ScrollBar").Value).IsEqual(1);
    }

    [TestCase]
    public void TheScrollBar_KeepsAGrabberWithoutAPage()
    {
        var bar = list.GetNode<ProceduralScrollBar>("%ScrollBar");

        AssertThat(bar.Page).IsEqual(0);
        AssertThat(bar.MinimumGrabberLength).IsEqual(11);
    }
}
