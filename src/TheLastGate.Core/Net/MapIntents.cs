using TheLastGate.State;

namespace TheLastGate.Net;

public static class MapIntents
{
    private const int BuildItem = 46;

    public static bool HoldsBuildItem(PlayerState player) => player.HeldItem.Sprite == BuildItem;

    public static byte[] Walk(in MapTile tile) => ForTile(ClientCommand.Move, tile);

    public static byte[] Turn(in MapTile tile) => ForTile(ClientCommand.Turn, tile);

    public static byte[] PickUp(in MapTile tile) => ForTile(ClientCommand.PickUp, tile);

    public static byte[] Drop(in MapTile tile) => ForTile(ClientCommand.Drop, tile);

    public static byte[] TakeOrUse(in MapTile tile) =>
        ForTile(((MapFlags)tile.Flags).HasFlag(MapFlags.IsUsable) ? ClientCommand.Use : ClientCommand.PickUp, tile);

    public static byte[] LookAtItem(in MapTile tile) => ForTile(ClientCommand.LookItem, tile);

    public static byte[] AttackOrGive(bool holdingItem, int characterNumber) =>
        holdingItem ? Give(characterNumber) : Attack(characterNumber);

    public static byte[] Give(int characterNumber) =>
        MapCommandPackets.ForCharacter(ClientCommand.Give, characterNumber);

    public static byte[] LookAt(int characterNumber) =>
        MapCommandPackets.ForCharacter(ClientCommand.Look, characterNumber);

    private static byte[] Attack(int characterNumber) =>
        MapCommandPackets.ForCharacter(ClientCommand.Attack, characterNumber);

    private static byte[] ForTile(ClientCommand command, in MapTile tile) =>
        MapCommandPackets.ForTile(command, tile.X, tile.Y);
}
