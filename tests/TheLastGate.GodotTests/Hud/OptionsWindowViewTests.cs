using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class OptionsWindowViewTests
{
    private readonly List<OptionsTab> tabs = [];
    private ClientSettings settings = null!;
    private OptionsWindowView view = null!;
    private OptionsWindow window = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        settings = ClientSettings.Load();
        view = await HudScene.Show<OptionsWindowView>("res://Hud/options_window.tscn");
        window = new OptionsWindow();
        window.Toggle();
        tabs.Clear();
        view.TabChosen += tabs.Add;
        Show();
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => view.Free();

    [TestCase]
    public void Window_HasNoClassicNodes()
    {
        foreach (var type in (string[])
            [
                nameof(NinePatchRect), nameof(Button), nameof(CheckBox), nameof(HSlider),
                nameof(TabContainer), nameof(PanelContainer),
            ])
        {
            var ours = view.FindChildren("*", type, true, false).Where(node => !node.Name.ToString().StartsWith('_'));
            AssertThat(ours.Count()).IsEqual(0);
        }

        foreach (var label in view.FindChildren("*", nameof(Label), true, false))
        {
            AssertThat(label is HudLabel).IsTrue();
        }
    }

    [TestCase]
    public void Window_KeepsClicksOffTheWorld()
    {
        var frame = view.GetNode<Control>("Window/Frame").GetGlobalRect();

        AssertThat(ClickThrough.ReachesTheWorldAt(frame.Position + new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.End - new Vector2(2, 2))).IsFalse();
        AssertThat(ClickThrough.ReachesTheWorldAt(frame.GetCenter())).IsFalse();
    }

    [TestCase]
    public void ChoosingATab_FiresTabChosen()
    {
        var tab = view.GetNode($"%Pages/Tab{(int)OptionsTab.Keybinds}");

        tab.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));

        AssertThat(tabs).ContainsExactly(OptionsTab.Keybinds);
    }

    [TestCase]
    public void ShowingATab_ShowsItsPage()
    {
        window.Show(OptionsTab.Display);

        Show();

        AssertThat(view.GetNode<CanvasItem>("%Pages/Page/Display").Visible).IsTrue();
        AssertThat(view.GetNode<CanvasItem>("%Pages/Page/General").Visible).IsFalse();
    }

    [TestCase]
    public void Capturing_KeepsTheKeyFromTheGame()
    {
        var key = view.GetNode("%Sections").FindChildren("Key", "", true, false).Cast<KeyButton>().First();
        var id = key.GetParent().GetParent().Name.ToString();
        var captured = new List<string>();
        view.KeyCaptured += chord => captured.Add(id);
        window.StartCapture(id);
        Show();

        AssertThat(key.Waiting).IsTrue();
        Keyboard.Press(Key.F7);

        AssertThat(captured).ContainsExactly(id);
    }

    [TestCase]
    public void ConfirmingReset_TurnsTheButtonRed()
    {
        window.ChooseReset();

        Show();

        var reset = view.GetNode<ProceduralButton>("%Reset");
        AssertThat(reset.Warning).IsTrue();
        AssertThat(reset.GetNode<HudLabel>("Label").Colour).IsEqual(TextColour.Red);
    }

    [TestCase]
    public void ALinkedSetting_HangsFromItsParentWithoutARule()
    {
        var always = view.GetNode<SettingsRow>("%InformationalOptions/" + nameof(GeneralOption.ItemTooltipsAlways));
        var parent = view.GetNode<SettingsRow>("%InformationalOptions/" + nameof(GeneralOption.ItemTooltips));

        AssertThat(always.Tree).IsEqual(SettingsTree.Last);
        AssertThat(always.RuleAbove).IsFalse();
        AssertThat(parent.RuleAbove).IsTrue();
        AssertThat(view.GetNode("%InformationalOptions").GetChild<SettingsRow>(0).RuleAbove).IsFalse();
    }

    [TestCase]
    public void TheRows_LeaveRoomForTheScrollBar()
    {
        var page = view.GetNode<ScrollContainer>("%Pages/Page/General");

        AssertThat(page.GetNode<Control>("Room/Rows").Size.X).IsLessEqual(page.Size.X - 7 - 4);
    }

    [TestCase]
    public void TheGeneralPage_HeadsItsSectionsInOrder()
    {
        var headings = view.GetNode("%Pages/Page/General/Room/Rows")
            .GetChildren()
            .OfType<RuledHeading>()
            .Select(heading => heading.Text);

        AssertThat(headings).ContainsExactly("Chat", "Informational", "Networking", "Miscellaneous");
    }

    [TestCase]
    public void CursorSize_SitsUnderAccessibility()
    {
        var accessibility = view.GetNode("%AccessibilityOptions");

        AssertThat(accessibility.GetChild(accessibility.GetChildCount() - 1).Name.ToString()).IsEqual("CursorRow");
    }

    private void Show() => view.ShowOptions(window, settings, new CharacterSettings());
}
