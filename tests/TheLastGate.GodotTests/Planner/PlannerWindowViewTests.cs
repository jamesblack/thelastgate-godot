using GdUnit4;
using Godot;
using TheLastGate.GodotTests.Hud;
using TheLastGate.Hud;
using TheLastGate.Planner;
using TheLastGate.Procedural;
using TheLastGate.Rules;
using TheLastGate.World;
using static GdUnit4.Assertions;
using OptionButton = Godot.OptionButton;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
[RequireGodotRuntime]
public class PlannerWindowViewTests
{
    private readonly List<CombatWeapon> weapons = [];
    private PlannerGame game = null!;
    private PlannerWindowView view = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private Control Frame => view.GetNode<Control>("Window/Frame");

    [BeforeTest]
    public async Task ShowThePlanner()
    {
        game = PlannerSheetTests.Game();
        view = GD.Load<PackedScene>("res://Planner/planner_window.tscn").Instantiate<PlannerWindowView>();
        Tree.Root.AddChild(view);
        weapons.Clear();
        view.WeaponChosen += weapons.Add;
        view.ShowWindow(true, 6901, 0, PlannerSheet.For(game));
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeThePlanner() => view.Free();

    [TestCase]
    public void Window_HasNoClassicNodes()
    {
        foreach (var type in (string[])[nameof(NinePatchRect), nameof(Button), nameof(OptionButton), nameof(CheckBox)])
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
    public void TheGapBesideTheCloseButton_KeepsClicksOffTheWorld()
    {
        var frame = view.GetNode<Control>("Window/Frame").GetGlobalRect();

        AssertThat(ClickThrough.ReachesTheWorldAt(new Vector2(frame.End.X + 0.5f, frame.Position.Y + 5))).IsFalse();
    }

    [TestCase]
    public void ThePickersMenus_FloatInTheWindowsLastChild()
    {
        var overlays = view.GetNode<Control>("Overlays");

        AssertThat(overlays.GetIndex()).IsEqual(view.GetChildCount() - 1);
        AssertThat(view.GetNode<ProceduralPicker>("%Weapon").Menu.GetParent()).IsEqual(overlays);
    }

    [TestCase]
    public void TheSkillRows_LeaveRoomBeforeTheScrollBar()
    {
        var scroll = view.GetNode<ScrollContainer>("%SkillScroll");
        var bar = scroll.GetVScrollBar();
        var barWidth = bar.Visible ? bar.Size.X : 0;

        AssertThat(view.GetNode<Control>("%SkillRows").Size.X).IsLessEqual(scroll.Size.X - barWidth - 4);
    }

    [TestCase]
    public void Window_KeepsClicksOffTheWorld()
    {
        var frame = Frame.GetGlobalRect();

        AssertThat(ClickThrough.ReachesTheWorldAt(frame.Position + new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.End - new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.GetCenter())).IsFalse();
    }

    [TestCase]
    public void Window_IsCentredAndWidened()
    {
        var frame = Frame.GetGlobalRect();

        AssertThat(frame.Size.X).IsEqual(680f);
        AssertThat(frame.GetCenter().X).IsEqual(640f);
    }

    [TestCase]
    public void ChoosingAWeapon_FiresWeaponChosen()
    {
        var weapon = view.GetNode<ProceduralPicker>("%Weapon");
        weapon.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        weapon.Menu.GetNode<ClickArea>($"Frame/Items/Item{(int)CombatWeapon.Sword}/Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(weapons).ContainsExactly(CombatWeapon.Sword);
    }

    [TestCase]
    public void FistsOfTheHeavens_DisablesThePickerWithItsTooltip()
    {
        game.Choices.ToggleGear((int)Gear.FistsOfTheHeavens);

        view.ShowWindow(true, 6901, 0, PlannerSheet.For(game));

        var weapon = view.GetNode<ProceduralPicker>("%Weapon");
        var click = weapon.GetNode<ClickArea>("Click");
        AssertThat(weapon.Disabled).IsTrue();
        AssertThat(click.TooltipText).StartsWith("Fists of the Heavens");
        var tip = click._MakeCustomTooltip(click.TooltipText);
        AssertThat(tip).IsInstanceOf<ProceduralTooltip>();
        ((Node)tip!).Free();
    }

    [TestCase]
    public void Readout_ShowsPointsLeftAndPlanned()
    {
        view.ShowWindow(true, 6901, 25, PlannerSheet.For(game));

        AssertThat(view.GetNode<Label>("%PointsLeft").Text).IsEqual("Exp left 6901");
        AssertThat(view.GetNode<Label>("%Planned").Text).IsEqual("Planned 25");
    }
}
