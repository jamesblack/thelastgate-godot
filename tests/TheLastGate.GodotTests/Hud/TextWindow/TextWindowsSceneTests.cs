using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Hud.TextWindow;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.TextWindow;

[TestSuite]
[RequireGodotRuntime]
public class TextWindowsSceneTests
{
    private const string ScenePath = "res://Hud/TextWindow/text_windows.tscn";
    private const string TextButtonPath = "res://Procedural/text_button.tscn";

    private readonly MessageOfTheDayLines messageLines = new();
    private TextWindowView view = null!;
    private TextWindows windows = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        windows = new TextWindows(new WindowPause());
        view = GD.Load<PackedScene>(ScenePath).Instantiate<TextWindowView>();
        Tree.Root.AddChild(view);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => view.Free();

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>(ScenePath).Instantiate<TextWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void Button_IsEveryButton()
    {
        AssertThat(view.Button("CloseButton").SceneFilePath).IsEqual("res://Procedural/close_button.tscn");
        foreach (var name in new[] { "OkButton", "LeftButton", "RightButton" })
        {
            var button = view.Button(name).GetParent();
            AssertThat(button.Name.ToString()).IsEqual(name);
            AssertThat(button.SceneFilePath).IsEqual(TextButtonPath);
        }
    }

    [TestCase]
    public void TheMessageOfTheDay_ShowsItsLinesAndLetsGo()
    {
        messageLines.Add(new LogLine("Welcome back!", (int)TextColour.Green));
        windows.Show(new WindowRequest(WindowKind.MessageOfTheDay, 0));

        view.Show(windows, messageLines);

        AssertThat(view.Visible).IsTrue();
        AssertThat(Shows("%Message")).IsTrue();
        AssertThat(Shows("%Tutorial")).IsFalse();
        AssertThat(Shows("%PictureArea")).IsFalse();
        AssertThat(MessageLine(0).Text).IsEqual("Welcome back!");
        AssertThat(MessageLine(0).Colour).IsEqual(TextColour.Green);
        AssertThat(Caption("OkButton")).IsEqual("Let's Go!");
        AssertThat(Shows("%OkButton")).IsTrue();
        AssertThat(Shows("%LeftButton")).IsFalse();
        AssertThat(Shows("%RightButton")).IsFalse();
    }

    [TestCase]
    public void TheWelcome_OffersTheTutorial()
    {
        windows.Show(new WindowRequest(WindowKind.NewPlayerWelcome, 0));

        view.Show(windows, messageLines);

        AssertThat(Shows("%Message")).IsTrue();
        AssertThat(Shows("%OkButton")).IsFalse();
        AssertThat(Caption("LeftButton")).IsEqual("Tutorial");
        AssertThat(Caption("RightButton")).IsEqual("I'm Good");
    }

    [TestCase]
    public void TheTutorial_ShowsItsPageAndPicture()
    {
        windows.Show(new WindowRequest(WindowKind.Tutorial, 1));

        view.Show(windows, messageLines);

        AssertThat(Shows("%Message")).IsFalse();
        AssertThat(Shows("%Tutorial")).IsTrue();
        AssertThat(Shows("%PictureArea")).IsTrue();
        AssertThat(view.GetNode<Label>("%Tutorial/Line0").Text).IsEqual(TutorialPages.Text(1, 1)[0]);
        AssertThat(view.GetNode<ArchiveSprite>("%PictureArea/Picture").SpriteNumber)
            .IsEqual(TutorialPages.Picture(1, 1));
        AssertThat(Caption("LeftButton")).IsEqual("Prev");
        AssertThat(Caption("RightButton")).IsEqual("Next");
        AssertThat(Shows("%OkButton")).IsFalse();
    }

    [TestCase]
    public void TheFooter_ShowsTheTopWindowsButtons()
    {
        windows.Show(new WindowRequest(WindowKind.MessageOfTheDay, 0));
        windows.Show(new WindowRequest(WindowKind.NewPlayerWelcome, 0));

        view.Show(windows, messageLines);

        AssertThat(Shows("%OkButton")).IsFalse();
        AssertThat(Shows("%LeftButton")).IsTrue();
        AssertThat(Caption("LeftButton")).IsEqual("Tutorial");
    }

    [TestCase]
    public void TheWelcome_ShowsOverATutorial()
    {
        windows.Show(new WindowRequest(WindowKind.Tutorial, 2));
        windows.Show(new WindowRequest(WindowKind.NewPlayerWelcome, 0));

        view.Show(windows, messageLines);

        AssertThat(Shows("%Message")).IsTrue();
        AssertThat(Shows("%Tutorial")).IsFalse();
        AssertThat(Shows("%PictureArea")).IsFalse();
        AssertThat(Caption("LeftButton")).IsEqual("Tutorial");
        AssertThat(Caption("RightButton")).IsEqual("I'm Good");
    }

    [TestCase]
    public void OnlyTheBodyFades()
    {
        AssertThat(Panel("%Body").FadesAs).IsEqual(WindowBackground.Text);
        AssertThat(Panel("%PictureArea").FadesAs).IsEqual(WindowBackground.Text);
        AssertThat(Panel("%Frame").FadesAs).IsEqual(WindowBackground.None);
        AssertThat(Panel("%Footer").FadesAs).IsEqual(WindowBackground.None);
        foreach (var name in new[] { "%OkButton", "%LeftButton", "%RightButton" })
        {
            AssertThat(Panel(name).FadesAs).IsEqual(WindowBackground.None);
        }
    }

    [TestCase]
    public void SavingInTheEditor_LeavesNoPreview()
    {
        messageLines.Add(new LogLine("Preview text", (int)TextColour.Yellow));
        windows.Show(new WindowRequest(WindowKind.Tutorial, 1));
        view.Show(windows, messageLines);

        view.Notification((int)Node.NotificationEditorPreSave);
        var packed = new PackedScene();
        packed.Pack(view);
        var saved = packed.Instantiate<TextWindowView>();

        AssertThat(saved.GetNode<CanvasItem>("%Tutorial").Visible).IsFalse();
        AssertThat(saved.GetNode<CanvasItem>("%PictureArea").Visible).IsFalse();
        AssertThat(saved.GetNode<CanvasItem>("%Message").Visible).IsTrue();
        AssertThat(saved.GetNode<Label>("%Message/Line0").Text).IsEqual("");
        AssertThat(saved.GetNode<HudLabel>("%Message/Line0").Colour).IsEqual(TextColour.Yellow);
        saved.Free();
    }

    private bool Shows(string path) => view.GetNode<CanvasItem>(path).Visible;

    private HudLabel MessageLine(int line) => view.GetNode<HudLabel>($"%Message/Line{line}");

    private string Caption(string button) => view.GetNode<Label>($"%{button}/Label").Text;

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        view.Visible = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(view.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    private ProceduralPanel Panel(string path) => view.GetNode<ProceduralPanel>(path);
}
