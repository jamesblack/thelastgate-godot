using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheLastGate.Buffs;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Planner;

// Equal to another input with the same numbers, so a search can be kept until something it reads changes.
public sealed class BuffFocusInput : IEquatable<BuffFocusInput>
{
    // The planner's state, plan included.
    public BuffModelInput Model { get; set; } = new();

    public BuffModelSkill Focused { get; set; } = new();

    public int FocusedRaiseIndex { get; set; }

    // The stat raise slot of each spell skill; NoSlot when not known.
    public int[] SpellRaiseIndex { get; } = [BuffFocus.NoSlot, BuffFocus.NoSlot, BuffFocus.NoSlot];

    // The focused Resistance or Immunity gains a tenth of the shown AV.
    public bool ArmorFeedsFocused { get; set; }

    // Read only when armor feeds the focused skill.
    public CombatModelInput Combat { get; set; } = new();

    public BuffModelSkill[] CombatSkills { get; } = NewCombatSkills();

    public int[] CombatRaiseIndex { get; } = new int[CombatModel.SkillCount];

    public List<FocusCandidate> Candidates { get; } = [];

    public bool Equals(BuffFocusInput? other) =>
        other != null
        && (FocusedRaiseIndex, ArmorFeedsFocused) == (other.FocusedRaiseIndex, other.ArmorFeedsFocused)
        && Model.Equals(other.Model)
        && Focused.Equals(other.Focused)
        && SpellRaiseIndex.SequenceEqual(other.SpellRaiseIndex)
        && Combat.Equals(other.Combat)
        && CombatSkills.SequenceEqual(other.CombatSkills)
        && CombatRaiseIndex.SequenceEqual(other.CombatRaiseIndex)
        && Candidates.SequenceEqual(other.Candidates);

    public override bool Equals(object? other) => Equals(other as BuffFocusInput);

    public override int GetHashCode() => HashCode.Combine(Model, Focused, FocusedRaiseIndex, Candidates.Count);

    internal static BuffModelSkill[] NewCombatSkills()
    {
        var skills = new BuffModelSkill[CombatModel.SkillCount];
        for (var skill = 0; skill < skills.Length; skill++)
        {
            skills[skill] = new BuffModelSkill();
        }

        return skills;
    }
}

public sealed record BuffFocusResult(
    int CurrentValue,
    FocusRoute? Best,
    FocusRoute? Direct,
    FocusRoute? ThroughAttributes,
    FocusRoute? ThroughBless);

// The cheapest way to give the focused skill +1 once buffs are part of the plan: mixes of extra points over the
// candidates (the skill itself, its attributes, Bless and what feeds Bless) are tried cheapest first, and the buff
// model says whether a mix lifts the skill's shown value by one.
public static class BuffFocus
{
    public const int NoSlot = -1;

    // Under the Reverse Fool only the sum of planned attribute points matters, so one pool stands for all five.
    public const int AttributePool = -2;

    // The stat raise slots of Hitpoints and Mana.
    public const int HitpointsRaise = 5;
    public const int ManaRaise = 7;

    private const int Bless = (int)BuffKind.Bless;

    // Every route is one of three kinds: the skill itself, its attributes, or through Bless.
    public static BuffFocusResult Search(BuffFocusInput input, CancellationToken cancel = default)
    {
        var focusedCandidate = input.Candidates.FindIndex(candidate =>
            candidate.RaiseIndex == input.FocusedRaiseIndex);
        var blessCandidate = input.SpellRaiseIndex[Bless] < 0
            ? -1
            : input.Candidates.FindIndex(candidate =>
                candidate.RaiseIndex != input.FocusedRaiseIndex
                && candidate.RaiseIndex == input.SpellRaiseIndex[Bless]);
        var currentValue = ValueWithPoints(input, new int[input.Candidates.Count]);
        var target = currentValue + 1;

        return new BuffFocusResult(
            currentValue,
            FocusSearch.Cheapest(input.Candidates, uint.MaxValue, Reaches, cancel),
            focusedCandidate < 0
                ? null
                : FocusSearch.Cheapest(input.Candidates, Bit(focusedCandidate), Reaches, cancel),
            FocusSearch.Cheapest(input.Candidates, ~(Bit(focusedCandidate) | Bit(blessCandidate)), Reaches, cancel),
            blessCandidate < 0
                ? null
                : FocusSearch.Cheapest(input.Candidates, uint.MaxValue, NeedsItsBlessPoints, cancel));

        bool Reaches(int[] points) => ValueWithPoints(input, points) >= target;

        // Only a route whose last Bless point is needed counts as going through Bless.
        bool NeedsItsBlessPoints(int[] points)
        {
            if (points[blessCandidate] == 0 || !Reaches(points))
            {
                return false;
            }

            var fewerBless = (int[])points.Clone();
            fewerBless[blessCandidate]--;
            return !Reaches(fewerBless);
        }
    }

    internal static void AddPoints(BuffModelInput model, int raiseIndex, int extra, int[] spellRaiseIndex)
    {
        if (raiseIndex is >= 0 and < AttributeAmounts.Count)
        {
            model.PlannedAttributes[(AttributeSlot)raiseIndex] += extra;
        }

        switch (raiseIndex)
        {
            case AttributePool:
                model.PlannedAttributes[AttributeSlot.Braveness] += extra;
                break;
            case HitpointsRaise:
                model.PlannedHitpoints += extra;
                break;
            case ManaRaise:
                model.PlannedMana += extra;
                break;
        }

        for (var kind = 0; kind < BuffModelInput.BuffCount; kind++)
        {
            if (raiseIndex == spellRaiseIndex[kind])
            {
                model.SpellSkills[kind] = model.SpellSkills[kind] with
                {
                    Planned = model.SpellSkills[kind].Planned + extra,
                };
            }
        }
    }

    private static uint Bit(int candidate) => candidate >= 0 ? 1u << candidate : 0;

    private static int ValueWithPoints(BuffFocusInput input, int[] points)
    {
        var model = input.Model.Copy();
        var focused = input.Focused;
        var combatSkills = input.CombatSkills.ToArray();
        for (var index = 0; index < input.Candidates.Count; index++)
        {
            var raiseIndex = input.Candidates[index].RaiseIndex;
            var extra = points[index];
            if (extra == 0)
            {
                continue;
            }

            AddPoints(model, raiseIndex, extra, input.SpellRaiseIndex);
            if (raiseIndex == input.FocusedRaiseIndex)
            {
                focused = focused with { Planned = focused.Planned + extra };
            }

            if (input.ArmorFeedsFocused)
            {
                AddCombatPoints(input, raiseIndex, extra, combatSkills);
            }
        }

        var result = BuffModel.Compute(model);
        if (!input.ArmorFeedsFocused)
        {
            return BuffModel.SkillValue(model, focused, result.Attributes);
        }

        var shown = BuffModel.CombatShown(model, result, input.Combat, combatSkills);
        return BuffModel.SkillValueWithArmor(model, focused, result.Attributes, shown.Armor);
    }

    // A skill not known is skipped: its raise slot may have been left at 0, which is Braveness's.
    private static void AddCombatPoints(BuffFocusInput input, int raiseIndex, int extra, BuffModelSkill[] combatSkills)
    {
        for (var skill = 0; skill < CombatModel.SkillCount; skill++)
        {
            if (input.Combat.Known[skill] && raiseIndex == input.CombatRaiseIndex[skill])
            {
                combatSkills[skill] = combatSkills[skill] with { Planned = combatSkills[skill].Planned + extra };
            }
        }
    }
}
