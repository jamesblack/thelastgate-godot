using GdUnit4;
using Godot;
using TheLastGate.Hud.Smith;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Smith;

[TestSuite]
[RequireGodotRuntime]
public class SmithWindowSceneTests
{
    private const string ScenePath = "res://Hud/Smith/smith_window.tscn";
    private const int MiddleButton = 5;
    private const int SecondMaterial = 2;

    private SmithWindowView window = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTheWindow()
    {
        window = GD.Load<PackedScene>(ScenePath).Instantiate<SmithWindowView>();
        Tree.Root.AddChild(window);
        window.Visible = true;
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheWindow() => window.Free();

    [TestCase]
    public void TheWindow_IsSavedVisibleForTheEditorButStartsHiddenInGame()
    {
        var fresh = GD.Load<PackedScene>(ScenePath).Instantiate<SmithWindowView>();
        AssertThat(fresh.Visible).IsTrue();

        Tree.Root.AddChild(fresh);

        AssertThat(fresh.Visible).IsFalse();
        fresh.Free();
    }

    [TestCase]
    public void TheNormalLayout_ShowsFromTheStart()
    {
        AssertThat(SecondMaterialShows(window)).IsFalse();
        AssertThat(ButtonShows(window, MiddleButton)).IsTrue();
        AssertThat(ButtonText(window, MiddleButton)).IsEqual("Smith!");
    }

    [TestCase]
    public void Slot_IsEveryItemSlot()
    {
        for (var slot = 0; slot < SmithClicks.Slots; slot++)
        {
            AssertThat(window.Slot(slot).GetParent().Name.ToString()).IsEqual($"Slot{slot}");
        }
    }

    [TestCase]
    public void Button_IsEveryButton()
    {
        for (var number = SmithClicks.FirstButton; number <= SmithClicks.LastButton; number++)
        {
            AssertThat(window.Button(number).GetParent().Name.ToString()).IsEqual($"Button{number}");
        }
    }

    [TestCase]
    public void ShowLayout_OneMaterial()
    {
        window.ShowLayout(false, false, ShopKind.WeaponSmith);

        AssertThat(SecondMaterialShows(window)).IsFalse();
        AssertThat(ButtonShows(window, SmithClicks.FirstButton)).IsFalse();
        AssertThat(ButtonShows(window, MiddleButton)).IsTrue();
        AssertThat(ButtonShows(window, SmithClicks.LastButton)).IsFalse();
        AssertThat(ButtonText(window, MiddleButton)).IsEqual("Smith!");
    }

    [TestCase]
    public void ShowLayout_Claystone()
    {
        window.ShowLayout(true, false, ShopKind.ArmourSmith);

        AssertThat(SecondMaterialShows(window)).IsTrue();
    }

    [TestCase]
    public void ShowLayout_WeaponWhetstone()
    {
        window.ShowLayout(false, true, ShopKind.WeaponSmith);

        AssertThat(ButtonTexts()).ContainsExactly("Grind", "Hone", "Sharpen");
    }

    [TestCase]
    public void ShowLayout_ArmourWhetstone()
    {
        window.ShowLayout(false, true, ShopKind.ArmourSmith);

        AssertThat(ButtonTexts()).ContainsExactly("Smooth", "Polish", "Thicken");
    }

    [TestCase]
    public void Captions_ShowOnlyOnEmptySlots()
    {
        ItemSlot[] items = [new(5000, 0, 0, 0), default, default, default];

        window.ShowSlots(items);

        AssertThat(Caption(0).Visible).IsFalse();
        AssertThat(Caption(1).Visible).IsTrue();
        AssertThat(Caption(3).Visible).IsTrue();
    }

    [TestCase]
    public void SavingInTheEditor_KeepsTheNormalLayout()
    {
        window.ShowLayout(true, true, ShopKind.WeaponSmith);
        window.ShowSlots([new ItemSlot(5000, 0, 0, 0), new ItemSlot(5001, 0, 0, 0), default, default]);

        window.Notification((int)Node.NotificationEditorPreSave);
        var packed = new PackedScene();
        packed.Pack(window);
        var saved = packed.Instantiate<SmithWindowView>();

        AssertThat(SecondMaterialShows(saved)).IsFalse();
        AssertThat(ButtonShows(saved, SmithClicks.FirstButton)).IsFalse();
        AssertThat(saved.GetNode<CanvasItem>("%Slot0/Caption").Visible).IsTrue();
        saved.Free();
    }

    [TestCase]
    public void OnlyTheBenchFades()
    {
        AssertThat(Panel("%Bench").FadesAs).IsEqual(WindowBackground.Smith);
        AssertThat(Panel("%Footer").FadesAs).IsEqual(WindowBackground.None);
        AssertThat(Panel("%Frame").FadesAs).IsEqual(WindowBackground.None);
        for (var number = SmithClicks.FirstButton; number <= SmithClicks.LastButton; number++)
        {
            AssertThat(((ProceduralPanel)window.Button(number).GetParent()).FadesAs).IsEqual(WindowBackground.None);
        }
    }

    [TestCase]
    public void TheFooter_SharesTheFramesBorder() =>
        AssertThat(Panel("%Footer").Edges).IsEqual(ProceduralEdges.Top);

    [TestCase]
    public void CloseButton_IsTheSharedCloseButton() =>
        AssertThat(window.CloseButton.SceneFilePath).IsEqual("res://Procedural/close_button.tscn");

    [TestCase]
    public void Buttons_AreTheSharedTextButton()
    {
        for (var number = SmithClicks.FirstButton; number <= SmithClicks.LastButton; number++)
        {
            AssertThat(window.Button(number).GetParent().SceneFilePath).IsEqual("res://Procedural/text_button.tscn");
        }
    }

    private static bool SecondMaterialShows(SmithWindowView shown) =>
        shown.GetNode<CanvasItem>($"%Slot{SecondMaterial}").Visible && shown.GetNode<CanvasItem>("%And").Visible;

    private static bool ButtonShows(SmithWindowView shown, int number) =>
        shown.GetNode<CanvasItem>($"%Button{number}").Visible;

    private static string ButtonText(SmithWindowView shown, int number) =>
        shown.GetNode<Label>($"%Button{number}/Label").Text;

    private string[] ButtonTexts() =>
    [
        ButtonText(window, SmithClicks.FirstButton), ButtonText(window, MiddleButton),
        ButtonText(window, SmithClicks.LastButton),
    ];

    private CanvasItem Caption(int slot) => window.GetNode<CanvasItem>($"%Slot{slot}/Caption");

    [TestCase]
    public async Task AClickOnTheFrame_StaysOffTheWorld()
    {
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(ClickThrough.ReachesTheWorld(window.GetNode<Control>("Window/Frame"))).IsFalse();
    }

    private ProceduralPanel Panel(string path) => window.GetNode<ProceduralPanel>(path);
}
