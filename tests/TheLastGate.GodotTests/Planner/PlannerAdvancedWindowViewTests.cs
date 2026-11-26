using GdUnit4;
using Godot;
using TheLastGate.GodotTests.Hud;
using TheLastGate.Hud;
using TheLastGate.Planner;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerAdvancedWindowViewTests
{
    private readonly List<Action<PlannerChoices>> changes = [];
    private PlannerGame game = null!;
    private PlannerAdvancedWindowView view = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        game = PlannerSheetTests.Game();
        view = GD.Load<PackedScene>("res://Planner/planner_advanced_window.tscn")
            .Instantiate<PlannerAdvancedWindowView>();
        Tree.Root.AddChild(view);
        changes.Clear();
        view.ChoiceChanged += changes.Add;
        view.ShowWindow(true, game.Choices, AdvancedSheet.For(PlannerSheet.For(game)));
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => view.Free();

    [TestCase]
    public void Window_HasNoClassicNodes()
    {
        foreach (var type in (string[])[nameof(NinePatchRect), nameof(Button), nameof(CheckBox)])
        {
            AssertThat(view.FindChildren("*", type, true, false).Count).IsEqual(0);
        }

        foreach (var label in view.FindChildren("*", nameof(Label), true, false))
        {
            AssertThat(label is HudLabel).IsTrue();
        }

        foreach (var scroll in view.FindChildren("*", nameof(ScrollContainer), true, false))
        {
            AssertThat(((ScrollContainer)scroll).GetVScrollBar().Material).IsNotNull();
        }
    }

    [TestCase]
    public void Window_KeepsClicksOffTheWorld()
    {
        var frame = view.GetNode<Control>("Window/Frame").GetGlobalRect();

        AssertThat(ClickThrough.ReachesTheWorldAt(frame.Position + new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.End - new Vector2(2, 2))).IsFalse();
    }

    [TestCase]
    public void Window_KeepsItsSizeAndPlace()
    {
        var frame = view.GetNode<Control>("Window/Frame").GetGlobalRect();

        AssertThat(frame).IsEqual(new Rect2(360, 60, 560, 600));
    }

    [TestCase]
    public void TickingGear_ChangesTheChoices()
    {
        var tick = view.GetNode<Container>("%Gear").GetChild(0).GetNode<ProceduralCheckBox>("%Tick");

        tick.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(changes.Count).IsEqual(1);
        var choices = new PlannerChoices();
        changes[0](choices);
        AssertThat(choices.GearTicks).IsEqual(1u);
    }

    [TestCase]
    public void Close_FiresClosePressed()
    {
        var closed = 0;
        view.ClosePressed += () => closed++;

        view.GetNode<ClickArea>("%Close").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(closed).IsEqual(1);
    }
}
