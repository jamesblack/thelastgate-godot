using System;
using System.Buffers.Binary;

namespace TheLastGate.Net;

public static class MapCommandPackets
{
    public static byte[] ForTile(ClientCommand command, int worldX, int worldY)
    {
        var packet = new byte[Protocol.PacketSize];
        packet[0] = (byte)command;
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(1), (ushort)worldX);
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(3), (uint)worldY);
        return packet;
    }

    public static (int X, int Y) TileOf(byte[] packet) =>
        (BinaryPrimitives.ReadUInt16LittleEndian(packet.AsSpan(1)),
            (int)BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(3)));

    public static byte[] ForCharacter(ClientCommand command, int characterNumber)
    {
        var packet = new byte[Protocol.PacketSize];
        packet[0] = (byte)command;
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(1), (uint)characterNumber);
        return packet;
    }
}
