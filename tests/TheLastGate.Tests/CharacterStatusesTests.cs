using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class CharacterStatusesTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsAndCharacter = 0b1110_0010;
    private const int TileIndex = 1000;
    private const int Sprite = 2000;
    private const int OneStepEveryTick = 100;
    private const int Attack = 0;
    private const int Cast = 9;
    private const int PlayerTextColour = 5;

    [Theory]
    [InlineData(1, CharacterStatusKind.Idle, 0, 8, 1)]
    [InlineData(19, CharacterStatusKind.StraightWalk, 16, 8, 0)]
    [InlineData(27, CharacterStatusKind.StraightWalk, 24, 8, 1)]
    [InlineData(54, CharacterStatusKind.DiagonalWalk, 48, 12, 0)]
    [InlineData(66, CharacterStatusKind.DiagonalWalk, 60, 12, 1)]
    [InlineData(101, CharacterStatusKind.Turn, 100, 4, 1)]
    [InlineData(163, CharacterStatusKind.Action, 160, 8, 0)]
    [InlineData(170, CharacterStatusKind.Action, 168, 8, 1)]
    public void BlockOf_FindsTheServersBlockForAStatus(
        int status,
        CharacterStatusKind kind,
        int firstStatus,
        int length,
        int direction)
    {
        Assert.Equal(new CharacterStatusBlock(kind, firstStatus, length, direction), CharacterStatuses.BlockOf(status));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(200)]
    public void BlockOf_KnowsNothingOfOtherStatuses(int status)
    {
        Assert.Equal(CharacterStatusKind.Unknown, CharacterStatuses.BlockOf(status).Kind);
    }

    [Fact]
    public void Idle_StaysPut()
    {
        Assert.Equal(1, Run(Character(1), 50, 3).Status);
    }

    [Fact]
    public void Walk_StepsAsFastAsItsSpeedAllows()
    {
        Assert.Equal(20, Run(Character(19), 50).Status);
        Assert.Equal(21, Run(Character(19), 50, 1).Status);
    }

    [Fact]
    public void Walk_StartsItsBlockAgainAfterTheLastStatus()
    {
        Assert.Equal(16, Run(Character(23), 50).Status);
    }

    [Fact]
    public void Stunned_HoldsTheStatus()
    {
        Assert.Equal(19, Run(Character(19, MapFlags.Stunned), 50, 2).Status);
    }

    [Fact]
    public void Action_StepsAtTheActionsSpeed()
    {
        Assert.Equal(164, Run(Character(163, speed: 250, attackSpeed: 150), 50).Status);
    }

    [Fact]
    public void Action_CanSpillIntoTheNextDirectionAndIsReported()
    {
        var (status, reports) = Run(
            Character(167, action: Cast, speed: 50, castSpeed: 50, textColour: PlayerTextColour),
            1);

        Assert.Equal(169, status);
        Assert.Single(reports);
    }

    [Fact]
    public void AnUnknownStatus_StaysAndIsReported()
    {
        var (status, reports) = Run(Character(10), 50);

        Assert.Equal(10, status);
        Assert.Single(reports);
    }

    [Fact]
    public void OrdinaryStatuses_ReportNothing()
    {
        Assert.Empty(Run(Character(19), 50, 10).Reports);
    }

    private static TestCommand Character(
        int status,
        MapFlags flags = MapFlags.None,
        int action = Attack,
        int speed = OneStepEveryTick,
        int attackSpeed = 0,
        int castSpeed = 0,
        int textColour = 0) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsAndCharacter)
            .UInt16(TileIndex)
            .UInt32((uint)flags)
            .UInt16(Sprite)
            .Bytes((byte)status, (byte)action)
            .UInt16(77)
            .UInt16(88)
            .UInt16((ushort)speed)
            .Byte(100)
            .UInt16((ushort)castSpeed)
            .UInt16((ushort)attackSpeed)
            .UInt16(0)
            .Byte((byte)textColour);

    private static (int Status, List<string> Reports) Run(TestCommand character, int serverTick, int moreTicks = 0)
    {
        var state = new GameState();
        var reports = new List<string>();
        state.UnusualDataSeen += reports.Add;
        var processor = new TickProcessor(state);
        processor.Process(
            TestCommand.Payload(character, TestCommand.Of(ServerCommand.Tick).Byte((byte)serverTick)));
        for (var tick = 0; tick < moreTicks; tick++)
        {
            processor.Process([]);
        }

        return (state.Map.Tiles[TileIndex].CharacterStatus, reports);
    }
}
