using System.Text;
using TheLastGate.Net;

namespace TheLastGate.Tests;

public class ChatPacketsTests
{
    private const int ServerInputLength = 120;
    private const int ChunkLength = 15;

    private static readonly ClientCommand[] Chunks =
    [
        ClientCommand.Input1, ClientCommand.Input2, ClientCommand.Input3, ClientCommand.Input4,
        ClientCommand.Input5, ClientCommand.Input6, ClientCommand.Input7, ClientCommand.Input8,
    ];

    [Fact]
    public void Say_SendsAShortLineAsItsFirstAndLastChunk()
    {
        var packets = ChatPackets.Say("/tavern");

        Assert.Equal([(byte)ClientCommand.Input1, (byte)ClientCommand.Input8], packets.Select(packet => packet[0]));
        Assert.All(packets, packet => Assert.Equal(Protocol.PacketSize, packet.Length));
        Assert.Equal("/tavern", Encoding.ASCII.GetString(packets[0], 1, 7));
        Assert.All(packets[0][8..], value => Assert.Equal(0, value));
        Assert.All(packets[1][1..], value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(14, 2)]
    [InlineData(15, 3)]
    [InlineData(29, 3)]
    [InlineData(30, 4)]
    [InlineData(89, 7)]
    [InlineData(90, 8)]
    [InlineData(119, 8)]
    public void Say_SendsOnlyTheChunksUpToTheLinesEnd(int letters, int packetCount)
    {
        Assert.Equal(packetCount, ChatPackets.Say(new string('a', letters)).Count);
    }

    // The server keeps its input buffer between lines, so a shorter line must not pick up the end of a longer one.
    [Theory]
    [InlineData("")]
    [InlineData("/tavern")]
    [InlineData("abcdefghijklmn")]
    [InlineData("abcdefghijklmno")]
    [InlineData("abcdefghijklmnopqrst")]
    public void Say_IsReadWholeByTheServerAfterALongerLine(string text)
    {
        var input = new byte[ServerInputLength];
        Said(input, ChatPackets.Say(new string('x', 119)));

        Assert.Equal(text, Said(input, ChatPackets.Say(text)));
    }

    [Fact]
    public void Say_CarriesLongTextIntoTheNextChunk()
    {
        var packets = ChatPackets.Say("abcdefghijklmnopqrst");

        Assert.Equal("abcdefghijklmno", Encoding.ASCII.GetString(packets[0], 1, 15));
        Assert.Equal("pqrst", Encoding.ASCII.GetString(packets[1], 1, 5));
        Assert.Equal(0, packets[1][6]);
    }

    [Fact]
    public void MaxLength_IsWhatTheServersInputBufferKeeps()
    {
        Assert.Equal(ServerInputLength - 1, ChatPackets.MaxLength);
    }

    [Fact]
    public void Say_SendsALineOfTheMostLettersInFull()
    {
        var text = string.Concat(Enumerable.Range(0, ChatPackets.MaxLength).Select(index => (char)('a' + index % 26)));

        Assert.Equal(text, Said(new byte[ServerInputLength], ChatPackets.Say(text)));
    }

    [Fact]
    public void Say_NeverSendsLettersPastTheMost()
    {
        var packets = ChatPackets.Say(new string('x', 130));

        Assert.Equal(Chunks, packets.Select(packet => (ClientCommand)packet[0]));
        var sent = packets.SelectMany(packet => packet[1..]).ToArray();
        Assert.All(sent[..ChatPackets.MaxLength], value => Assert.Equal((byte)'x', value));
        Assert.Equal(0, sent[ChatPackets.MaxLength]);
    }

    // Each chunk fills its own 15 bytes of the buffer, and the last one ends it and says what the buffer holds.
    private static string? Said(byte[] input, IReadOnlyList<byte[]> packets)
    {
        string? said = null;
        foreach (var packet in packets)
        {
            var chunk = Array.IndexOf(Chunks, (ClientCommand)packet[0]);
            packet.AsSpan(1, ChunkLength).CopyTo(input.AsSpan(chunk * ChunkLength));
            if (chunk == Chunks.Length - 1)
            {
                input[^1] = 0;
                said = Encoding.Latin1.GetString(input, 0, Array.IndexOf(input, (byte)0));
            }
        }

        return said;
    }
}
