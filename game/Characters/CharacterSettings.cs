using System;
using System.Buffers.Binary;

namespace TheLastGate.Characters;

// The settings block of a character file: name, description and changed flag (read by
// CharacterRecord), then five int32 toggles at 324 and twenty { char name[8], int32 action }
// hotbar entries at 344.
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

    public bool HideWalls { get; set; } = true;
    public bool ShowNames { get; set; } = true;
    public bool ShowHealthPercent { get; set; } = true;
    public bool ShowHealthBars { get; set; }
    public bool ShowStatBase { get; set; }

    // A skill number below 60, 100 + an inventory slot, or 200 + an equipment slot.
    public int[] HotbarActions { get; } = CreateFilled(Unassigned);
    public string[] HotbarNames { get; } = CreateFilled(EmptyHotbarName);

    public static CharacterSettings Parse(ReadOnlySpan<byte> stored)
    {
        var toggles = new bool[ToggleCount];
        for (var toggleIndex = 0; toggleIndex < ToggleCount; toggleIndex++)
        {
            toggles[toggleIndex] =
                BinaryPrimitives.ReadInt32LittleEndian(stored[(TogglesOffset + toggleIndex * sizeof(int))..]) != 0;
        }

        var settings = new CharacterSettings
        {
            HideWalls = toggles[0],
            ShowNames = toggles[1],
            ShowHealthPercent = toggles[2],
            ShowHealthBars = toggles[3],
            ShowStatBase = toggles[4],
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
        bool[] toggles = [HideWalls, ShowNames, ShowHealthPercent, ShowHealthBars, ShowStatBase];
        for (var toggleIndex = 0; toggleIndex < ToggleCount; toggleIndex++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                stored[(TogglesOffset + toggleIndex * sizeof(int))..],
                toggles[toggleIndex] ? 1 : 0);
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

    private static T[] CreateFilled<T>(T value)
    {
        var values = new T[HotbarSlots];
        Array.Fill(values, value);
        return values;
    }
}
