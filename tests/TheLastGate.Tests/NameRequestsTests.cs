using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class NameRequestsTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte FlagsObjectAndCharacter = 0b0110_1010;
    private const int Centre = WorldMap.Size / 2;
    private const int Guard = 552;
    private const int Bishop = 553;
    private const int Id = 41;
    private readonly TickProcessor processor;

    private readonly List<byte[]> sent = [];
    private readonly GameState state = new();

    public NameRequestsTests()
    {
        processor = new TickProcessor(state);
        state.NameRequests.Send = sent.Add;
    }

    [Fact]
    public void AnUnnamedCharacter_IsAskedAboutOnlyAfterTheWait()
    {
        Process(Character(Guard, Centre + 3, Centre));
        Wait(NameRequests.TicksBetweenRequests - 1);

        Assert.Empty(sent);
        Process();
        Assert.Equal([AutoLook(Guard)], sent);
    }

    [Fact]
    public void TheCharacterNearestThePlayer_IsAskedAboutFirst()
    {
        Process(Character(Guard, Centre + 4, Centre), Character(Bishop, Centre, Centre - 1));

        PassTheWait();

        Assert.Equal([AutoLook(Bishop)], sent);
    }

    [Fact]
    public void AnUnansweredRequest_IsAskedAgainAfterAnotherWait()
    {
        Process(Character(Guard, Centre + 3, Centre));
        PassTheWait();
        sent.Clear();

        Wait(NameRequests.TicksBetweenRequests);
        Assert.Empty(sent);
        Process();

        Assert.Equal([AutoLook(Guard)], sent);
    }

    [Fact]
    public void ACharacterArrivingAfterTheWait_IsAskedAboutAtOnce()
    {
        PassTheWait();

        Process(Character(Guard, Centre + 3, Centre));

        Assert.Equal([AutoLook(Guard)], sent);
    }

    [Fact]
    public void ACharacterWhoseNameIsKnown_IsSkipped()
    {
        LearnName(Guard, "Guard");
        Process(Character(Guard, Centre + 1, Centre), Character(Bishop, Centre + 4, Centre));

        PassTheWait();

        Assert.Equal([AutoLook(Bishop)], sent);
    }

    [Fact]
    public void AnInvisibleCharacter_IsSkipped()
    {
        Process(Character(Guard, Centre + 3, Centre, MapFlags.Invisible));

        PassTheWait();

        Assert.Empty(sent);
    }

    private static byte[] AutoLook(int number) => MapCommandPackets.ForCharacter(ClientCommand.AutoLook, number);

    private static TestCommand Character(int number, int column, int row, MapFlags extraFlags = MapFlags.None) =>
        TestCommand.Of(AbsoluteSetMap)
            .Byte(FlagsObjectAndCharacter)
            .UInt16((ushort)(column + row * WorldMap.Size))
            .UInt32((uint)(MapFlags.IsCharacter | extraFlags))
            .UInt16(0)
            .UInt16(3000)
            .Bytes(0, 0)
            .UInt16((ushort)number)
            .UInt16(Id)
            .UInt16(100);

    private void LearnName(int number, string name) =>
        Process(
            TestCommand.Of(ServerCommand.Look1)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .Byte(1),
            TestCommand.Of(ServerCommand.Look2),
            TestCommand.Of(ServerCommand.Look3)
                .UInt16(0)
                .UInt16(0)
                .UInt16(0)
                .UInt16((ushort)number)
                .UInt16(Id),
            TestCommand.Of(ServerCommand.Look4),
            TestCommand.Of(ServerCommand.Look5).Text(name, 15));

    private void PassTheWait() => Wait(NameRequests.TicksBetweenRequests + 1);

    private void Wait(int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            Process();
        }
    }

    private void Process(params TestCommand[] commands) => processor.Process(TestCommand.Payload(commands));
}
