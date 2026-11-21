using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.State;

namespace TheLastGate.Stats;

public static class SkillOrder
{
    private const int SeyanDuSkill = 44;
    private const int IdentifySkill = 52;

    private static readonly HashSet<int> ClassSkills = [50, 51, 52, 53, 54];
    private static readonly HashSet<int> LycanthropeSkills = [53, 54];
    private static readonly HashSet<int> WorksUnlearnt = [8, 23, 28, 29, 30, 32, 44, 50, 51, 52, 53, 54];

    public static int[] Sorted(IEnumerable<SkillEntry> skills, uint kindred)
    {
        var sorted = skills.ToArray();
        Array.Sort(sorted, (first, second) => Compare(first, second, kindred));
        return Array.ConvertAll(sorted, skill => skill.Number);
    }

    private static int Compare(SkillEntry first, SkillEntry second, uint kindred)
    {
        if (first.Number == second.Number)
        {
            return 0;
        }

        if (first.Learnt != second.Learnt)
        {
            return first.Learnt ? -1 : 1;
        }

        var knowsIdentify = (kindred & Kindred.KnowsIdentify) != 0;
        if (!knowsIdentify && Last(first, second, skill => skill.Number == IdentifySkill) is { } identify)
        {
            return identify;
        }

        var isLycanthrope = (kindred & Kindred.Lycanthrope) != 0;
        if (!isLycanthrope && Last(first, second, skill => LycanthropeSkills.Contains(skill.Number)) is { } lycanthrope)
        {
            return lycanthrope;
        }

        if (Last(first, second, skill => !ClassSkills.Contains(skill.Number)) is { } classSkill)
        {
            return classSkill;
        }

        if (!first.Learnt && Last(first, second, skill => !WorksUnlearnt.Contains(skill.Number)) is { } unlearnt)
        {
            return unlearnt;
        }

        if ((kindred & Kindred.SeyanDu) == 0)
        {
            if (first.Number == SeyanDuSkill)
            {
                return 1;
            }

            if (second.Number == SeyanDuSkill)
            {
                return -1;
            }
        }

        return first.SortKey != second.SortKey
            ? first.SortKey.CompareTo(second.SortKey)
            : string.CompareOrdinal(first.Name, second.Name);
    }

    private static int? Last(SkillEntry first, SkillEntry second, Func<SkillEntry, bool> goesLast)
    {
        var firstLast = goesLast(first);
        return firstLast == goesLast(second) ? null : firstLast ? 1 : -1;
    }
}
