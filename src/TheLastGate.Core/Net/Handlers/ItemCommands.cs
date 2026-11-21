using System;
using System.Diagnostics;
using TheLastGate.Buffs;
using TheLastGate.Items;
using TheLastGate.State;

namespace TheLastGate.Net.Handlers;

internal static class ItemCommands
{
    public static void Apply(PlayerState player, ItemLooks itemLooks, BuffWatch buffs, ReadOnlySpan<byte> command)
    {
        var reader = new PacketReader(command);
        switch ((ServerCommand)reader.ReadByte())
        {
            case ServerCommand.SetCharacterItem:
            {
                var slot = reader.ReadUInt32();
                var item = new ItemSlot(reader.ReadInt16(), reader.ReadInt16(), reader.ReadByte(), reader.ReadByte());
                if (Store(player.Inventory, slot, item))
                {
                    itemLooks.SlotChanged(ItemSlotKind.Inventory, (int)slot, item.Sprite);
                }

                break;
            }
            case ServerCommand.SetCharacterWorn:
            {
                var slot = reader.ReadUInt32();
                var item = new ItemSlot(reader.ReadInt16(), reader.ReadInt16(), reader.ReadByte(), 0);
                if (Store(player.Worn, slot, item))
                {
                    itemLooks.SlotChanged(ItemSlotKind.Worn, (int)slot, item.Sprite);
                }

                break;
            }
            case ServerCommand.SetCharacterSpell:
            {
                var slot = reader.ReadUInt32();
                var sprite = reader.ReadInt16();
                // The current client keeps this in a signed char, so only the low byte counts.
                var active = (sbyte)reader.ReadInt16();
                var updated = new BuffSlot(sprite, active);
                if (slot < player.Buffs.Length)
                {
                    var old = player.Buffs[slot];
                    player.Buffs[slot] = updated;
                    buffs.SpellPacket((int)slot, old, updated);
                }

                break;
            }
            case ServerCommand.SetCharacterHeldItem:
                player.HeldItem = new ItemSlot(reader.ReadInt16(), reader.ReadInt16(), reader.ReadByte(), 0);
                itemLooks.HeldItemChanged(player.HeldItem.Sprite);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static bool Store<TSlot>(TSlot[] slots, uint slot, TSlot value)
    {
        if (slot >= slots.Length)
        {
            return false;
        }

        slots[slot] = value;
        return true;
    }
}
