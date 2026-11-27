using GdUnit4;
using Godot;
using TheLastGate.GodotTests.Hud;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Notes;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
[RequireGodotRuntime]
public class WorldMapWindowViewTests
{
    private NoteSet notes = new();
    private WorldMapWindowView view = null!;
    private WorldMapWindow window = new();

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheMap()
    {
        notes = new NoteSet();
        window = new WorldMapWindow();
        window.OpenAtPlayer();
        view = GD.Load<PackedScene>("res://Map/world_map_window.tscn").Instantiate<WorldMapWindowView>();
        Tree.Root.AddChild(view);
        view.ShowWindow(window, null, notes, (0, 0));
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheMap() => view.Free();

    [TestCase]
    public void ShowPathsInWorldBox_TurnsTheSettingOn()
    {
        view.GetNode<ProceduralCheckBox>("%ShowPathsInWorld").Toggle();

        AssertThat(notes.ShowInWorld).IsTrue();
        AssertThat(notes.Changed).IsTrue();
    }

    [TestCase]
    public async Task ShowPathsInWorldBox_FollowsTheSetting()
    {
        notes.ShowInWorld = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(view.GetNode<ProceduralCheckBox>("%ShowPathsInWorld").Checked).IsTrue();
    }

    [TestCase]
    public void Window_HasNoClassicSpriteNodes()
    {
        foreach (var type in (string[])[nameof(NinePatchRect), nameof(CheckBox), nameof(Button), nameof(PopupMenu)])
        {
            AssertThat(view.FindChildren("*", type, true, false).Count).IsEqual(0);
        }
    }

    [TestCase("HelpBlocker")]
    [TestCase("LabelBlocker")]
    [TestCase("DeleteBlocker")]
    [TestCase("NewBlocker")]
    [TestCase("ImportBlocker")]
    [TestCase("ExportBlocker")]
    [TestCase("DeleteCategoryBlocker")]
    [TestCase("DuplicatesBlocker")]
    [TestCase("MergePrompt")]
    public void APromptsBackdrop_KeepsClicksOffTheWorld(string backdrop)
    {
        view.FindChild(backdrop, true, false).Set(CanvasItem.PropertyName.Visible, true);

        AssertThat(ClickThrough.ReachesTheWorldAt(view.GetViewportRect().Size - new Vector2(5, 5))).IsFalse();
    }

    [TestCase]
    public void MenusAndPrompts_FloatInTheWindowsLastChild()
    {
        var overlays = view.GetNode<Control>("Overlays");

        AssertThat(overlays.GetIndex()).IsEqual(view.GetChildCount() - 1);
        foreach (var name in (string[])["%HelpBlocker", "%NewBlocker", "%MergePrompt", "%Menu", "%NoteMenu"])
        {
            var overlay = view.GetNode<Control>(name);
            AssertThat(overlay.GetParent()).IsEqual(overlays);
            AssertThat(overlay.TopLevel).IsFalse();
            AssertThat(overlay.ZAsRelative).IsTrue();
        }
    }

    [TestCase]
    public void AWindowAddedLater_DrawsAboveTheMapsMenus()
    {
        var later = new Control();
        view.GetParent().AddChild(later);

        AssertThat(later.GetIndex()).IsGreater(view.GetIndex());
        AssertThat(view.GetNode<CanvasItem>("%NoteMenu").ZIndex).IsEqual(0);
        later.Free();
    }

    [TestCase]
    public void APromptBackdrop_CoversTheScreen()
    {
        var help = view.GetNode<Control>("%HelpBlocker");
        help.Visible = true;

        AssertThat(help.GetGlobalRect()).IsEqual(view.GetViewportRect());
    }

    [TestCase]
    public void Escape_WithTheHelpUp_ClosesOnlyTheHelp()
    {
        OpenTheHelp();

        Keyboard.Press(Key.Escape);

        AssertThat(view.GetNode<Control>("%HelpBlocker").Visible).IsFalse();
        AssertThat(window.IsOpen).IsTrue();
    }

    [TestCase]
    public void ClosingTheMap_ClosesTheHelp()
    {
        OpenTheHelp();

        window.Close();
        view.ShowWindow(window, null, notes, (0, 0));
        window.OpenAtPlayer();
        view.ShowWindow(window, null, notes, (0, 0));

        AssertThat(view.GetNode<Control>("%HelpBlocker").Visible).IsFalse();
    }

    [TestCase]
    public void TheHelp_IsTheSameHeightOnEveryPage()
    {
        OpenTheHelp();
        var frame = view.GetNode<Control>("%HelpBlocker/Window/Frame");
        var firstHeight = frame.GetCombinedMinimumSize().Y;

        for (var page = 1; page < WorldMapHelp.Pages.Count; page++)
        {
            view.GetNode<ClickArea>("%HelpNext/Click")
                .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
            AssertThat(frame.GetCombinedMinimumSize().Y).IsEqual(firstHeight);
        }
    }

    [TestCase]
    public async Task AClash_OpensTheMergePrompt()
    {
        var places = notes.AddCategory("Places", 0);
        var there = notes.AddCategory("Dungeons", 0, places);
        var moving = notes.AddCategory("Dungeons", 0);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        view.GetNode<WorldMapCategories>("%Categories")
            .AskAboutClash(new NoteDrag(moving, true), new NoteDropSlot(places, -1, true), there);

        AssertThat(view.GetNode<MergePrompt>("%MergePrompt").Visible).IsTrue();
    }

    private void OpenTheHelp()
    {
        view.GetNode<ClickArea>("%Help").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
        AssertThat(view.GetNode<Control>("%HelpBlocker").Visible).IsTrue();
    }
}
