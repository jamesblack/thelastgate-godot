using System;
using TheLastGate.Net;

namespace TheLastGate.State;

public sealed class NameRequests
{
    public const int TicksBetweenRequests = 24;

    private int pendingId;
    private int pendingNumber;
    private int ticksSinceRequest;

    public Action<byte[]> Send { get; set; } = _ => { };

    public void NoteUnknown(int characterNumber, int characterId) =>
        (pendingNumber, pendingId) = (characterNumber, characterId);

    public byte[]? Tick(CharacterNames names)
    {
        ticksSinceRequest++;
        if (pendingNumber == 0 || ticksSinceRequest <= TicksBetweenRequests)
        {
            return null;
        }

        var request = names.NameOf(pendingNumber, pendingId).Length == 0
            ? MapCommandPackets.ForCharacter(ClientCommand.AutoLook, pendingNumber)
            : null;
        (pendingNumber, pendingId, ticksSinceRequest) = (0, 0, 0);
        return request;
    }
}
