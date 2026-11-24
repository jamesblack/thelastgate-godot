using TheLastGate.Cursors;
using TheLastGate.Net;
using TheLastGate.World;

namespace TheLastGate.Windows;

public static class ShopClicks
{
    // A grave's 40 carried, 20 worn, held item and gold; a merchant fills the first 40.
    public const int Slots = 62;

    public static byte[]? Packet(MapButton button, MapKeys keys, int shopNumber, int slot) => button switch
    {
        MapButton.Left when (keys & (MapKeys.Ctrl | MapKeys.Alt)) != 0 => ShopPackets.BuyTen(shopNumber, slot),
        MapButton.Left => ShopPackets.Buy(shopNumber, slot),
        MapButton.Right => ShopPackets.Describe(shopNumber, slot),
        _ => null,
    };

    public static MouseCursor Cursor(bool slotHasItem, bool holdingItem) => (slotHasItem, holdingItem) switch
    {
        (true, _) => MouseCursor.Take,
        (false, true) => MouseCursor.Drop,
        _ => MouseCursor.None,
    };
}
