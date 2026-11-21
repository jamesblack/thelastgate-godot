using System;
using TheLastGate.State;

namespace TheLastGate.Net;

public static class MapIntents
{
    public static bool HoldsBuildItem(PlayerState player) => throw new NotImplementedException();

    public static byte[] Walk(in MapTile tile) => throw new NotImplementedException();

    public static byte[] Turn(in MapTile tile) => throw new NotImplementedException();

    public static byte[] PickUp(in MapTile tile) => throw new NotImplementedException();

    public static byte[] Drop(in MapTile tile) => throw new NotImplementedException();

    public static byte[] TakeOrUse(in MapTile tile) => throw new NotImplementedException();

    public static byte[] LookAtItem(in MapTile tile) => throw new NotImplementedException();

    public static byte[] AttackOrGive(PlayerState player, int characterNumber) =>
        throw new NotImplementedException();

    public static byte[] Give(int characterNumber) => throw new NotImplementedException();

    public static byte[] LookAt(int characterNumber) => throw new NotImplementedException();
}
