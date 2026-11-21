using System;
using TheLastGate.Characters;
using TheLastGate.State;

namespace TheLastGate.Net;

// Sends the character's name and description from its file after login: thirteen bytes a command, one command every
// fifth tick. Each command says which text it fills (0 the name, 1 and 2 the two halves of the description) and where
// in it its bytes start; the server names a new character and takes the description once the last one arrives.
public sealed class UserDataUpload
{
    private const int TicksBetweenCommands = 5;
    private const int ChunkLength = 13;
    private const int ChunksPerText = 6;
    private const int TextLength = ChunksPerText * ChunkLength;
    private const int ChunkCount = ChunksPerText * 3;

    // The record's fields, which hold one more byte than the server takes.
    private const int NameField = 80;
    private const int DescriptionField = 160;

    private const string StartMessage = "Transfering user data...";
    private const string DoneMessage = "Transfer done.";

    private readonly TextLog chat;
    private readonly byte[] texts = new byte[TextLength * 3];
    private int nextChunk;
    private int ticksWaited;

    public UserDataUpload(string name, string description, TextLog chat)
    {
        this.chat = chat;
        var nameField = new byte[NameField];
        var descriptionField = new byte[DescriptionField];
        NulTerminatedText.Write(nameField, name);
        NulTerminatedText.Write(descriptionField, description);
        nameField.AsSpan(0, TextLength).CopyTo(texts);
        descriptionField.AsSpan(0, TextLength * 2).CopyTo(texts.AsSpan(TextLength));
    }

    public bool Done => nextChunk >= ChunkCount;

    // Only a character marked changed is sent, as in the current client: a new one, one whose description was edited,
    // or one just picked from the launcher's list.
    public static UserDataUpload? For(CharacterRecord record, TextLog chat) =>
        record.Changed ? new UserDataUpload(record.CharacterName, record.Description, chat) : null;

    // The command to send this tick, if any.
    public byte[]? Tick()
    {
        if (Done || ++ticksWaited < TicksBetweenCommands)
        {
            return null;
        }

        ticksWaited = 0;
        if (nextChunk == 0)
        {
            chat.Add(new LogLine(StartMessage, (int)LogFont.Yellow));
        }

        var packet = new byte[Protocol.PacketSize];
        packet[0] = (byte)ClientCommand.SetUser;
        packet[1] = (byte)(nextChunk / ChunksPerText);
        packet[2] = (byte)(nextChunk % ChunksPerText * ChunkLength);
        texts.AsSpan(nextChunk * ChunkLength, ChunkLength).CopyTo(packet.AsSpan(3));
        nextChunk++;
        if (Done)
        {
            chat.Add(new LogLine(DoneMessage, (int)LogFont.Yellow));
        }

        return packet;
    }
}
