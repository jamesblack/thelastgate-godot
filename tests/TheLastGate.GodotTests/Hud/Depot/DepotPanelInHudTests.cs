using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Game;
using TheLastGate.Hud;
using TheLastGate.Hud.Depot;
using TheLastGate.Net;
using TheLastGate.Procedural;
using TheLastGate.State;
using TheLastGate.Windows;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
[RequireGodotRuntime]
public class DepotPanelInHudTests
{
    private const int MainNumber = 1;
    private const int AltNumber = 2;
    private const int DepotFlag = 0x8000;
    private const byte LastPage = 7;
    private const byte LastSlot = 63;

    private static readonly ServerAddress TestServer = new("depot-panel.test", "", 1);

    private readonly List<byte[]> sent = [];
    private HudView hud = null!;
    private TickProcessor processor = null!;
    private ClientSettings settings = null!;
    private GameState state = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private DepotCharactersView Panel => hud.GetNode<DepotCharactersView>("%DepotCharacters");

    private Control DepotWindow => hud.GetNode<Control>("%DepotWindow");

    private HudLabel Status => Panel.GetNode<HudLabel>("%Status");

    private CategoryListView List => Panel.GetNode<CategoryListView>("%List");

    [BeforeTest]
    public async Task ShowTheHud()
    {
        state = new GameState();
        processor = new TickProcessor(state);
        sent.Clear();
        settings = ClientSettings.Load();
        hud = GD.Load<PackedScene>("res://Hud/hud_view.tscn").Instantiate<HudView>();
        Tree.Root.AddChild(hud);
        hud.ClientSettings = settings;
        hud.Depots = new DepotDirectory(
            new DepotLayoutKeeper(TestServer),
            Season.Current,
            [new DepotCharacter(MainNumber, "Main"), new DepotCharacter(AltNumber, "Altchar")]);
        hud.Show(state, new GameSession(state), new ClassicFonts(), sent.Add);
        await Frames(2);
    }

    [AfterTest]
    public void CloseTheHud() => hud.Free();

    [TestCase]
    public async Task OptionOff_PanelHidden()
    {
        settings.DepotCharacterList = false;

        await OpenDepot(MainNumber);

        AssertThat(DepotWindow.Visible).IsTrue();
        AssertThat(Panel.Visible).IsFalse();
    }

    [TestCase]
    public async Task OptionOn_PanelShowsWithTheDepotWindow()
    {
        settings.DepotCharacterList = true;
        hud.Refresh(0);
        AssertThat(Panel.Visible).IsFalse();

        await OpenDepot(MainNumber);
        AssertThat(Panel.Visible).IsTrue();

        processor.Process(TestCommand.Payload(TestCommand.Of(ServerCommand.CloseShop)));
        hud.Refresh(0);
        AssertThat(Panel.Visible).IsFalse();
    }

    [TestCase]
    public async Task ThePanel_SitsRightOfTheDepotWindowOnScreen()
    {
        settings.DepotCharacterList = true;

        await OpenDepot(MainNumber);

        var windowRect = DepotWindow.GetNode<Control>("Window").GetGlobalRect();
        var panelRect = Panel.GetGlobalRect();
        AssertThat(panelRect.Position.X).IsEqual(windowRect.End.X + 1);
        AssertThat(panelRect.Position.Y).IsEqual(windowRect.Position.Y);
        AssertThat(panelRect.End.Y).IsEqual(windowRect.End.Y);
        AssertThat(new Rect2(0, 0, 1280, 720).Encloses(panelRect)).IsTrue();
    }

    [TestCase]
    public async Task ChoosingAnotherCharactersPage_SendsLookWithTheDepotFlag()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);

        var altRow = RowIndexOf("Altchar");
        Click(altRow);
        await Frames(2);
        Click(RowIndexOf("Page 4", altRow + 1));

        AssertThat(sent).HasSize(1);
        AssertThat(sent[0]).ContainsExactly(ClientPackets.OneValue(ClientCommand.Look, AltNumber | DepotFlag));
    }

    [TestCase]
    public async Task SortingAnotherCharactersDepot_SaysSortDepotWithTheirName()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);

        Click(RowIndexOf("Altchar"), MapButton.Right);
        ChooseFromMenu("Sort depot");

        AssertThat(sent.SelectMany(packet => packet).ToArray())
            .ContainsExactly(ChatPackets.Say("#sortdepot Altchar").SelectMany(packet => packet).ToArray());
    }

    [TestCase]
    public async Task ChoosingAPageOfTheOpenDepot_TurnsThePageWithoutSending()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);

        Click(RowIndexOf("Page 3"));

        AssertThat(sent).IsEmpty();
        AssertThat(DepotWindow.GetNode<ProceduralPanel>("%Tabs/Page2").Fill).IsEqual(ProceduralFill.Selected);
    }

    [TestCase]
    public async Task RefreshWhileSwitching_AsksForTheRequestedDepot()
    {
        settings.DepotCharacterList = true;
        state.ShopRefresh.Send = sent.Add;
        await OpenDepot(MainNumber);
        await ChooseAltPage("Page 4");
        sent.Clear();

        PlayTicks(9);
        AssertThat(sent).ContainsExactly(ClientPackets.OneValue(ClientCommand.Look, AltNumber | DepotFlag));

        await OpenDepot(AltNumber);
        sent.Clear();
        PlayTicks(9);
        AssertThat(sent).ContainsExactly(ClientPackets.OneValue(ClientCommand.Look, AltNumber | DepotFlag));
    }

    [TestCase]
    public async Task ChoosingAnotherCharactersPage_TurnsToItOnceTheirDepotOpens()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);

        await ChooseAltPage("Page 4");
        hud.Refresh(0);
        AssertThat(DepotTab(0).Fill).IsEqual(ProceduralFill.Selected);

        await OpenDepot(AltNumber);
        AssertThat(DepotTab(3).Fill).IsEqual(ProceduralFill.Selected);
    }

    [TestCase]
    public async Task NoAnswer_ShowsInThePanel()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);
        await ChooseAltPage("Page 4");

        await Seconds(DepotSwitch.AnswerSeconds + 0.2);
        hud.Refresh(0);

        AssertThat(Status.Visible).IsTrue();
        AssertThat(Status.Text).IsEqual("Altchar's depot didn't answer.");
    }

    [TestCase]
    public async Task NoAnswerWhileThePanelIsHidden_IsNotShownLater()
    {
        settings.DepotCharacterList = true;
        await OpenDepot(MainNumber);
        await ChooseAltPage("Page 4");
        processor.Process(TestCommand.Payload(TestCommand.Of(ServerCommand.CloseShop)));
        hud.Refresh(0);

        await Seconds(DepotSwitch.AnswerSeconds + 0.2);
        hud.Refresh(0);
        await OpenDepot(MainNumber);

        AssertThat(Status.Visible).IsFalse();
    }

    private static async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task Seconds(double seconds) =>
        await Tree.ToSignal(Tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task OpenDepot(int characterNumber)
    {
        processor.Process(
            TestCommand.Payload(
                TestCommand.Of(ServerCommand.Look3)
                    .UInt16(0)
                    .UInt16(0)
                    .UInt16(0)
                    .UInt16((ushort)(characterNumber | DepotFlag)),
                TestCommand.Of(ServerCommand.Look7).Bytes(LastPage, LastSlot).UInt16(0).Bytes(0, 0, 0)));
        hud.Refresh(0);
        await Frames(2);
    }

    private async Task ChooseAltPage(string pageText)
    {
        var altRow = RowIndexOf("Altchar");
        Click(altRow);
        await Frames(2);
        Click(RowIndexOf(pageText, altRow + 1));
    }

    private void PlayTicks(int count)
    {
        for (var tick = 0; tick < count; tick++)
        {
            processor.Process([]);
        }
    }

    private ProceduralPanel DepotTab(int page) => DepotWindow.GetNode<ProceduralPanel>($"%Tabs/Page{page}");

    private CategoryRowView Row(int index) => List.GetNode<CategoryRowView>($"Cells/List/Rows/Row{index}");

    private string RowText(int index) => Row(index).GetNode<HudLabel>("Cells/Name").Text;

    private int RowIndexOf(string text, int firstIndex = 0)
    {
        for (var index = firstIndex; List.GetNodeOrNull($"Cells/List/Rows/Row{index}") != null; index++)
        {
            if (Row(index).Visible && RowText(index) == text)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"No row shows \"{text}\".");
    }

    private void Click(int index, MapButton button = MapButton.Left) =>
        Row(index).GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(button));

    private void ChooseFromMenu(string text)
    {
        var item = Panel.GetNode("%Menu/Frame/Items")
            .GetChildren()
            .Single(row => row.GetNodeOrNull<HudLabel>("Cells/Label")?.Text == text);
        item.GetNode<ClickArea>("Click").EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
    }
}
