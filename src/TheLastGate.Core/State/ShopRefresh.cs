using System;
using TheLastGate.Net;

namespace TheLastGate.State;

// While a shop is open its contents are asked for again every few ticks, so prices and stock stay current.
public sealed class ShopRefresh
{
    private const int RefreshAfterTicks = 8;

    private int ticks;

    public Action<byte[]> Send { get; set; } = _ => { };

    public void EndOfTick(ShopKind? openShop, int shopNumber)
    {
        ticks++;
        if (openShop != null && ticks > RefreshAfterTicks)
        {
            Send(ClientPackets.OneValue(ClientCommand.Look, shopNumber));
            ticks = 0;
        }
    }
}
