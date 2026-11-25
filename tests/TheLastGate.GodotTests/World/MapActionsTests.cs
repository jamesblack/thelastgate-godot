using GdUnit4;
using TheLastGate.Cursors;
using TheLastGate.Game;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MapActionsTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsObjectAndCharacter = 0b0110_1010;
    private const int OriginX = 500;
    private const int OriginY = 400;
    private const int BuildItem = 46;
    private const int Candle = 1500;
    private const int Guard = 3000;
    private const int GuardNumber = 812;

    private const MapKeys QueueKeys = MapKeys.Shift | MapKeys.Ctrl;
    private static readonly (float Column, float Row) OnPlayerTile = (27.5f, 27.5f);
    private static readonly (float Column, float Row) NearTheNextTile = (27.9f, 27.5f);

    private bool giveMore;
    private bool useQueue;

    [BeforeTest]
    public void SwitchTheOptionsOff() => (giveMore, useQueue) = (false, false);

    [TestCase]
    public void NoKeys_HighlightsTheFloorWithTheWalkCursor()
    {
        var action = Decide(new GameState(), OnPlayerTile, MapKeys.None, MapButton.None);

        AssertThat(action).IsEqual(new MapAction(Tile(27, 27), MapLayer.Floor, MouseCursor.Walk, null, null));
    }

    [TestCase]
    public void NoKeys_WalksOnLeftAndTurnsOnRight()
    {
        var state = State();

        AssertThat(Decide(state, OnPlayerTile, MapKeys.None, MapButton.Left).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.Move, OriginX + 27, OriginY + 27));
        AssertThat(Decide(state, OnPlayerTile, MapKeys.None, MapButton.Right).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.Turn, OriginX + 27, OriginY + 27));
    }

    [TestCase]
    public void AwayFromTheMap_DoesNothing()
    {
        AssertThat(Decide(State(), null, MapKeys.None, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    [TestCase]
    public void BuildMode_PicksUpOnLeftAndDropsOnRightAtTheFloorTile()
    {
        var state = State(HeldItem(BuildItem));

        var hover = Decide(state, OnPlayerTile, MapKeys.Shift, MapButton.None);
        AssertThat(hover).IsEqual(new MapAction(Tile(27, 27), MapLayer.Floor, MouseCursor.None, null, null));
        AssertThat(Decide(state, OnPlayerTile, MapKeys.None, MapButton.Left).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.PickUp, OriginX + 27, OriginY + 27));
        AssertThat(Decide(state, OnPlayerTile, MapKeys.Ctrl, MapButton.Right).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.Drop, OriginX + 27, OriginY + 27));
    }

    [TestCase]
    public void Shift_HighlightsTheNearestItemWithTheTakeCursor()
    {
        var action = Decide(State(Item(28, 27)), NearTheNextTile, MapKeys.Shift, MapButton.None);

        AssertThat(action).IsEqual(new MapAction(Tile(28, 27), MapLayer.Object, MouseCursor.Take, null, null));
    }

    [TestCase]
    public void Shift_PicksUpAnItemOnLeftAndLooksAtItOnRight()
    {
        var state = State(Item(28, 27));

        AssertThat(Decide(state, NearTheNextTile, MapKeys.Shift, MapButton.Left).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.PickUp, OriginX + 28, OriginY + 27));
        AssertThat(Decide(state, NearTheNextTile, MapKeys.Shift, MapButton.Right).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.LookItem, OriginX + 28, OriginY + 27));
    }

    [TestCase]
    public void Shift_UsesAUsableItemWithTheUseCursor()
    {
        var state = State(Item(28, 27, MapFlags.IsUsable));

        AssertThat(Decide(state, NearTheNextTile, MapKeys.Shift, MapButton.None).Cursor).IsEqual(MouseCursor.Use);
        AssertThat(Decide(state, NearTheNextTile, MapKeys.Shift, MapButton.Left).Command)
            .IsEqual(MapCommandPackets.ForTile(ClientCommand.Use, OriginX + 28, OriginY + 27));
    }

    [TestCase]
    public void Shift_DropsAHeldItemOnEmptyFloor()
    {
        var state = State(HeldItem(Candle));

        var action = Decide(state, OnPlayerTile, MapKeys.Shift, MapButton.Left);

        AssertThat((action.HighlightedTile, action.HighlightedLayer, action.Cursor))
            .IsEqual((Tile(27, 27), MapLayer.Floor, MouseCursor.Drop));
        AssertThat(action.Command).IsEqual(MapCommandPackets.ForTile(ClientCommand.Drop, OriginX + 27, OriginY + 27));
    }

    [TestCase]
    public void Shift_WithAHeldItemOverAnItemShowsTheUseCursorOnlyForUsableItems()
    {
        var plain = State(HeldItem(Candle), Item(27, 27));
        var usable = State(HeldItem(Candle), Item(27, 27, MapFlags.IsUsable));

        AssertThat(Decide(plain, OnPlayerTile, MapKeys.Shift, MapButton.None).Cursor).IsEqual(MouseCursor.None);
        AssertThat(Decide(usable, OnPlayerTile, MapKeys.Shift, MapButton.None).Cursor).IsEqual(MouseCursor.Use);
    }

    [TestCase]
    public void CtrlShift_QueuesAUseOfTheNearestUsableItem()
    {
        var state = QueueState(Item(28, 27, MapFlags.IsUsable));

        AssertThat(Decide(state, NearTheNextTile, QueueKeys, MapButton.None))
            .IsEqual(new MapAction(Tile(28, 27), MapLayer.Object, MouseCursor.Use, null, null));
        var click = Decide(state, NearTheNextTile, QueueKeys, MapButton.Left);
        AssertThat(click.QueuedAction.GetValueOrDefault()).IsEqual((OriginX + 28, OriginY + 27, ClientCommand.Use));
        AssertThat(click.Command).IsNull();
    }

    [TestCase]
    public void CtrlShift_WithAHeldItemGivesItToTheNearestCharacterAndRemembersIt()
    {
        var state = GiveMoreState(HeldItem(Candle), Character(28, 27));

        AssertThat(Decide(state, NearTheNextTile, QueueKeys, MapButton.None))
            .IsEqual(new MapAction(Tile(28, 27), MapLayer.Character, MouseCursor.Give, null, null));
        var click = Decide(state, NearTheNextTile, QueueKeys, MapButton.Left);
        AssertThat(click.Command).IsEqual(MapCommandPackets.ForCharacter(ClientCommand.Give, GuardNumber));
        AssertThat(click.GivesMore).IsTrue();
    }

    [TestCase]
    public void CtrlShift_GivingMoreWithNoCharacterNearDoesNothing()
    {
        var state = GiveMoreState(HeldItem(Candle));
        useQueue = true;

        AssertThat(Decide(state, OnPlayerTile, QueueKeys, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    [TestCase]
    public void CtrlShift_WithAnEmptyHandStillQueuesUses()
    {
        var state = GiveMoreState(Item(28, 27, MapFlags.IsUsable));
        useQueue = true;

        AssertThat(Decide(state, NearTheNextTile, QueueKeys, MapButton.Left).QueuedAction.GetValueOrDefault())
            .IsEqual((OriginX + 28, OriginY + 27, ClientCommand.Use));
    }

    [TestCase]
    public void Ctrl_GivingAsUsualDoesNotRememberTheGift()
    {
        var state = GiveMoreState(HeldItem(Candle), Character(28, 27));

        AssertThat(Decide(state, NearTheNextTile, MapKeys.Ctrl, MapButton.Left).GivesMore).IsFalse();
    }

    [TestCase]
    public void CtrlShift_WithTheQueueOffDoesNothing()
    {
        var state = State(Item(28, 27, MapFlags.IsUsable));

        AssertThat(Decide(state, NearTheNextTile, QueueKeys, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    [TestCase]
    public void CtrlShift_QueuesATakeOfAnItemThatCanOnlyBeTaken()
    {
        var action = Decide(QueueState(Item(28, 27)), NearTheNextTile, QueueKeys, MapButton.Left);

        AssertThat(action.Cursor).IsEqual(MouseCursor.Take);
        AssertThat(action.QueuedAction.GetValueOrDefault()).IsEqual((OriginX + 28, OriginY + 27, ClientCommand.PickUp));
        AssertThat(action.Command).IsNull();
    }

    [TestCase]
    public void CtrlShift_WithAHeldItemDoesNotQueueATake()
    {
        var action = Decide(QueueState(HeldItem(Candle), Item(27, 27)), OnPlayerTile, QueueKeys, MapButton.Left);

        AssertThat(action.QueuedAction).IsNull();
    }

    [TestCase]
    public void CtrlShift_DoesNotQueueItemsThatCannotBeSeen()
    {
        var hidden = Decide(
            QueueState(Item(28, 27, MapFlags.IsUsable | MapFlags.Invisible)),
            NearTheNextTile,
            QueueKeys,
            MapButton.Left);
        var hiddenTake = Decide(
            QueueState(Item(28, 27, MapFlags.Invisible)),
            NearTheNextTile,
            QueueKeys,
            MapButton.Left);

        AssertThat(hidden.QueuedAction).IsNull();
        AssertThat(hiddenTake.QueuedAction).IsNull();
    }

    [TestCase]
    public void CtrlShift_WithAHeldItemShowsTheDropCursorOverEmptyFloor()
    {
        var action = Decide(QueueState(HeldItem(Candle)), OnPlayerTile, QueueKeys, MapButton.Left);

        AssertThat(action)
            .IsEqual(new MapAction(Tile(27, 27), MapLayer.Floor, MouseCursor.Drop, null, null));
    }

    [TestCase]
    public void Shift_WithNothingNearDoesNothing()
    {
        AssertThat(Decide(State(), OnPlayerTile, MapKeys.Shift, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    [TestCase]
    public void Ctrl_HighlightsTheNearestCharacterWithTheHitCursor()
    {
        var action = Decide(State(Character(28, 27)), NearTheNextTile, MapKeys.Ctrl, MapButton.None);

        AssertThat(action).IsEqual(new MapAction(Tile(28, 27), MapLayer.Character, MouseCursor.Hit, null, null));
    }

    [TestCase]
    public void Ctrl_AttacksOnLeftAndLooksOnRight()
    {
        var state = State(Character(28, 27));

        AssertThat(Decide(state, NearTheNextTile, MapKeys.Ctrl, MapButton.Left).Command)
            .IsEqual(MapCommandPackets.ForCharacter(ClientCommand.Attack, GuardNumber));
        AssertThat(Decide(state, NearTheNextTile, MapKeys.Ctrl, MapButton.Right).Command)
            .IsEqual(MapCommandPackets.ForCharacter(ClientCommand.Look, GuardNumber));
    }

    [TestCase]
    public void Ctrl_GivesTheHeldItemWithTheGiveCursor()
    {
        var state = State(HeldItem(Candle), Character(28, 27));

        var action = Decide(state, NearTheNextTile, MapKeys.Ctrl, MapButton.Left);

        AssertThat(action.Cursor).IsEqual(MouseCursor.Give);
        AssertThat(action.Command).IsEqual(MapCommandPackets.ForCharacter(ClientCommand.Give, GuardNumber));
    }

    [TestCase]
    public void Ctrl_WithNobodyNearDoesNothing()
    {
        AssertThat(Decide(State(), OnPlayerTile, MapKeys.Ctrl, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    [TestCase]
    public void Alt_HighlightsTheNearestCharacterWithTheSelectCursor()
    {
        var action = Decide(State(Character(28, 27)), NearTheNextTile, MapKeys.Alt, MapButton.None);

        AssertThat(action).IsEqual(new MapAction(Tile(28, 27), MapLayer.Character, MouseCursor.Select, null, null));
    }

    [TestCase]
    public void Alt_SelectsOnLeftThenDeselectsAndLooksOnRight()
    {
        var state = State(Character(28, 27));

        AssertThat(Decide(state, NearTheNextTile, MapKeys.Alt, MapButton.Left).NewSelection).IsEqual(GuardNumber);
        AssertThat(Decide(state, NearTheNextTile, MapKeys.Alt, MapButton.Left, GuardNumber).NewSelection).IsEqual(0);
        AssertThat(Decide(state, NearTheNextTile, MapKeys.Alt, MapButton.Right).Command)
            .IsEqual(MapCommandPackets.ForCharacter(ClientCommand.Look, GuardNumber));
    }

    [TestCase]
    public void Alt_ClickingWithNobodyNearClearsTheSelection()
    {
        AssertThat(Decide(State(), OnPlayerTile, MapKeys.Alt, MapButton.Left, GuardNumber).NewSelection).IsEqual(0);
    }

    [TestCase(MapKeys.Shift | MapKeys.Ctrl)]
    [TestCase(MapKeys.Ctrl | MapKeys.Alt)]
    [TestCase(MapKeys.Shift | MapKeys.Ctrl | MapKeys.Alt)]
    public void OtherKeyCombinations_DoNothingYet(MapKeys keys)
    {
        var state = State(Item(27, 27), Character(27, 27));

        AssertThat(Decide(state, OnPlayerTile, keys, MapButton.Left)).IsEqual(MapAction.Nothing);
    }

    private static int Tile(int column, int row) => column + row * WorldMap.Size;

    private GameState GiveMoreState(params TestCommand[] commands)
    {
        giveMore = true;
        return State(commands);
    }

    private GameState QueueState(params TestCommand[] commands)
    {
        useQueue = true;
        return State(commands);
    }

    private static TestCommand Item(int column, int row, MapFlags extraFlags = MapFlags.None) =>
        Place(column, row, MapFlags.IsItem | extraFlags, Candle, 0, 0);

    private static TestCommand Character(int column, int row) =>
        Place(column, row, MapFlags.IsCharacter, 0, Guard, GuardNumber);

    private static TestCommand Place(int column, int row, MapFlags flags, int item, int character, int number) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsObjectAndCharacter)
            .UInt16((ushort)Tile(column, row))
            .UInt32((uint)flags)
            .UInt16((ushort)item)
            .UInt16((ushort)character)
            .Bytes(0, 0)
            .UInt16((ushort)number)
            .UInt16(1)
            .UInt16(100);

    private static TestCommand HeldItem(int sprite) =>
        TestCommand.Of(ServerCommand.SetCharacterHeldItem).UInt16((ushort)sprite).UInt16(0).Byte(1);

    private static GameState State(params TestCommand[] commands)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
            [
                TestCommand.Of(ServerCommand.SetOrigin).UInt16(OriginX).UInt16(OriginY),
                .. commands,
            ]));
        return state;
    }

    private MapAction Decide(
        GameState state,
        (float Column, float Row)? point,
        MapKeys keys,
        MapButton button,
        int selectedCharacter = 0)
    {
        var session = new GameSession(state);
        session.GiveMore.Enabled = giveMore;
        session.UseQueue.Enabled = useQueue;
        return MapActions.Decide(state, session, point, keys, button, selectedCharacter);
    }
}
