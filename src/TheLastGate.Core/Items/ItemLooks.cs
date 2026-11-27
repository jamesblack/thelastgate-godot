using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using TheLastGate.Net;
using TheLastGate.Profiling;
using TheLastGate.State;

namespace TheLastGate.Items;

public enum ItemSlotKind
{
    Inventory,
    Worn,
}

public sealed record ItemLook(IReadOnlyList<LogLine> Lines, ItemDetails Details);

public sealed record ManualLook(ItemSlotKind Kind, int Index, int Sprite);

// Keeps the look text of every inventory and worn slot, fetched quietly in the background whenever a slot changes,
// so a tooltip can show the moment the mouse rests on an item.
public sealed class ItemLooks
{
    private const int DebounceTicks = 4;
    private const int RefetchTicks = 40;
    private const int TimeoutTicks = 30;
    private const int ManualPauseTicks = 20;
    private const int MaxFailures = 2;
    private const int MaxHeaderless = 64;
    private const int MaxHandoffs = 8;
    private const int ActionOffset = 1;
    private const int GearSlotOffset = ActionOffset + sizeof(int);
    private const int IdentifySkill = 52;
    private const int TicksPerSecond = 20;
    private const int WeaponStaleTicks = 5 * TicksPerSecond;

    private readonly List<(ItemSlotKind Kind, int Index, int Sprite)> arrivals = [];
    private readonly LookCapture capture;
    private readonly TextLog chat;
    private readonly HashSet<int> headerless = [];
    private readonly Slot[] inventory = new Slot[PlayerState.InventorySlots];
    private readonly List<Slot> vacated = [];
    private readonly Slot[] worn = new Slot[PlayerState.WornSlots];
    private Flight? flight;
    private int handoffTick;
    private Slot held = new();
    private int heldSprite;
    private int pauseUntil;
    private (ItemSlotKind Kind, int Index)? preferred;
    private int tick;

    public ItemLooks(TextLog chat)
    {
        this.chat = chat;
        capture = new LookCapture(Resolve, ManualAnswered, IdentifyAnswered);
        Reset();
    }

    public Action<byte[]> SendLook { get; set; } = _ => { };

    public bool Enabled { get; set; } = true;

    public ItemLook? Held(int sprite)
    {
        if (held.Sprite != sprite)
        {
            held = new Slot { Sprite = sprite };
        }

        return sprite != 0 ? held.Text : null;
    }

    public static bool IsManualLook(byte[] packet) =>
        packet.Length >= GearSlotOffset + sizeof(int)
        && ((ClientCommand)packet[0] == ClientCommand.InventoryLook
            || ((ClientCommand)packet[0] == ClientCommand.Inventory
                && ReadValue(packet, ActionOffset) == (int)InventoryAction.LookAtGear));

    public void Reset()
    {
        capture.Abandon();
        Refill(inventory);
        Refill(worn);

        flight = null;
        headerless.Clear();
        held = new Slot();
        heldSprite = 0;
        vacated.Clear();
        arrivals.Clear();
        handoffTick = -1;
        tick = RefetchTicks;
        pauseUntil = 0;
    }

    public void SlotChanged(ItemSlotKind kind, int index, int sprite)
    {
        if (SlotFor(kind, index) is not { } slot)
        {
            return;
        }

        MarkFlightStale(kind, index);

        // The item that was here may be heading for the hand, even if the new one looks the same.
        BeginHandoff();
        if (slot.Text != null && vacated.Count < MaxHandoffs)
        {
            vacated.Add(slot.Copy());
        }

        if (sprite != 0 && arrivals.Count < MaxHandoffs)
        {
            arrivals.Add((kind, index, sprite));
        }

        Track(slot, sprite);
    }

    private void Track(Slot slot, int sprite)
    {
        if (sprite == 0)
        {
            slot.Clear();
            return;
        }

        if (sprite != slot.Sprite)
        {
            slot.Sprite = sprite;
            slot.Text = null;
            slot.Failures = 0;
        }

        if (headerless.Contains(sprite))
        {
            slot.State = SlotState.Failed;
        }
        else if (slot.State != SlotState.Dirty)
        {
            slot.State = SlotState.Dirty;
            slot.DirtyTick = tick;
        }
    }

    // The held item can't be looked at, so its text comes from the slot it left and goes to the slot it lands in;
    // both slot updates arrive in the same tick as the held item's.
    public void HeldItemChanged(int sprite)
    {
        BeginHandoff();
        heldSprite = sprite;

        // A hand emptied while another slot changed means the held item was used up on it, as when merging
        // soulstones or catalysts, so its text describes nothing that is left.
        var putDown = sprite != 0 || vacated.Count == 0;
        if (held.Text != null && putDown)
        {
            foreach (var arrival in arrivals)
            {
                if (arrival.Sprite != held.Sprite)
                {
                    continue;
                }

                if (SlotFor(arrival.Kind, arrival.Index) is { } destination && destination.Sprite == arrival.Sprite)
                {
                    destination.TakeOver(held);
                }

                break;
            }
        }

        if (sprite == 0)
        {
            held = new Slot();
            arrivals.Clear();
            vacated.Clear();
            return;
        }

        var departure = vacated.FindIndex(slot => slot.Text != null && slot.Sprite == sprite);
        if (departure >= 0)
        {
            held = vacated[departure];
            vacated.RemoveAt(departure);
        }
        else
        {
            held = new Slot { Sprite = sprite };
        }

        arrivals.Clear();
    }

    public void Sent(byte[] packet)
    {
        if (IsManualLook(packet))
        {
            var (kind, index) = LookedAt(packet);
            capture.ExpectManual(tick, new ManualLook(kind, index, SlotFor(kind, index)?.Sprite ?? 0));
            pauseUntil = tick + ManualPauseTicks;
        }
        else if (heldSprite != 0 && IsIdentify(packet))
        {
            capture.ExpectIdentify(tick);
        }
    }

    public bool Offer(LogLine line) => capture.Offer(line);

    public void EndOfTick()
    {
        using var timing = Timings.Measure("tick.itemlooks");
        capture.EndOfTick();
        tick++;
        if (flight != null && tick - flight.SentTick > TimeoutTicks)
        {
            Abandon();
        }

        capture.DropUnansweredSentBefore(tick - TimeoutTicks);
        FetchNext();
    }

    // God and Osiris weapons count kills and gain levels without the server sending their slot again.
    public void Prefer(ItemSlotKind kind, int index)
    {
        preferred = (kind, index);
        if (heldSprite == 0 && kind == ItemSlotKind.Worn && index is (int)WornSlot.MainHand or (int)WornSlot.OffHand
            && SlotFor(kind, index) is { State: SlotState.Ready } slot && tick - slot.ReadyTick >= WeaponStaleTicks)
        {
            slot.State = SlotState.Dirty;
            slot.DirtyTick = tick - DebounceTicks;
        }
    }

    public bool IsUpdating(ItemSlotKind kind, int index) =>
        SlotFor(kind, index) is { State: SlotState.Dirty, Text: not null };

    public void ClearPreference() => preferred = null;

    public ItemLook? For(ItemSlotKind kind, int index, int sprite)
    {
        if (SlotFor(kind, index) is not { } slot)
        {
            return null;
        }

        if (slot.Sprite != sprite)
        {
            MarkFlightStale(kind, index);
            Track(slot, sprite);
            return null;
        }

        return slot.State != SlotState.Empty ? slot.Text : null;
    }

    private static int ReadValue(byte[] packet, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(offset));

    private static bool IsIdentify(byte[] packet) =>
        packet.Length >= ActionOffset + sizeof(int)
        && (ClientCommand)packet[0] == ClientCommand.Skill
        && ReadValue(packet, ActionOffset) == IdentifySkill;

    private static (ItemSlotKind Kind, int Index) LookedAt(byte[] packet) =>
        (ClientCommand)packet[0] == ClientCommand.InventoryLook
            ? (ItemSlotKind.Inventory, ReadValue(packet, ActionOffset))
            : (ItemSlotKind.Worn, ReadValue(packet, GearSlotOffset));

    private static void Refill(Slot[] slots)
    {
        for (var index = 0; index < slots.Length; index++)
        {
            slots[index] = new Slot();
        }
    }

    private Slot? SlotFor(ItemSlotKind kind, int index)
    {
        var slots = kind == ItemSlotKind.Worn ? worn : inventory;
        return index >= 0 && index < slots.Length ? slots[index] : null;
    }

    private void MarkFlightStale(ItemSlotKind kind, int index)
    {
        if (flight is { } current && current.Kind == kind && current.Index == index)
        {
            current.Stale = true;
        }
    }

    private void BeginHandoff()
    {
        if (handoffTick == tick)
        {
            return;
        }

        handoffTick = tick;
        vacated.Clear();
        arrivals.Clear();
    }

    private void Resolve(LookReply reply)
    {
        if (FlightSlot() is { } slot)
        {
            if (reply.Shared.Count > 0)
            {
                slot.Keep(reply.Shared, tick);
            }
            else
            {
                MarkFailed(slot);
            }
        }

        Release(reply.Released);
        flight = null;
    }

    private void ManualAnswered(ManualLook look, IReadOnlyList<LogLine> lines)
    {
        if (heldSprite == 0 && SlotFor(look.Kind, look.Index) is { } slot && slot.Sprite == look.Sprite
            && look.Sprite != 0)
        {
            slot.Keep(lines, tick);
        }
    }

    // A look at an item elsewhere could arrive instead, so a reply naming another item is not the held one's.
    private void IdentifyAnswered(IReadOnlyList<LogLine> lines)
    {
        if (heldSprite == 0 || held.Sprite != heldSprite || lines.Count == 0)
        {
            return;
        }

        if (held.Text is { Lines: [var knownName, ..] } && knownName != lines[0])
        {
            return;
        }

        held.Keep(lines, tick);
    }

    private void Abandon()
    {
        var released = capture.Abandon();
        if (FlightSlot() is { } slot)
        {
            // No reply at all means the item never announces itself, as with shrines; stop asking.
            if (released.Count == 0)
            {
                if (headerless.Count < MaxHeaderless)
                {
                    headerless.Add(slot.Sprite);
                }

                slot.State = SlotState.Failed;
            }
            else
            {
                MarkFailed(slot);
            }
        }

        Release(released);
        flight = null;
    }

    private Slot? FlightSlot() => flight is { Stale: false } current ? SlotFor(current.Kind, current.Index) : null;

    private void MarkFailed(Slot slot)
    {
        slot.Failures++;
        if (slot.Failures >= MaxFailures)
        {
            slot.State = SlotState.Failed;
            return;
        }

        slot.State = SlotState.Dirty;
        slot.DirtyTick = tick;
    }

    private void Release(IReadOnlyList<LogLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.Text.Length > 0)
            {
                chat.Add(line);
            }
        }
    }

    // The server adds a comparison with the held item to a look, which must not be kept.
    private void FetchNext()
    {
        if (!Enabled || flight != null || capture.HasPending || tick < pauseUntil || heldSprite != 0)
        {
            return;
        }

        if (preferred is { } hover && SlotFor(hover.Kind, hover.Index) is { } hoveredSlot && ReadyToFetch(hoveredSlot))
        {
            StartFetch(hover.Kind, hover.Index, hoveredSlot);
            return;
        }

        if (FirstReady(worn) is { } wornIndex)
        {
            StartFetch(ItemSlotKind.Worn, wornIndex, worn[wornIndex]);
            return;
        }

        if (FirstReady(inventory) is { } inventoryIndex)
        {
            StartFetch(ItemSlotKind.Inventory, inventoryIndex, inventory[inventoryIndex]);
        }
    }

    private int? FirstReady(Slot[] slots)
    {
        var index = Array.FindIndex(slots, ReadyToFetch);
        return index >= 0 ? index : null;
    }

    private bool ReadyToFetch(Slot slot) =>
        slot.State == SlotState.Dirty && tick - slot.DirtyTick >= DebounceTicks
        && tick - slot.LastFetchTick >= RefetchTicks;

    private void StartFetch(ItemSlotKind kind, int index, Slot slot)
    {
        flight = new Flight(kind, index, tick);
        slot.LastFetchTick = tick;
        var action = kind == ItemSlotKind.Worn ? InventoryAction.LookAtGear : InventoryAction.LookAtItem;
        SendLook(InventoryPackets.For(action, index, 0));
        SendLook(InventoryPackets.For(action, index, 0));
        capture.ExpectFetch(tick);
    }

    private enum SlotState
    {
        Empty,
        Dirty,
        Ready,
        Failed,
    }

    private sealed class Flight(ItemSlotKind kind, int index, int sentTick)
    {
        public ItemSlotKind Kind { get; } = kind;
        public int Index { get; } = index;
        public int SentTick { get; } = sentTick;
        public bool Stale { get; set; }
    }

    private sealed class Slot
    {
        public SlotState State { get; set; }
        public int Sprite { get; set; }
        public ItemLook? Text { get; set; }
        public int DirtyTick { get; set; }
        public int LastFetchTick { get; set; }
        public int ReadyTick { get; set; }
        public int Failures { get; set; }

        public Slot Copy() => (Slot)MemberwiseClone();

        public void Keep(IReadOnlyList<LogLine> lines, int tick)
        {
            Text = new ItemLook(lines, ItemDetailsParser.Parse(lines));
            ReadyTick = tick;
            State = SlotState.Ready;
            Failures = 0;
        }

        public void TakeOver(Slot origin)
        {
            State = origin.State;
            Text = origin.Text;
            DirtyTick = origin.DirtyTick;
            LastFetchTick = origin.LastFetchTick;
            ReadyTick = origin.ReadyTick;
            Failures = origin.Failures;
        }

        public void Clear()
        {
            State = SlotState.Empty;
            Sprite = 0;
            Text = null;
            Failures = 0;
        }
    }
}
