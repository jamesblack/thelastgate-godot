using GdUnit4;
using TheLastGate.Game;
using TheLastGate.Net;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class CharacterFramesTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsAndCharacter = 0b1110_0010;
    private const byte CharacterLookOnly = 0b0010_0000;
    private const int TileIndex = 1000;
    private const int Sprite = 2000;
    private const int SpecialIdleSprite = 22480;
    private const int OneStepEveryTick = 100;
    private const int Attack = 0;
    private const int Pickup = 1;
    private const int Cast = 9;
    private const int UnknownAction = 10;
    private const int ActionPastTheTable = 11;
    private const int PlayerTextColour = 5;

    [TestCase]
    public void Idle_ShowsTheFrameForItsDirection()
    {
        var tile = Run(Character(1), 50);

        AssertThat(tile.Sprite).IsEqual(Sprite + 8);
        AssertThat(tile.Offset).IsEqual((0, 0));
    }

    [TestCase]
    public void Idle_CyclesTheIdleFramesOfTheSpecialIdleSprite()
    {
        AssertThat(Run(Character(1, SpecialIdleSprite), 50).Sprite).IsEqual(SpecialIdleSprite + 8 + 1);
        AssertThat(Run(Character(1, SpecialIdleSprite), 50, 7).Sprite).IsEqual(SpecialIdleSprite + 8);
    }

    [TestCase]
    public void Idle_FacingUpCyclesEvenWhileStunnedLikeTheCurrentClient()
    {
        var facingUp = Run(Character(0, SpecialIdleSprite, flags: MapFlags.Stunned), 50);
        var facingDown = Run(Character(1, SpecialIdleSprite, flags: MapFlags.Stunned), 50);

        AssertThat(facingUp.Sprite).IsEqual(SpecialIdleSprite + 1);
        AssertThat(facingDown.Sprite).IsEqual(SpecialIdleSprite + 8);
    }

    [TestCase(19, 67, -6, 3)]
    [TestCase(27, 75, 6, -3)]
    [TestCase(35, 83, -6, -3)]
    [TestCase(43, 91, 6, 3)]
    public void Walk_ShowsTheFrameAndHowFarAlongTheTileItIs(int status, int frame, int offsetX, int offsetY)
    {
        var tile = Run(Character(status), 50);

        AssertThat(tile.Sprite).IsEqual(Sprite + frame);
        AssertThat(tile.Offset).IsEqual((offsetX, offsetY));
    }

    [TestCase]
    public void Walk_MovesOnAFrameEachTickItsSpeedAllows()
    {
        var tile = Run(Character(19), 50, 1);

        AssertThat(tile.Sprite).IsEqual(Sprite + 68);
        AssertThat(tile.Offset).IsEqual((-8, 4));
    }

    [TestCase]
    public void Walk_StartsTheStepAgainAfterItsLastFrame()
    {
        AssertThat(Run(Character(23), 50, 1).Sprite).IsEqual(Sprite + 64);
    }

    [TestCase(54, 100, -16, 0)]
    [TestCase(66, 108, 0, -8)]
    [TestCase(78, 116, 0, 8)]
    [TestCase(90, 124, 16, 0)]
    public void DiagonalWalk_SpreadsEightFramesOverTwelveStepsAndMovesOneAxis(
        int status,
        int frame,
        int offsetX,
        int offsetY)
    {
        var tile = Run(Character(status), 50);

        AssertThat(tile.Sprite).IsEqual(Sprite + frame);
        AssertThat(tile.Offset).IsEqual((offsetX, offsetY));
    }

    [TestCase(96, 128)]
    [TestCase(101, 133)]
    [TestCase(159, 191)]
    public void Turn_ShowsItsFrameWithoutMoving(int status, int frame)
    {
        var tile = Run(Character(status), 50);

        AssertThat(tile.Sprite).IsEqual(Sprite + frame);
        AssertThat(tile.Offset).IsEqual((0, 0));
    }

    [TestCase(Attack, 195)]
    [TestCase(Pickup, 227)]
    [TestCase(Cast, 419)]
    [TestCase(UnknownAction, 323)]
    public void Action_ShowsTheRowOfFramesForWhatTheCharacterDoes(int action, int frame)
    {
        AssertThat(Run(Character(163, action: action), 50).Sprite).IsEqual(Sprite + frame);
    }

    [TestCase]
    public void Action_MovesOnAtTheActionsSpeed()
    {
        var tile = Run(Character(163, action: Attack, speed: 250, attackSpeed: 150), 50, 1);

        AssertThat(tile.Sprite).IsEqual(Sprite + 196);
    }

    [TestCase]
    public void Action_CanSpillIntoTheNextDirectionLikeTheCurrentClient()
    {
        var (tile, reports) = RunWithReports(
            Character(167, action: Cast, speed: 50, castSpeed: 50, textColour: PlayerTextColour),
            1,
            1);

        AssertThat(tile.Sprite).IsEqual(Sprite + 169 + 32 + (7 << 5));
        AssertThat(reports).HasSize(1);
    }

    [TestCase]
    public void Action_PastTheEndOfTheCurrentClientsTableUsesTheFirstRowAndIsReported()
    {
        var (tile, reports) = RunWithReports(Character(163, action: ActionPastTheTable), 50);

        AssertThat(tile.Sprite).IsEqual(Sprite + 195);
        AssertThat(reports).HasSize(1);
    }

    [TestCase]
    public void Animation_ReportsNothingForOrdinaryStatuses()
    {
        var (_, reports) = RunWithReports(Character(19), 50, 10);

        AssertThat(reports).IsEmpty();
    }

    [TestCase]
    public void Stunned_HoldsTheCurrentFrame()
    {
        AssertThat(Run(Character(19, flags: MapFlags.Stunned), 50, 2).Sprite).IsEqual(Sprite + 67);
    }

    [TestCase]
    public void UnknownStatus_ShowsTheFirstFrameAndKeepsTheLastOffset()
    {
        var (tile, reports) = RunWithReports(
            Character(19),
            50,
            followedBy: TestCommand.Of(AbsoluteSetMap)
                .Byte(CharacterLookOnly)
                .UInt16(TileIndex)
                .UInt16(Sprite)
                .Bytes(10, 0));

        AssertThat(tile.Sprite).IsEqual(Sprite);
        AssertThat(tile.Offset).IsEqual((-6, 3));
        AssertThat(reports).HasSize(1);
    }

    [TestCase]
    public void NoCharacter_ShowsNoCharacterSprite()
    {
        AssertThat(Run(Character(1, 0), 50).Sprite).IsEqual(0);
    }

    private static TestCommand Character(
        int status,
        int sprite = Sprite,
        int action = Attack,
        MapFlags flags = MapFlags.None,
        int speed = OneStepEveryTick,
        int attackSpeed = 0,
        int castSpeed = 0,
        int textColour = 0) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsAndCharacter)
            .UInt16(TileIndex)
            .UInt32((uint)flags)
            .UInt16((ushort)sprite)
            .Bytes((byte)status, (byte)action)
            .UInt16(77)
            .UInt16(88)
            .UInt16((ushort)speed)
            .Byte(100)
            .UInt16((ushort)castSpeed)
            .UInt16((ushort)attackSpeed)
            .UInt16(0)
            .Byte((byte)textColour);

    private static TestCommand Tick(int serverTick) => TestCommand.Of(ServerCommand.Tick).Byte((byte)serverTick);

    private static Drawn Run(TestCommand character, int serverTick, int moreTicks = 0) =>
        RunWithReports(character, serverTick, moreTicks).Tile;

    private static (Drawn Tile, List<string> Reports) RunWithReports(
        TestCommand character,
        int serverTick,
        int moreTicks = 0,
        TestCommand? followedBy = null)
    {
        var state = new GameState();
        var session = new GameSession(state);
        var reports = new List<string>();
        state.UnusualDataSeen += reports.Add;
        session.MapFrames.UnusualDataSeen += reports.Add;
        var processor = new TickProcessor(state);
        processor.Process(TestCommand.Payload(character, Tick(serverTick)));
        for (var tick = 0; tick < moreTicks; tick++)
        {
            processor.Process([]);
        }

        if (followedBy != null)
        {
            processor.Process(TestCommand.Payload(followedBy));
        }

        return (new Drawn(session.MapFrames.CharacterSprite(TileIndex), session.MapFrames.CharacterOffset(TileIndex)),
            reports);
    }

    private readonly record struct Drawn(int Sprite, (int X, int Y) Offset);
}
