using System.Text;
using TheLastGate.Characters;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class UserDataUploadTests
{
    private const int TicksBetweenPackets = 5;
    private const int PacketCount = 18;
    private const int ChunkLength = 13;

    private static readonly string LongDescription = new string('d', 150) + "END";

    private readonly TextLog chat = new();

    [Fact]
    public void AnUnchangedCharacter_SendsNothing()
    {
        var record = CharacterRecord.CreateNew();
        record.Changed = false;

        Assert.Null(UserDataUpload.For(record, chat));
    }

    [Fact]
    public void AChangedCharacter_SendsItsNameAndDescription()
    {
        var record = CharacterRecord.CreateNew();
        record.CharacterName = "Ishtar";
        record.Description = "A tall templar.";

        var packets = AllPackets(UserDataUpload.For(record, chat)!);

        Assert.Equal("Ishtar", Text(packets[0]).TrimEnd('\0'));
        Assert.Equal("A tall templar.", Text(packets[6]).TrimEnd('\0') + Text(packets[7]).TrimEnd('\0'));
    }

    [Fact]
    public void APacketGoesEveryFifthTick()
    {
        var upload = new UserDataUpload("Ishtar", "A tall templar.", chat);

        var sentOn = Enumerable.Range(1, 12).Where(_ => upload.Tick() != null).ToArray();

        Assert.Equal([5, 10], sentOn);
    }

    [Fact]
    public void TheNameGoesFirstInThirteenByteChunks()
    {
        var packets = AllPackets(new UserDataUpload("Ishtar", "A tall templar.", chat));

        Assert.Equal(PacketCount, packets.Count);
        Assert.All(packets, packet => Assert.Equal(Protocol.PacketSize, packet.Length));
        Assert.All(packets, packet => Assert.Equal((byte)ClientCommand.SetUser, packet[0]));
        Assert.Equal([0, 0], packets[0][1..3]);
        Assert.Equal("Ishtar", Text(packets[0]).TrimEnd('\0'));
        Assert.Equal([0, 13], packets[1][1..3]);
        Assert.Equal([0, 65], packets[5][1..3]);
    }

    [Fact]
    public void TheDescriptionGoesInTwoHalvesOfSeventyEightBytes()
    {
        var packets = AllPackets(new UserDataUpload("Ishtar", LongDescription, chat));

        Assert.Equal([1, 0], packets[6][1..3]);
        Assert.Equal([1, 65], packets[11][1..3]);
        Assert.Equal([2, 0], packets[12][1..3]);
        Assert.Equal([2, 65], packets[17][1..3]);
        var sentDescription = string.Concat(packets.Skip(6).Select(Text));
        Assert.Equal(LongDescription, sentDescription.TrimEnd('\0'));
    }

    [Fact]
    public void ALongNameIsCutToWhatTheServerTakes()
    {
        var packets = AllPackets(new UserDataUpload(new string('n', 79), "A tall templar.", chat));

        Assert.Equal(new string('n', 6 * ChunkLength), string.Concat(packets.Take(6).Select(Text)));
    }

    [Fact]
    public void TheChatSaysWhenTheTransferStartsAndEnds()
    {
        var upload = new UserDataUpload("Ishtar", "A tall templar.", chat);

        TickUntilSent(upload);
        Assert.Equal([new LogLine("Transfering user data...", (int)LogFont.Yellow)], chat.Lines);
        while (!upload.Done)
        {
            upload.Tick();
        }

        Assert.Equal(new LogLine("Transfer done.", (int)LogFont.Yellow), chat.Lines[^1]);
        Assert.Equal(2, chat.Lines.Count);
    }

    [Fact]
    public void AFinishedUpload_SendsNothingMore()
    {
        var upload = new UserDataUpload("Ishtar", "A tall templar.", chat);
        AllPackets(upload);

        Assert.True(upload.Done);
        Assert.All(Enumerable.Range(0, 20), _ => Assert.Null(upload.Tick()));
    }

    private static string Text(byte[] packet) => Encoding.Latin1.GetString(packet, 3, ChunkLength);

    private static byte[] TickUntilSent(UserDataUpload upload)
    {
        for (var tick = 0; tick < TicksBetweenPackets; tick++)
        {
            if (upload.Tick() is { } packet)
            {
                return packet;
            }
        }

        throw new InvalidOperationException("No packet was sent.");
    }

    private static List<byte[]> AllPackets(UserDataUpload upload)
    {
        var packets = new List<byte[]>();
        while (!upload.Done)
        {
            packets.Add(TickUntilSent(upload));
        }

        return packets;
    }
}
