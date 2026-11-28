using System;
using System.Buffers.Binary;
using TheLastGate.World;

namespace TheLastGate.Characters;

// The settings block of a character file: name, description and changed flag (read by
// CharacterRecord), then five int32 toggles at 324 (the first is a WallHiding, the rest 0 or 1)
// and twenty { char name[8], int32 action } hotbar entries at 344.
public sealed class CharacterSettings
{
    public const int HotbarSlots = 20;
    public const int Unassigned = -1;
    public const string EmptyHotbarName = "-";

    private const int TogglesOffset = 324;
    private const int ToggleCount = 5;
    private const int HotbarOffset = 344;
    private const int HotbarNameLength = 8;
    private const int HotbarEntrySize = 12;

    private static readonly (int Slot, int Skill, string Name)[] EveryClassStartingSkills =
        [(0, 50, "Light"), (19, 51, "Recall")];

    private static readonly (int Slot, int Skill, string Name)[] TemplarStartingSkills = [(7, 40, "Cleave")];

    private static readonly (int Slot, int Skill, string Name)[] HarakimStartingSkills =
        [(7, 24, "Blast"), (11, 27, "Ghost C"), (4, 11, "Magic S"), (3, 17, "Protect")];

    private static readonly (int Slot, int Skill, string Name)[] OtherClassStartingSkills =
        [(11, 37, "Blind"), (3, 17, "Protect")];

    public WallHiding WallHiding { get; set; } = WallHiding.Near;
    public bool ShowNames { get; set; } = true;
    public bool ShowHealthPercent { get; set; } = true;
    public bool ShowHealthBars { get; set; }
    public bool ShowStatBase { get; set; }

    // A skill number below 60, 100 + an inventory slot, or 200 + an equipment slot.
    public int[] HotbarActions { get; } = CreateFilled(Unassigned);
    public string[] HotbarNames { get; } = CreateFilled(EmptyHotbarName);

    public static CharacterSettings Parse(ReadOnlySpan<byte> stored)
    {
        var toggles = new int[ToggleCount];
        for (var toggleIndex = 0; toggleIndex < ToggleCount; toggleIndex++)
        {
            toggles[toggleIndex] =
                BinaryPrimitives.ReadInt32LittleEndian(stored[(TogglesOffset + toggleIndex * sizeof(int))..]);
        }

        var settings = new CharacterSettings
        {
            WallHiding = ReadWallHiding(toggles[0]),
            ShowNames = toggles[1] != 0,
            ShowHealthPercent = toggles[2] != 0,
            ShowHealthBars = toggles[3] != 0,
            ShowStatBase = toggles[4] != 0,
        };

        for (var slot = 0; slot < HotbarSlots; slot++)
        {
            var entry = stored.Slice(HotbarOffset + slot * HotbarEntrySize, HotbarEntrySize);
            settings.HotbarNames[slot] = NulTerminatedText.Read(entry[..HotbarNameLength]);
            settings.HotbarActions[slot] = BinaryPrimitives.ReadInt32LittleEndian(entry[HotbarNameLength..]);
        }

        return settings;
    }

    public void SetStartingHotbar(CharacterClass characterClass)
    {
        Array.Fill(HotbarActions, Unassigned);
        Array.Fill(HotbarNames, EmptyHotbarName);
        AssignHotbar(EveryClassStartingSkills);
        AssignHotbar(
            characterClass switch
            {
                CharacterClass.Templar => TemplarStartingSkills,
                CharacterClass.Harakim => HarakimStartingSkills,
                _ => OtherClassStartingSkills,
            });
    }

    public void Write(Span<byte> stored)
    {
        int[] toggles =
        [
            (int)WallHiding, Stored(ShowNames), Stored(ShowHealthPercent), Stored(ShowHealthBars), Stored(ShowStatBase),
        ];
        for (var toggleIndex = 0; toggleIndex < ToggleCount; toggleIndex++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                stored[(TogglesOffset + toggleIndex * sizeof(int))..],
                toggles[toggleIndex]);
        }

        for (var slot = 0; slot < HotbarSlots; slot++)
        {
            var entry = stored.Slice(HotbarOffset + slot * HotbarEntrySize, HotbarEntrySize);
            NulTerminatedText.Write(entry[..HotbarNameLength], HotbarNames[slot]);
            BinaryPrimitives.WriteInt32LittleEndian(entry[HotbarNameLength..], HotbarActions[slot]);
        }
    }

    private void AssignHotbar((int Slot, int Skill, string Name)[] skills)
    {
        foreach (var (slot, skill, name) in skills)
        {
            HotbarActions[slot] = skill;
            HotbarNames[slot] = name;
        }
    }

    private static WallHiding ReadWallHiding(int stored) =>
        stored switch
        {
            0 => WallHiding.Off,
            2 => WallHiding.All,
            _ => WallHiding.Near,
        };

    private static int Stored(bool toggle) => toggle ? 1 : 0;

    private static T[] CreateFilled<T>(T value)
    {
        var values = new T[HotbarSlots];
        Array.Fill(values, value);
        return values;
    }
}
