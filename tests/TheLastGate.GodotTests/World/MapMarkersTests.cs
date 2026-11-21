using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class MapMarkersTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte CharacterLookAndIdentity = 0b0110_0000;
    private const int OriginX = 500;
    private const int OriginY = 400;
    private const int Column = 30;
    private const int Row = 20;
    private const int Number = 552;
    private const int Id = 77;
    private const int Drop = 1;
    private const int PickUp = 2;
    private const int Give = 3;
    private const int Use = 4;
    private const int Wave = 6;

    [TestCase]
    public void IsWalkTarget_ForTheTileThePlayerIsWalkingTo()
    {
        var tile = Tile();

        AssertThat(MapMarkers.IsWalkTarget(tile, new PlayerTarget(0, OriginX + Column, OriginY + Row, 0, 0, 0)))
            .IsTrue();
        AssertThat(MapMarkers.IsWalkTarget(tile, new PlayerTarget(0, OriginX + Column + 1, OriginY + Row, 0, 0, 0)))
            .IsFalse();
    }

    [TestCase]
    public void IsAttackTarget_ForTheCharacterThePlayerAttacks()
    {
        var tile = Tile();

        AssertThat(MapMarkers.IsAttackTarget(tile, new PlayerTarget(Number, 0, 0, 0, 0, 0))).IsTrue();
        AssertThat(MapMarkers.IsAttackTarget(tile, new PlayerTarget(Number + 1, 0, 0, 0, 0, 0))).IsFalse();
    }

    [TestCase]
    public void IsAttackTarget_NeverWhileThePlayerIsNotAttacking()
    {
        AssertThat(MapMarkers.IsAttackTarget(Tile(0), new PlayerTarget(0, 0, 0, 0, 0, 0))).IsFalse();
    }

    [TestCase]
    public void IsGiveTarget_ForTheCharacterTheGiveIsAimedAtById()
    {
        var tile = Tile();

        AssertThat(MapMarkers.IsGiveTarget(tile, new PlayerTarget(0, 0, 0, Give, Id, 0))).IsTrue();
        AssertThat(MapMarkers.IsGiveTarget(tile, new PlayerTarget(0, 0, 0, Give, Number, 0))).IsFalse();
        AssertThat(MapMarkers.IsGiveTarget(tile, new PlayerTarget(0, 0, 0, Use, Id, 0))).IsFalse();
    }

    [TestCase(Drop, 32)]
    [TestCase(PickUp, 33)]
    [TestCase(Use, 45)]
    [TestCase(Give, 0)]
    [TestCase(Wave, 0)]
    public void ActionMarker_MarksTheTileOfADropPickUpOrUse(int action, int sprite)
    {
        AssertThat(MapMarkers.ActionMarker(Tile(), new PlayerTarget(0, 0, 0, action, OriginX + Column, OriginY + Row)))
            .IsEqual(sprite);
    }

    [TestCase]
    public void ActionMarker_NotOnOtherTiles()
    {
        AssertThat(
                MapMarkers.ActionMarker(Tile(), new PlayerTarget(0, 0, 0, Drop, OriginX + Column, OriginY + Row + 1)))
            .IsEqual(0);
    }

    private static MapTile Tile(int number = Number)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                TestCommand.Of(ServerCommand.SetOrigin).UInt16(OriginX).UInt16(OriginY),
                TestCommand.Of(AbsoluteSetMap)
                    .Byte(CharacterLookAndIdentity)
                    .UInt16(Column + Row * WorldMap.Size)
                    .UInt16(3000)
                    .Bytes(0, 0)
                    .UInt16((ushort)number)
                    .UInt16(Id)
                    .UInt16(100)));
        return state.Map.Tiles[Column + Row * WorldMap.Size];
    }
}
