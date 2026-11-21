using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Planner;

// Equal to another input with the same numbers, so a search can be kept until something it reads changes.
public sealed class CombatFocusInput : IEquatable<CombatFocusInput>
{
    // The planner's state, plan included.
    public BuffModelInput Model { get; set; } = new();

    // The weapon, off-hand, race and server values; the shown skills and base Weapon and Armor Value are worked out
    // again for each mix.
    public CombatModelInput Combat { get; set; } = new();

    public BuffModelSkill[] CombatSkills { get; } = BuffFocusInput.NewCombatSkills();

    // The stat raise slot of each combat skill; NoSlot when not known.
    public int[] CombatRaiseIndex { get; } = NoSlots(CombatModel.SkillCount);

    public int[] SpellRaiseIndex { get; } = NoSlots(BuffModelInput.BuffCount);

    public CombatStat Stat { get; set; }

    public List<FocusCandidate> Candidates { get; } = [];

    public bool Equals(CombatFocusInput? other) =>
        other != null
        && Stat == other.Stat
        && Model.Equals(other.Model)
        && Combat.Equals(other.Combat)
        && CombatSkills.SequenceEqual(other.CombatSkills)
        && CombatRaiseIndex.SequenceEqual(other.CombatRaiseIndex)
        && SpellRaiseIndex.SequenceEqual(other.SpellRaiseIndex)
        && Candidates.SequenceEqual(other.Candidates);

    public override bool Equals(object? other) => Equals(other as CombatFocusInput);

    public override int GetHashCode() => HashCode.Combine(Model, Combat, Stat, Candidates.Count);

    private static int[] NoSlots(int count)
    {
        var slots = new int[count];
        Array.Fill(slots, BuffFocus.NoSlot);
        return slots;
    }
}

// The cheapest route to +1 on the stat; a tie goes to fewer points.
public sealed record CombatFocusResult(int CurrentValue, FocusRoute? Best);

// The cheapest way to give Hit, Parry, Weapon or Armor Value +1, with buffs, tarot and item effects counted.
public static class CombatFocus
{
    public static CombatFocusResult Search(CombatFocusInput input)
    {
        var currentValue = StatWithPoints(input, new int[input.Candidates.Count]);
        var best = FocusSearch.Cheapest(
            input.Candidates,
            uint.MaxValue,
            points => StatWithPoints(input, points) >= currentValue + 1);
        return new CombatFocusResult(currentValue, best);
    }

    private static int StatWithPoints(CombatFocusInput input, int[] points)
    {
        var model = input.Model.Copy();
        var combatSkills = input.CombatSkills.ToArray();
        for (var index = 0; index < input.Candidates.Count; index++)
        {
            var raiseIndex = input.Candidates[index].RaiseIndex;
            var extra = points[index];
            if (extra == 0)
            {
                continue;
            }

            BuffFocus.AddPoints(model, raiseIndex, extra, input.SpellRaiseIndex);
            for (var skill = 0; skill < CombatModel.SkillCount; skill++)
            {
                if (raiseIndex == input.CombatRaiseIndex[skill])
                {
                    combatSkills[skill] = combatSkills[skill] with { Planned = combatSkills[skill].Planned + extra };
                }
            }
        }

        var shown = BuffModel.CombatShown(model, BuffModel.Compute(model), input.Combat, combatSkills);
        return shown[input.Stat];
    }
}
