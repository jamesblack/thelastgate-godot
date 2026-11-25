using System.Collections.Generic;
using System.Linq;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.State;

namespace TheLastGate.Hotbar;

public static class HotbarAssignments
{
    private const int ButtonNameLength = 7;

    private static readonly int[] UsableSkills =
        [37, 40, 16, 49, 22, 48, 41, 35, 24, 21, 20, 25, 18, 27, 47, 26, 52, 15, 50, 11, 42, 17, 43, 51, 46, 19];

    public static LogLine Assign(
        CharacterSettings settings,
        int slot,
        int skillNumber,
        string skillName,
        string keyName)
    {
        if (settings.HotbarActions[slot] == skillNumber)
        {
            Clear(settings, slot);
            return Line($"{keyName} is now unassigned.");
        }

        settings.HotbarActions[slot] = skillNumber;
        settings.HotbarNames[slot] = skillName.Length > ButtonNameLength ? skillName[..ButtonNameLength] : skillName;
        return Line($"{keyName} is now {skillName}.");
    }

    public static void Clear(CharacterSettings settings, int slot)
    {
        settings.HotbarActions[slot] = CharacterSettings.Unassigned;
        settings.HotbarNames[slot] = CharacterSettings.EmptyHotbarName;
    }

    public static IReadOnlyList<int> MenuSkills(int[][] skills, uint kindred) =>
        UsableSkills
            .Where(skill => Kindred.IsKnown(skill, skills[skill][0] != 0, kindred))
            .ToList();

    private static LogLine Line(string text) => new(text, (int)TextColour.Yellow);
}
