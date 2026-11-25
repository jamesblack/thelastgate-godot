using System;
using System.Collections.Generic;
using System.Text;

namespace TheLastGate.Net;

public static class ChatPackets
{
    private const int ChunkLength = 15;

    private static readonly ClientCommand[] Chunks =
    [
        ClientCommand.Input1, ClientCommand.Input2, ClientCommand.Input3, ClientCommand.Input4,
        ClientCommand.Input5, ClientCommand.Input6, ClientCommand.Input7, ClientCommand.Input8,
    ];

    // The server ends its input buffer with a NUL of its own, in the last byte the chunks fill.
    public static readonly int MaxLength = Chunks.Length * ChunkLength - 1;

    // The server keeps its input buffer between lines and reads it up to the first NUL once the last chunk comes, so
    // only the chunks up to the one holding the line's NUL are needed, then the last chunk to have it said.
    public static IReadOnlyList<byte[]> Say(string text)
    {
        var letters = Encoding.Latin1.GetBytes(text.Length > MaxLength ? text[..MaxLength] : text);
        var chunksWithText = Math.Min(letters.Length / ChunkLength + 1, Chunks.Length);
        var packets = new List<byte[]>();
        for (var chunk = 0; chunk < chunksWithText; chunk++)
        {
            packets.Add(Chunk(chunk, letters));
        }

        if (chunksWithText < Chunks.Length)
        {
            packets.Add(Chunk(Chunks.Length - 1, letters));
        }

        return packets;
    }

    private static byte[] Chunk(int chunk, byte[] letters)
    {
        var packet = new byte[Protocol.PacketSize];
        packet[0] = (byte)Chunks[chunk];
        var start = chunk * ChunkLength;
        if (start < letters.Length)
        {
            letters.AsSpan(start, Math.Min(ChunkLength, letters.Length - start)).CopyTo(packet.AsSpan(1));
        }

        return packet;
    }
}
