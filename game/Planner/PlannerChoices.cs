using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using TheLastGate.Buffs;

namespace TheLastGate.Planner;

// The planner's picks for one character. A weapon or off-hand not picked yet follows the character's best skill.
public sealed class PlannerChoices
{
    public const int MostCopies = 20;

    public CombatWeapon? Weapon { get; set; }

    public CombatOffhand? Offhand { get; set; }

    // Bit 1 << BuffKind is set when that spell is shown buffed.
    public uint SpellToggles { get; set; }

    public ulong TarotCards { get; set; }

    public uint CorruptorTicks { get; set; }

    // Kept while unticked.
    public int[] CorruptorCounts { get; } = new int[TreeEffects.CorruptorCount];

    public uint EnchantTicks { get; set; }

    public int[] EnchantCounts { get; } = new int[ItemEffects.EnchantCount];

    public uint GearTicks { get; set; }

    public void ToggleSpell(BuffKind kind) => SpellToggles ^= 1u << (int)kind;

    public void ToggleCard(TarotCard card) => TarotCards ^= 1ul << (int)card;

    // Ticking one with no copies gives it one; unticking keeps the count for next time.
    public void ToggleCorruptor(int corruptor)
    {
        CorruptorTicks ^= 1u << corruptor;
        if ((CorruptorTicks & (1u << corruptor)) != 0 && CorruptorCounts[corruptor] == 0)
        {
            CorruptorCounts[corruptor] = 1;
        }
    }

    public void ToggleEnchant(int enchant)
    {
        EnchantTicks ^= 1u << enchant;
        if ((EnchantTicks & (1u << enchant)) != 0 && ItemEffects.Enchants[enchant].Counted
            && EnchantCounts[enchant] == 0)
        {
            EnchantCounts[enchant] = 1;
        }
    }

    public void ToggleGear(int gear) => GearTicks ^= 1u << gear;

    public void SetCorruptorCount(int corruptor, int count) =>
        CorruptorCounts[corruptor] = Math.Clamp(count, 1, MostCopies);

    public void SetEnchantCount(int enchant, int count) => EnchantCounts[enchant] = Math.Clamp(count, 1, MostCopies);
}

// The planner's picks for every character, in the current client's file layout: a format word and a record count, then
// the records, all 32-bit little-endian words. Format 1 records hold the character number, weapon and off-hand; format 2
// adds the spell toggles and the tarot cards as two words; format 3 adds the corruptor ticks and a count byte for each
// corruptor, padded to 28; format 4 adds the enchant ticks, a count byte for each enchant padded to 16, and the gear
// ticks. Older formats still load; saving writes format 4.
public static class PlannerChoicesStore
{
    public const string FileName = "TLGPlanner.dat";

    private const int CurrentFormat = 4;
    private const int HeaderSize = 8;
    private const int MostRecords = 256;
    private const int CorruptorCountSlots = 28;
    private const int EnchantCountSlots = 16;
    private const int NotPicked = -1;

    private static readonly int[] RecordSizes = [0, 12, 24, 56, 80];

    // Character 0 isn't created yet, so it has no choices. A count is kept to 20, and a ticked one is at least 1.
    public static PlannerChoices Load(string path, uint characterNumber)
    {
        var choices = new PlannerChoices();
        if (characterNumber == 0 || ReadRecords(path).Find(record => record.Character == characterNumber) is not
            { } found)
        {
            return choices;
        }

        choices.Weapon = Enum.IsDefined((CombatWeapon)found.Weapon) ? (CombatWeapon)found.Weapon : null;
        choices.Offhand = Enum.IsDefined((CombatOffhand)found.Offhand) ? (CombatOffhand)found.Offhand : null;
        choices.SpellToggles = found.SpellToggles;
        choices.TarotCards = ((ulong)found.CardsHigh << 32) | found.CardsLow;
        choices.CorruptorTicks = found.CorruptorTicks;
        for (var corruptor = 0; corruptor < choices.CorruptorCounts.Length; corruptor++)
        {
            choices.CorruptorCounts[corruptor] = CheckedCopies(
                found.CorruptorCounts[corruptor],
                found.CorruptorTicks,
                corruptor);
        }

        choices.EnchantTicks = found.EnchantTicks;
        for (var enchant = 0; enchant < choices.EnchantCounts.Length; enchant++)
        {
            choices.EnchantCounts[enchant] = CheckedCopies(found.EnchantCounts[enchant], found.EnchantTicks, enchant);
        }

        choices.GearTicks = found.GearTicks;
        return choices;
    }

    // Every other character's picks are kept; when the file is full, a new character makes room by dropping the
    // oldest. A damaged file is started again.
    public static bool Save(string path, uint characterNumber, PlannerChoices choices)
    {
        if (characterNumber == 0)
        {
            return false;
        }

        var records = ReadRecords(path);
        var index = records.FindIndex(record => record.Character == characterNumber);
        if (index < 0)
        {
            if (records.Count == MostRecords)
            {
                records.RemoveAt(0);
            }

            records.Add(new ChoiceRecord { Character = characterNumber });
            index = records.Count - 1;
        }

        var stored = records[index];
        stored.Weapon = choices.Weapon is { } weapon ? (int)weapon : NotPicked;
        stored.Offhand = choices.Offhand is { } offhand ? (int)offhand : NotPicked;
        stored.SpellToggles = choices.SpellToggles;
        stored.CardsLow = (uint)choices.TarotCards;
        stored.CardsHigh = (uint)(choices.TarotCards >> 32);
        stored.CorruptorTicks = choices.CorruptorTicks;
        for (var corruptor = 0; corruptor < choices.CorruptorCounts.Length; corruptor++)
        {
            stored.CorruptorCounts[corruptor] = (byte)Math.Clamp(choices.CorruptorCounts[corruptor], 0, byte.MaxValue);
        }

        stored.EnchantTicks = choices.EnchantTicks;
        for (var enchant = 0; enchant < choices.EnchantCounts.Length; enchant++)
        {
            stored.EnchantCounts[enchant] = (byte)Math.Clamp(choices.EnchantCounts[enchant], 0, byte.MaxValue);
        }

        stored.GearTicks = choices.GearTicks;
        return WriteRecords(path, records);
    }

    private static int CheckedCopies(byte stored, uint ticks, int bit)
    {
        var ticked = ((ticks >> bit) & 1) != 0;
        return stored > PlannerChoices.MostCopies ? PlannerChoices.MostCopies : ticked && stored == 0 ? 1 : stored;
    }

    // A missing or damaged file holds nothing.
    private static List<ChoiceRecord> ReadRecords(string path)
    {
        byte[] file;
        try
        {
            file = File.Exists(path) ? File.ReadAllBytes(path) : [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        if (file.Length < HeaderSize)
        {
            return [];
        }

        var format = BinaryPrimitives.ReadUInt32LittleEndian(file);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(4));
        if (format is < 1 or > CurrentFormat || count > MostRecords
            || file.Length < HeaderSize + count * RecordSizes[format])
        {
            return [];
        }

        var records = new List<ChoiceRecord>();
        for (var index = 0; index < count; index++)
        {
            var size = RecordSizes[format];
            records.Add(ChoiceRecord.Read(file.AsSpan(HeaderSize + index * size, size), (int)format));
        }

        return records;
    }

    private static bool WriteRecords(string path, List<ChoiceRecord> records)
    {
        var size = RecordSizes[CurrentFormat];
        var file = new byte[HeaderSize + records.Count * size];
        BinaryPrimitives.WriteUInt32LittleEndian(file, CurrentFormat);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), (uint)records.Count);
        for (var index = 0; index < records.Count; index++)
        {
            records[index].Write(file.AsSpan(HeaderSize + index * size, size));
        }

        try
        {
            File.WriteAllBytes(path, file);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed class ChoiceRecord
    {
        public uint Character { get; set; }

        public int Weapon { get; set; }

        public int Offhand { get; set; }

        public uint SpellToggles { get; set; }

        public uint CardsLow { get; set; }

        public uint CardsHigh { get; set; }

        public uint CorruptorTicks { get; set; }

        public byte[] CorruptorCounts { get; } = new byte[CorruptorCountSlots];

        public uint EnchantTicks { get; set; }

        public byte[] EnchantCounts { get; } = new byte[EnchantCountSlots];

        public uint GearTicks { get; set; }

        // What an older format doesn't hold is left empty.
        public static ChoiceRecord Read(ReadOnlySpan<byte> bytes, int format)
        {
            var record = new ChoiceRecord
            {
                Character = BinaryPrimitives.ReadUInt32LittleEndian(bytes),
                Weapon = BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]),
                Offhand = BinaryPrimitives.ReadInt32LittleEndian(bytes[8..]),
            };
            if (format >= 2)
            {
                record.SpellToggles = BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..]);
                record.CardsLow = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..]);
                record.CardsHigh = BinaryPrimitives.ReadUInt32LittleEndian(bytes[20..]);
            }

            if (format >= 3)
            {
                record.CorruptorTicks = BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]);
                bytes.Slice(28, CorruptorCountSlots).CopyTo(record.CorruptorCounts);
            }

            if (format >= 4)
            {
                record.EnchantTicks = BinaryPrimitives.ReadUInt32LittleEndian(bytes[56..]);
                bytes.Slice(60, EnchantCountSlots).CopyTo(record.EnchantCounts);
                record.GearTicks = BinaryPrimitives.ReadUInt32LittleEndian(bytes[76..]);
            }

            return record;
        }

        public void Write(Span<byte> bytes)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, Character);
            BinaryPrimitives.WriteInt32LittleEndian(bytes[4..], Weapon);
            BinaryPrimitives.WriteInt32LittleEndian(bytes[8..], Offhand);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[12..], SpellToggles);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[16..], CardsLow);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[20..], CardsHigh);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[24..], CorruptorTicks);
            CorruptorCounts.CopyTo(bytes[28..]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[56..], EnchantTicks);
            EnchantCounts.CopyTo(bytes[60..]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes[76..], GearTicks);
        }
    }
}
