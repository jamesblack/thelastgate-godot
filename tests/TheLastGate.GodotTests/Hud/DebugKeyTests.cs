using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.Hotkeys;
using TheLastGate.Hud;
using TheLastGate.Profiling;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class DebugKeyTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    private static readonly DebugFacts Facts = new("1.4.2", 0x010203, "4.7.2", "Adapter", "Vendor", "3.3", 1, 3, 16.6);

    private readonly NavigationKeys navigationKeys = new();
    private HudView hud = null!;
    private SpriteLoader sprites = null!;
    private GameState state = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private TimingsView? Timings => hud.GetChildren().OfType<TimingsView>().SingleOrDefault();

    [BeforeTest]
    public async Task ShowTheHud()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        state = new GameState();
        navigationKeys.TurnOff();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.Show(state, new GameSession(state), new ClassicFonts(), _ => { });
        hud.ReadDebugFacts = () => Facts;
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
    public async Task WithoutTimings_TheDebugKeyPrintsTheFactsToChat()
    {
        var linesBefore = state.Chat.Lines.Count;
        Keyboard.Press(Key.F11);
        await Frames(2);

        AssertThat(state.Chat.Lines.Count).IsGreater(linesBefore);
        AssertThat(Timings).IsNull();
    }

    [TestCase]
    public async Task WithTimings_TheDebugKeyTogglesTheTimingsView()
    {
        hud.ShowTimings(new TimingRecorder(), new PacketCounter());
        var linesBefore = state.Chat.Lines.Count;

        Keyboard.Press(Key.F11);
        await Frames(2);
        AssertThat(Timings!.Visible).IsTrue();
        AssertThat(state.Chat.Lines.Count).IsEqual(linesBefore);

        Keyboard.Press(Key.F11);
        await Frames(2);
        AssertThat(Timings!.Visible).IsFalse();
    }

    private static async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
