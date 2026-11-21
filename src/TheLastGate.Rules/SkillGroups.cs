using System;

namespace TheLastGate.Rules;

[Flags]
public enum SkillGroups
{
    None = 0,
    Passive = 1,
    ActiveSpell = 2,
    ActiveMelee = 4,
}

// The groups the server's tree nodes and corruptors talk about ("+x% to passive skills"). Skill 31 is in two, as it is
// on the server.
public static class SkillGroupTable
{
    private static readonly (int First, int Last, SkillGroups Group)[] Ranges =
    [
        (0, 10, SkillGroups.Passive), (12, 12, SkillGroups.Passive), (14, 14, SkillGroups.Passive),
        (16, 16, SkillGroups.Passive), (23, 23, SkillGroups.Passive), (28, 34, SkillGroups.Passive),
        (36, 36, SkillGroups.Passive), (38, 39, SkillGroups.Passive), (44, 45, SkillGroups.Passive),
        (11, 11, SkillGroups.ActiveSpell), (15, 15, SkillGroups.ActiveSpell), (17, 21, SkillGroups.ActiveSpell),
        (24, 27, SkillGroups.ActiveSpell), (42, 43, SkillGroups.ActiveSpell), (46, 47, SkillGroups.ActiveSpell),
        (13, 13, SkillGroups.ActiveMelee), (22, 22, SkillGroups.ActiveMelee), (31, 31, SkillGroups.ActiveMelee),
        (35, 35, SkillGroups.ActiveMelee), (37, 37, SkillGroups.ActiveMelee), (40, 41, SkillGroups.ActiveMelee),
        (48, 49, SkillGroups.ActiveMelee),
    ];

    public static SkillGroups Of(int skillNumber)
    {
        var groups = SkillGroups.None;
        foreach (var (first, last, group) in Ranges)
        {
            if (skillNumber >= first && skillNumber <= last)
            {
                groups |= group;
            }
        }

        return groups;
    }
}
