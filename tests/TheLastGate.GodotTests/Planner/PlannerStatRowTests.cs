using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Planner;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerStatRowTests
{
    private readonly List<(int Slot, bool Raise)> clicks = [];
    private PlannerStatRow row = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowARow()
    {
        row = GD.Load<PackedScene>("res://Planner/planner_stat_row.tscn").Instantiate<PlannerStatRow>();
        Tree.Root.AddChild(row);
        clicks.Clear();
        row.GlyphClicked += (slot, raise) => clicks.Add((slot, raise));
    }

    [AfterTest]
    public void FreeTheRow() => row.Free();

    [TestCase]
    public void BoughtRow_HasTheSelectedBand()
    {
        row.Show(Row(2));
        AssertThat(row.Fill).IsEqual(ProceduralFill.Selected);

        row.Show(Row());
        AssertThat(row.Fill).IsEqual(ProceduralFill.None);
    }

    [TestCase]
    public void GainingRow_ShowsTheStripe()
    {
        row.Show(Row(usesRaisedAttribute: true));
        AssertThat(row.GetNode<CanvasItem>("%Marker").Visible).IsTrue();

        row.Show(Row());
        AssertThat(row.GetNode<CanvasItem>("%Marker").Visible).IsFalse();
    }

    [TestCase]
    public void Outline_RingsTheRowInGold()
    {
        row.ShowOutline(true);
        AssertThat(row.Profile).IsEqual("G");

        row.ShowOutline(false);
        AssertThat(row.Profile).IsEqual("");
    }

    [TestCase]
    public void Outline_KeepsTheContentInPlace()
    {
        var outer = row.GetCombinedMinimumSize();

        row.ShowOutline(true);

        AssertThat(row.GetCombinedMinimumSize()).IsEqual(outer);
    }

    [TestCase]
    public void DisabledGlyph_IgnoresClicks()
    {
        row.Show(Row(canRaise: false));

        Release("%Raise");

        AssertThat(clicks).IsEmpty();
        AssertThat(row.GetNode<GlyphButton>("%Raise").Disabled).IsTrue();
    }

    [TestCase]
    public void Raise_FiresGlyphClicked()
    {
        row.Show(Row(canRaise: true));

        Release("%Raise");

        AssertThat(clicks).ContainsExactly((StatSlots.FirstSkill, true));
    }

    [TestCase]
    public void ChangedTotal_IsWhite()
    {
        row.Show(Row(2));
        AssertThat(row.GetNode<HudLabel>("%Total").Colour).IsEqual(TextColour.White);

        row.Show(Row());
        AssertThat(row.GetNode<HudLabel>("%Total").Colour).IsEqual(TextColour.Yellow);
    }

    [TestCase]
    public void BoughtAndGain_ShowTheirNumbers()
    {
        row.Show(Row(2, 1));

        AssertThat(row.GetNode<Label>("%Bought").Text).IsEqual("+2");
        AssertThat(row.GetNode<Label>("%Gain").Text).IsEqual("+1");
    }

    private static PlannerRow Row(
        int directRaise = 0,
        int attributeGain = 0,
        bool canRaise = true,
        bool usesRaisedAttribute = false) =>
        new(
            "Two-Handed",
            StatSlots.FirstSkill,
            4,
            109,
            directRaise,
            attributeGain,
            0,
            9,
            canRaise,
            true,
            usesRaisedAttribute);

    private void Release(string glyph) =>
        row.GetNode<ClickArea>($"{glyph}/Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
}
