using System;
using TheLastGate.Net;

namespace TheLastGate.State;

// While a shop is open its contents are asked for again every few ticks, so prices and stock stay current.
public sealed class ShopRefresh
{
    private const int RefreshAfterTicks = 8;

    private int? expectedNumber;
    private int ticks;

    public Action<byte[]> Send { get; set; } = _ => { };

    // Until the asked-for shop opens, refreshing the open one would bring it back over the new one.
    public void Expect(int shopNumber) => expectedNumber = shopNumber;

    public void StopExpecting() => expectedNumber = null;

    public void EndOfTick(ShopKind? openShop, int shopNumber)
    {
        if (openShop == null || shopNumber == expectedNumber)
        {
            expectedNumber = null;
        }

        ticks++;
        if (openShop != null && ticks > RefreshAfterTicks)
        {
            Send(ClientPackets.OneValue(ClientCommand.Look, expectedNumber ?? shopNumber));
            ticks = 0;
        }
    }
}
