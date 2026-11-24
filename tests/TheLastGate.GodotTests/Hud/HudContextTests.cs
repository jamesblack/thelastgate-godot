using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Game;
using TheLastGate.Hud;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public partial class HudContextTests
{
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    private HudView hud = null!;
    private SpriteLoader sprites = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowTheHud()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        var state = new GameState();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.Sprites = sprites;
        hud.Show(state, new GameSession(state), new ClassicFonts(), _ => { });
    }

    [AfterTest]
    public void CloseTheHud()
    {
        hud.Free();
        sprites.Dispose();
    }

    [TestCase]
    public void ANodeAddedInsideTheHud_IsShownTheContext()
    {
        var user = new ContextRecorder();

        hud.AddChild(user);

        AssertThat(user.Contexts).HasSize(1);
        AssertThat(user.Contexts[0].Fonts).IsNotNull();
        AssertThat(user.Contexts[0].Sprites).IsSame(sprites);
    }

    [TestCase]
    public void ANodeAddedDeeperInsideTheHud_IsShownTheContextToo()
    {
        var holder = new Node();
        var user = new ContextRecorder();
        holder.AddChild(user);

        hud.AddChild(holder);

        AssertThat(user.Contexts).HasSize(1);
    }

    [TestCase]
    public void ANodeOutsideTheHud_IsShownNothing()
    {
        var user = new ContextRecorder();

        Tree.Root.AddChild(user);

        AssertThat(user.Contexts).IsEmpty();
        user.Free();
    }

    [TestCase]
    public void ChangingDarkMode_ShowsTheNewContext()
    {
        var user = new ContextRecorder();
        hud.AddChild(user);

        hud.DarkMode = !hud.DarkMode;

        AssertThat(user.Contexts[^1].DarkMode).IsEqual(hud.DarkMode);
    }

    private sealed partial class ContextRecorder : Node, IHudContextUser
    {
        public ContextRecorder() => AddToGroup(HudContext.Group);

        public List<HudContext> Contexts { get; } = [];

        public void ShowContext(HudContext context) => Contexts.Add(context);
    }
}
