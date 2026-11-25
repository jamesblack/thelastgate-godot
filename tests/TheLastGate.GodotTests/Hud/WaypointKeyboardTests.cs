using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.Hotkeys;
using TheLastGate.Hud;
using TheLastGate.Hud.Waypoints;
using TheLastGate.Net;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class WaypointKeyboardTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    private readonly NavigationKeys navigationKeys = new();
    private readonly List<byte[]> sentPackets = [];
    private HudView hud = null!;
    private GameSession session = null!;
    private SpriteLoader sprites = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private WaypointWindowView View => hud.GetNode<WaypointWindowView>("%WaypointWindow");

    [BeforeTest]
    public async Task OpenTheWaypointWindow()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        var state = new GameState();
        session = new GameSession(state);
        navigationKeys.TurnOff();
        sentPackets.Clear();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.Show(state, session, new ClassicFonts(), sentPackets.Add);
        session.WaypointWindow.Open();
        hud.Refresh(0);
        await Frames(2);
    }

    [AfterTest]
    public void CloseTheHud()
    {
        hud.Free();
        sprites.Dispose();
        BindingActions.ResetAll();
        navigationKeys.TurnOn();
    }

    [TestCase]
    public async Task OpeningTheWindowPutsTheTypingInTheSearch()
    {
        await Frames(1);

        AssertThat(View.Search.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task DownSelectsTheTopRowAndKeepsTheTypingInTheSearch()
    {
        Keyboard.Press(Key.Down);
        await Frames(1);

        AssertThat(session.WaypointWindow.Selected).IsEqual(0);
        AssertThat(View.Search.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task DownAndUpMoveTheSelection()
    {
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Up);
        await Frames(1);

        AssertThat(session.WaypointWindow.Selected).IsEqual(1);
    }

    [TestCase]
    public async Task UpOnTheTopRowClearsTheSelection()
    {
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Up);
        await Frames(1);

        AssertThat(session.WaypointWindow.Selected).IsNull();
        AssertThat(View.Search.HasFocus()).IsTrue();
    }

    [TestCase]
    public async Task UpWithNothingSelectedSelectsNothing()
    {
        Keyboard.Press(Key.Up);
        await Frames(1);

        AssertThat(session.WaypointWindow.Selected).IsNull();
    }

    [TestCase]
    public async Task EnterTravelsToTheSelectedWaypoint()
    {
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Down);
        Keyboard.Press(Key.Enter);
        await Frames(1);

        AssertThat(sentPackets.Any(packet => packet.SequenceEqual(WaypointPackets.Travel(1)))).IsTrue();
    }

    [TestCase]
    public async Task TypingAfterMovingTheSelectionStillSearches()
    {
        Keyboard.Press(Key.Down);
        Keyboard.Type("a");
        await Frames(1);

        AssertThat(View.Search.Text).IsEqual("a");
        AssertThat(session.WaypointWindow.Search).IsEqual("a");
    }

    [TestCase]
    public async Task WithEnterToTalkOn_UpDoesNotOpenTheChatLine()
    {
        var line = hud.GetNode<ChatView>("%Chat").Line;
        line.EnterToTalk = true;

        Keyboard.Press(Key.Up);
        await Frames(1);

        AssertThat(line.TakesTyping).IsFalse();
    }

    private static async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
