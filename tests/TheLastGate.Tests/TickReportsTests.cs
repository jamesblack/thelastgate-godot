using System.Buffers.Binary;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class TickReportsTests
{
    private readonly TickProcessor processor;
    private readonly List<byte[]> sent = [];
    private readonly GameState state = new();

    public TickReportsTests()
    {
        processor = new TickProcessor(state);
        state.TickReports.Send = sent.Add;
    }

    [Fact]
    public void EachTickProcessed_CountsAsPlayed()
    {
        Play(3);

        Assert.Equal(3u, state.TickReports.TicksPlayed);
    }

    [Fact]
    public void FifteenTicks_ReportNothing()
    {
        Play(15);

        Assert.Empty(sent);
    }

    [Fact]
    public void EverySixteenthTick_ReportsTheCountPlayed()
    {
        Play(33);

        Assert.Equal(2, sent.Count);
        Assert.All(sent, command => Assert.Equal((byte)ClientCommand.TickReport, command[0]));
        Assert.Equal(16u, BinaryPrimitives.ReadUInt32LittleEndian(sent[0].AsSpan(1)));
        Assert.Equal(32u, BinaryPrimitives.ReadUInt32LittleEndian(sent[1].AsSpan(1)));
    }

    private void Play(int ticks)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            processor.Process([]);
        }
    }
}
