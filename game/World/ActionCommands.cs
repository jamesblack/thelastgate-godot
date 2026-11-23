using TheLastGate.Net;

namespace TheLastGate.World;

// The commands the current client sends through its cmd(): those aimed at a tile, a shop or the smith, the fighting
// mode, Escape's reset and stat raises.
public static class ActionCommands
{
    public static bool CancelsPendingActions(byte[] packet) =>
        (ClientCommand)packet[0] is ClientCommand.Move or ClientCommand.Turn or ClientCommand.PickUp
        or ClientCommand.Drop or ClientCommand.Use or ClientCommand.LookItem or ClientCommand.Shop
        or ClientCommand.Smith or ClientCommand.Mode or ClientCommand.Reset or ClientCommand.Stat;
}
