using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class SpectatingTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsObjectAndCharacter = 0b0110_1010;
    private const int ChunkLength = 15;
    private const int Own = 11;
    private const int Target = 22;
    private const string StartOnBob = "Now spectating Bob. Use /spectate self to return.";
    private readonly TickProcessor processor;
    private readonly GameState state = new();
    private int started;
    private int stopped;

    public SpectatingTests()
    {
        processor = new TickProcessor(state);
        state.SpectatingStarted += () => started++;
        state.SpectatingStopped += () => stopped++;
        Process(Centre(Own));
    }

    [Fact]
    public void OrdinaryLines_StartNothing()
    {
        Process(Lines("Bob says: \"Now spectating nothing\""));

        Assert.False(state.Spectating);
        Assert.Equal((0, 0), (started, stopped));
    }

    [Fact]
    public void TheStartMessage_StartsSpectating()
    {
        Process(Lines(StartOnBob));

        Assert.True(state.Spectating);
        Assert.Equal((1, 0), (started, stopped));
    }

    [Fact]
    public void TheViewMovingToTheTarget_KeepsSpectating()
    {
        Process(Lines(StartOnBob));

        Process(Centre(Target));

        Assert.True(state.Spectating);
        Assert.Equal((1, 0), (started, stopped));
    }

    [Fact]
    public void TheStopMessage_StopsSpectating()
    {
        Process(Lines(StartOnBob));
        Process(Centre(Target));

        Process(Lines("No longer spectating."));

        Assert.False(state.Spectating);
        Assert.Equal((1, 1), (started, stopped));
    }

    [Fact]
    public void TheViewComingBackToTheOwnCharacter_StopsSpectating()
    {
        Process(Lines(StartOnBob));
        Process(Centre(Target));

        Process(Centre(Own));

        Assert.False(state.Spectating);
        Assert.Equal((1, 1), (started, stopped));
    }

    private static TestCommand Centre(int characterNumber) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsObjectAndCharacter)
            .UInt16(WorldMap.PlayerTile)
            .UInt32((uint)MapFlags.IsCharacter)
            .UInt16(0)
            .UInt16(3000)
            .Bytes(0, 0)
            .UInt16((ushort)characterNumber)
            .UInt16(1)
            .UInt16(100);

    // Each line goes to the client in pieces of fifteen letters, the last ending the line.
    private static TestCommand[] Lines(params string[] lines) =>
        lines.SelectMany(line => (line + "\n").Chunk(ChunkLength)
                .Select(piece => TestCommand.Of(ServerCommand.Log1).Text(new string(piece), ChunkLength)))
            .ToArray();

    private void Process(params TestCommand[] commands) => processor.Process(TestCommand.Payload(commands));
}
