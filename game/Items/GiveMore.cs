using System;
using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Items;

// A Ctrl+Shift give remembers the item given. The next time the hand is empty, the first inventory item with the same
// sprite and properties is picked up, so the next one is ready to give; either way the gift is then forgotten.
public sealed class GiveMore
{
    private bool enabled;
    private ItemSlot? given;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!value)
            {
                given = null;
            }
        }
    }

    public Action<byte[]> Send { get; set; } = _ => { };

    public void Gave(ItemSlot item)
    {
        if (Enabled)
        {
            given = item;
        }
    }

    public void HeldItemChanged(PlayerState player)
    {
        if (player.HeldItem.Sprite != 0 || given is not { } item)
        {
            return;
        }

        given = null;
        var slot = Array.FindIndex(
            player.Inventory,
            carried => carried.Sprite == item.Sprite && carried.Properties == item.Properties);
        if (slot >= 0)
        {
            Send(InventoryPackets.For(InventoryAction.SwapWithInventorySlot, slot, 0));
        }
    }
}
