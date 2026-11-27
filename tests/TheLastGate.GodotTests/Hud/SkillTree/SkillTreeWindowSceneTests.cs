using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;
using TheLastGate.GodotTests.Windows;
using TheLastGate.Hud;
using TheLastGate.Hud.SkillTree;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.SkillTree;

[TestSuite]
[RequireGodotRuntime]
public class SkillTreeWindowSceneTests
{
    private const int Nodes = 12;
    private const long SpriteCacheBytes = 64L * 1024 * 1024;

    private SpriteLoader sprites = null!;
    private SkillTreeWindowView window = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(window.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        window = GD.Load<PackedScene>("res://Hud/SkillTree/skill_tree_window.tscn").Instantiate<SkillTreeWindowView>();
        Tree.Root.AddChild(window);
        window.Visible = true;
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        var context = new HudContext(new ClassicFonts(), sprites, null);
        foreach (var user in window.FindChildren("*", "", true, false).OfType<IHudContextUser>())
        {
            user.ShowContext(context);
        }

        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow()
    {
        window.Free();
        sprites.Dispose();
    }

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>("res://Hud/SkillTree/skill_tree_window.tscn")
            .Instantiate<SkillTreeWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void TheWindow_HasTheNodesTheHudWires()
    {
        AssertThat(window.CloseButton).IsNotNull();
        for (var node = 0; node < Nodes; node++)
        {
            AssertThat(window.Node(node).GetNodeOrNull<CanvasItem>("Ring")).IsNotNull();
            AssertThat(window.Node(node).GetNodeOrNull<ArchiveSprite>("Icon")).IsNotNull();
            AssertThat(window.Node(node).GetNodeOrNull<CanvasItem>("Corruption")).IsNotNull();
        }
    }

    [TestCase]
    public void TheWindow_KeepsTheClassicSize()
    {
        AssertThat(window.GetNode<Control>("Window/Frame").Size).IsEqual(new Vector2(300, 331));
    }

    [TestCase]
    public void NodesSitWhereTheyDoToday()
    {
        AssertThat(window.Node(0).GlobalPosition - window.GlobalPosition).IsEqual(new Vector2(92, 151));
        AssertThat(window.Node(7).GlobalPosition - window.GlobalPosition).IsEqual(new Vector2(136, 24));
    }

    [TestCase]
    public void IconsAreCentredOverTheirCircles()
    {
        for (var node = 0; node < Nodes; node++)
        {
            var circle = window.Node(node).GetNode<Control>("Circle");
            foreach (var name in new[] { "Icon", "Corruption" })
            {
                var icon = window.Node(node).GetNode<TextureRect>(name);
                AssertThat(icon.GetRect()).IsEqual(circle.GetRect());
                AssertThat(icon.StretchMode).IsEqual(TextureRect.StretchModeEnum.KeepCentered);
            }
        }

        var centreIcon = window.GetNode<TextureRect>("%CentreIcon");
        AssertThat(centreIcon.GetRect()).IsEqual(centreIcon.GetParent().GetNode<Control>("Circle").GetRect());
        AssertThat(centreIcon.StretchMode).IsEqual(TextureRect.StretchModeEnum.KeepCentered);
    }

    [TestCase]
    public void TheWindow_FadesEvenly() => FadeLayers.AssertNoStackedFills(window);

    [TestCase]
    public void AtNoBackground_OnlyTheContentFades()
    {
        var settings = ClientSettings.Load();
        settings.BackgroundOpacity[WindowBackground.SkillTree] = WindowBackgrounds.Clear;
        var context = new HudContext(null, null, settings);
        foreach (var user in window.FindChildren("*", "", true, false).OfType<IHudContextUser>())
        {
            user.ShowContext(context);
        }

        AssertThat(window.GetNode<CanvasItem>("Window/Frame").SelfModulate.A).IsEqual(1f);
        AssertThat(window.GetNode<CanvasItem>("%TreeArea").SelfModulate.A).IsEqual(0f);
        var solid = window.FindChildren("*", "", true, false)
            .Where(node => node is ClickArea or Label or ArchiveSprite || node.Name == "Circle")
            .Cast<CanvasItem>()
            .ToList();
        AssertThat(solid).IsNotEmpty();
        AssertThat(solid.All(item => FadeLayers.EffectiveAlpha(item) == 1f)).IsTrue();
    }

    [TestCase]
    public void CloseButton_IsTheSharedCloseButton() =>
        AssertThat(window.CloseButton.SceneFilePath).IsEqual("res://Procedural/close_button.tscn");
}
