using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class MapIntentsTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsObjectAndCharacter = 0b0110_1010;
    private const int OriginX = 500;
    private const int OriginY = 400;
    private const int Column = 28;
    private const int Row = 27;
    private const int BuildItem = 46;
    private const int Candle = 1500;
    private const int GuardNumber = 812;

    [Fact]
    public void HoldsBuildItem_OnlyForTheBuildItem()
    {
        Assert.True(MapIntents.HoldsBuildItem(State(HeldItem(BuildItem)).Player));
        Assert.False(MapIntents.HoldsBuildItem(State(HeldItem(Candle)).Player));
        Assert.False(MapIntents.HoldsBuildItem(State().Player));
    }

    [Fact]
    public void TileIntents_AimAtTheTilesWorldPosition()
    {
        var tile = TileOf(State(Item()));

        Assert.Equal(ForTile(ClientCommand.Move), MapIntents.Walk(tile));
        Assert.Equal(ForTile(ClientCommand.Turn), MapIntents.Turn(tile));
        Assert.Equal(ForTile(ClientCommand.PickUp), MapIntents.PickUp(tile));
        Assert.Equal(ForTile(ClientCommand.Drop), MapIntents.Drop(tile));
        Assert.Equal(ForTile(ClientCommand.LookItem), MapIntents.LookAtItem(tile));
    }

    [Fact]
    public void TakeOrUse_PicksUpAnItemThatIsNotUsable()
    {
        Assert.Equal(ForTile(ClientCommand.PickUp), MapIntents.TakeOrUse(TileOf(State(Item()))));
    }

    [Fact]
    public void TakeOrUse_UsesAUsableItem()
    {
        Assert.Equal(ForTile(ClientCommand.Use), MapIntents.TakeOrUse(TileOf(State(Item(MapFlags.IsUsable)))));
    }

    [Fact]
    public void AttackOrGive_AttacksWithAnEmptyHand()
    {
        Assert.Equal(
            MapCommandPackets.ForCharacter(ClientCommand.Attack, GuardNumber),
            MapIntents.AttackOrGive(State().Player, GuardNumber));
    }

    [Fact]
    public void AttackOrGive_GivesTheHeldItem()
    {
        Assert.Equal(
            MapCommandPackets.ForCharacter(ClientCommand.Give, GuardNumber),
            MapIntents.AttackOrGive(State(HeldItem(Candle)).Player, GuardNumber));
    }

    [Fact]
    public void CharacterIntents_AimAtTheCharacter()
    {
        Assert.Equal(MapCommandPackets.ForCharacter(ClientCommand.Give, GuardNumber), MapIntents.Give(GuardNumber));
        Assert.Equal(MapCommandPackets.ForCharacter(ClientCommand.Look, GuardNumber), MapIntents.LookAt(GuardNumber));
    }

    private static byte[] ForTile(ClientCommand command) =>
        MapCommandPackets.ForTile(command, OriginX + Column, OriginY + Row);

    private static MapTile TileOf(GameState state) => state.Map.Tiles[Column + Row * WorldMap.Size];

    private static TestCommand Item(MapFlags extraFlags = MapFlags.None) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsObjectAndCharacter)
            .UInt16(Column + Row * WorldMap.Size)
            .UInt32((uint)(MapFlags.IsItem | extraFlags))
            .UInt16(Candle)
            .UInt16(0)
            .Bytes(0, 0)
            .UInt16(0)
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
}
