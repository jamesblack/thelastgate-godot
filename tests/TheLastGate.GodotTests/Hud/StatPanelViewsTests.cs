using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class StatPanelViewsTests
{
    [TestCase]
    public async Task TheAttributes_ShowSevenRows()
    {
        var view = await HudScene.Show<AttributesView>("res://Hud/attributes_view.tscn");

        AssertThat(view.Rows.Count()).IsEqual(StatPanel.TopRows);
        AssertThat(view.Row(6)).IsSame(view.Rows.Last());
        view.Free();
    }

    [TestCase]
    public async Task ShowPool_WritesTheCurrentAndMaximum()
    {
        var view = await HudScene.Show<PoolsView>("res://Hud/pools_view.tscn");

        view.ShowPool("Endurance", 12, 255);

        AssertThat(view.GetNode<Label>("%Endurance").Text).IsEqual("12");
        AssertThat(view.GetNode<Label>("%MaxEndurance").Text).IsEqual("255");
        view.Free();
    }

    [TestCase]
    public async Task TheChosenList_IsFilledSelected()
    {
        var view = await HudScene.Show<StatModeButtonsView>("res://Hud/stat_mode_buttons_view.tscn");
        var panel = new StatPanel();
        panel.Choose(StatListMode.Offenses);

        view.ShowMarked(panel);

        AssertThat(view.Buttons.Select(Fill).ToList())
            .ContainsExactly(ProceduralFill.None, ProceduralFill.Selected, ProceduralFill.None);
        view.Free();
    }

    [TestCase]
    public async Task SkillsChosenTwice_AddsTheOneOffRingToSkills()
    {
        var view = await HudScene.Show<StatModeButtonsView>("res://Hud/stat_mode_buttons_view.tscn");
        var panel = new StatPanel();
        panel.Choose(StatListMode.Skills);

        view.ShowMarked(panel);

        var skills = view.Buttons.First();
        AssertThat(Fill(skills)).IsEqual(ProceduralFill.Selected);
        AssertThat(skills.GetNode<CanvasItem>("BothMarker").Visible).IsTrue();
        view.Free();
    }

    private static ProceduralFill Fill(StatModeButton button) => button.GetNode<ProceduralPanel>("Cell").Fill;
}
