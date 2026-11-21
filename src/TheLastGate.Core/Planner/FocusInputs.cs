using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheLastGate.Buffs;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Planner;

// What each focus search reads, taken from one planner sheet: the values the rows show and the cost of every point
// beyond the plan.
public static class FocusInputs
{
    private const int SkillAttributeSlots = 3;
    private const int FoolPoolMostPoints = 10;
    private const int ResistSkill = 23;
    private const int ImmunitySkill = 32;
    private const int NoCost = SkillFocus.NoCost;

    // The group of skills each attribute's tree extra percent goes to.
    private static readonly SkillGroups[] ExtraPercentGroups =
    [
        SkillGroups.Passive, SkillGroups.ActiveSpell, SkillGroups.ActiveSpell, SkillGroups.ActiveMelee,
        SkillGroups.ActiveMelee,
    ];

    public static SkillFocusInput Plain(PlannerSheet sheet, int skill)
    {
        var game = sheet.Game;
        var input = new SkillFocusInput
        {
            Divisor = SkillPreview.AttributeDivisor(game.HardMode),
            DirectCost = CostAfter(game, StatPlan.SkillSlot(skill), 0),
            FoolReversed = TarotMath.Has(game.Choices.TarotCards, TarotCard.FoolReversed),
        };
        var attributes = game.SkillTexts[skill].Attributes;
        for (var slot = 0; slot < SkillAttributeSlots && slot < attributes.Count; slot++)
        {
            if (attributes[slot] is not (>= 0 and < AttributeAmounts.Count))
            {
                continue;
            }

            var attribute = (AttributeSlot)attributes[slot];
            input.Attributes[slot] = attribute;

            // The values the rows show, so the suggested +1 is a +1 in the preview.
            input.AttributeValues[slot] = sheet.Planned?.Attributes[attribute]
                ?? PlannerGame.Score(game.Attributes[(int)attribute]) + game.Plan.Raised((int)attribute);
            FillCosts(game, (int)attribute, input.AttributeStepCosts[slot]);
        }

        for (var attribute = 0; attribute < AttributeAmounts.Count; attribute++)
        {
            FillCosts(game, attribute, input.PoolStepCosts[attribute]);
        }

        return input;
    }

    // Null without the buff model.
    public static BuffFocusInput? Buffed(PlannerSheet sheet, int skill)
    {
        if (sheet.PlannedInput is not { } model)
        {
            return null;
        }

        var input = new BuffFocusInput
        {
            Model = model,
            Focused = sheet.ModelSkill(skill, true),
            FocusedRaiseIndex = StatPlan.SkillSlot(skill),
        };
        SpellRaiseIndexes(model).CopyTo(input.SpellRaiseIndex, 0);
        if (ArmorFeeds(model.Effects, skill) && sheet.Combat is { } combat)
        {
            input.ArmorFeedsFocused = true;
            input.Combat = combat;
            sheet.CombatSkills(true).CopyTo(input.CombatSkills, 0);
            CombatRaiseIndexes(combat).CopyTo(input.CombatRaiseIndex, 0);
        }

        var candidates = new CandidateList(sheet.Game, input.Candidates);
        var blessShown = SpellShown(model, BuffKind.Bless);
        candidates.Add(input.FocusedRaiseIndex, 1);
        if (FoolReversed(model))
        {
            candidates.AddAttributePool();
        }
        else
        {
            candidates.AddAttributes(input.Focused.Attributes);
            candidates.AddAttributes(ExtraPercentAttributes(input.Focused, model.Effects));
            if (blessShown)
            {
                candidates.AddAttributes(model.SpellSkills[(int)BuffKind.Bless].Attributes);
                candidates.AddAptitude();
            }
        }

        if (blessShown)
        {
            candidates.Add(input.SpellRaiseIndex[(int)BuffKind.Bless], FocusSearch.MostPoints);
        }

        if (input.ArmorFeedsFocused)
        {
            AddCombatStat(
                candidates,
                model,
                input.Combat,
                input.CombatSkills,
                input.CombatRaiseIndex,
                CombatStat.Armor);
        }

        if (blessShown)
        {
            AddPools(candidates, model.Effects);
        }

        return input;
    }

    // Null until the Combat block has more than the server's values.
    public static CombatFocusInput? Combat(PlannerSheet sheet, CombatStat stat)
    {
        if (sheet.PlannedInput is not { } model || sheet.Combat is not { } combat)
        {
            return null;
        }

        var input = new CombatFocusInput { Model = model, Combat = combat, Stat = stat };
        SpellRaiseIndexes(model).CopyTo(input.SpellRaiseIndex, 0);
        sheet.CombatSkills(true).CopyTo(input.CombatSkills, 0);
        CombatRaiseIndexes(combat).CopyTo(input.CombatRaiseIndex, 0);
        AddCombatStat(
            new CandidateList(sheet.Game, input.Candidates),
            model,
            combat,
            input.CombatSkills,
            input.CombatRaiseIndex,
            stat);
        return input;
    }

    internal static bool SpellShown(BuffModelInput model, BuffKind kind) =>
        model.SpellKnown[(int)kind] && model.ShowBuffed[(int)kind];

    // Resistance or Immunity gains a tenth of the shown Armor Value.
    internal static bool ArmorFeeds(PlannerEffects effects, int skill) => skill switch
    {
        ResistSkill => effects.ArmorToResist != 0,
        ImmunitySkill => effects.ArmorToImmunity != 0,
        _ => false,
    };

    // The search buys pool points; the route shows them as the attribute points that are cheapest.
    internal static FocusRoute? SplitPool(PlannerGame game, FocusRoute? route)
    {
        if (route == null)
        {
            return null;
        }

        var raises = new List<FocusRaise>();
        foreach (var raise in route.Raises)
        {
            if (raise.RaiseIndex != BuffFocus.AttributePool)
            {
                raises.Add(raise);
                continue;
            }

            var stepCosts = PoolStepCosts(game);
            if (AttributePool.Cheapest(stepCosts, raise.Points) is not { } pick)
            {
                continue;
            }

            for (var attribute = 0; attribute < AttributeAmounts.Count; attribute++)
            {
                var points = pick.Points[attribute];
                if (points > 0)
                {
                    raises.Add(new FocusRaise(attribute, points, stepCosts[attribute].Take(points).Sum()));
                }
            }
        }

        return route with { Raises = raises };
    }

    private static bool FoolReversed(BuffModelInput model) =>
        TarotMath.Has(model.TarotCards, TarotCard.FoolReversed);

    private static int CostAfter(PlannerGame game, int slot, int extra) =>
        game.Plan.CostAfter(slot, game.ValuesFor(slot), extra) ?? NoCost;

    private static void FillCosts(PlannerGame game, int slot, int[] stepCosts)
    {
        for (var step = 0; step < stepCosts.Length; step++)
        {
            stepCosts[step] = CostAfter(game, slot, step);
        }
    }

    private static int[][] PoolStepCosts(PlannerGame game)
    {
        var stepCosts = new int[AttributeAmounts.Count][];
        for (var attribute = 0; attribute < stepCosts.Length; attribute++)
        {
            stepCosts[attribute] = new int[AttributePool.MostPoints];
            FillCosts(game, attribute, stepCosts[attribute]);
        }

        return stepCosts;
    }

    private static int[] SpellRaiseIndexes(BuffModelInput model) =>
        Enumerable.Range(0, BuffModelInput.BuffCount)
            .Select(kind => model.SpellKnown[kind]
                ? StatPlan.SkillSlot(PlannerSheet.SpellSkillNumbers[kind])
                : BuffFocus.NoSlot)
            .ToArray();

    private static int[] CombatRaiseIndexes(CombatModelInput combat) =>
        Enumerable.Range(0, CombatModel.SkillCount)
            .Select(skill => combat.Known[skill]
                ? StatPlan.SkillSlot(PlannerSheet.CombatSkillNumbers[skill])
                : BuffFocus.NoSlot)
            .ToArray();

    // The tree's extra percents add attributes the skill doesn't list, chosen by the skill's group.
    private static IEnumerable<AttributeSlot> ExtraPercentAttributes(BuffModelSkill skill, PlannerEffects effects) =>
        Enum.GetValues<AttributeSlot>()
            .Where(attribute => effects.SkillExtraPercent[attribute] != 0
                && (skill.Groups & ExtraPercentGroups[(int)attribute]) != 0);

    private static bool ItemValueTakesSpellBonus(PlannerEffects effects, CombatStat stat) => stat switch
    {
        CombatStat.Weapon => effects.SpellBonusToWeapon != 0,
        CombatStat.Armor => effects.SpellBonusToArmor != 0,
        _ => false,
    };

    // Determination and Evergreen add attributes to WV and AV; Brilliance adds them to the Spell Bonus behind Enhance,
    // Protect, Tachi and Odachi.
    private static bool AttributeSumFeedsItemValue(BuffModelInput model, CombatStat stat)
    {
        var effects = model.Effects;
        if (stat is not (CombatStat.Weapon or CombatStat.Armor))
        {
            return false;
        }

        if (effects.Determination != 0 || effects.DeterminationNodes != 0 || effects.Evergreen != 0)
        {
            return true;
        }

        if (effects.Brilliance == 0 && effects.BrillianceNodes == 0)
        {
            return false;
        }

        var itemBuff = stat == CombatStat.Weapon ? BuffKind.Enhance : BuffKind.Protect;
        return SpellShown(model, itemBuff) || ItemValueTakesSpellBonus(effects, stat);
    }

    private static bool SpellBonusFeeds(BuffModelInput model, CombatStat stat) => stat switch
    {
        CombatStat.Hit or CombatStat.Parry => SpellShown(model, BuffKind.Bless),
        CombatStat.Weapon => SpellShown(model, BuffKind.Enhance) || ItemValueTakesSpellBonus(model.Effects, stat),
        CombatStat.Armor => SpellShown(model, BuffKind.Protect) || ItemValueTakesSpellBonus(model.Effects, stat),
        _ => throw new UnreachableException(),
    };

    private static void AddPools(CandidateList candidates, PlannerEffects effects)
    {
        if (effects.HitpointsToSpellBonus != 0)
        {
            candidates.Add(BuffFocus.HitpointsRaise, FocusSearch.MostPoints);
        }

        if (effects.ManaToSpellBonus != 0 || effects.Flow != 0)
        {
            candidates.Add(BuffFocus.ManaRaise, FocusSearch.MostPoints);
        }
    }

    private static void AddSpell(CandidateList candidates, BuffModelInput model, BuffKind kind, bool attributePool)
    {
        if (!SpellShown(model, kind))
        {
            return;
        }

        candidates.Add(StatPlan.SkillSlot(PlannerSheet.SpellSkillNumbers[(int)kind]), FocusSearch.MostPoints);
        if (attributePool)
        {
            return;
        }

        candidates.AddAttributes(model.SpellSkills[(int)kind].Attributes);
        candidates.AddAptitude();
    }

    private static void AddCombatStat(
        CandidateList candidates,
        BuffModelInput model,
        CombatModelInput combat,
        BuffModelSkill[] combatSkills,
        int[] combatRaiseIndex,
        CombatStat stat)
    {
        var attributePool = FoolReversed(model);
        var affecting = CombatModel.SkillsAffecting(combat, stat, FocusSearch.MostPoints);
        var skills = Enum.GetValues<CombatSkill>().Where(affecting.Contains).ToArray();
        foreach (var skill in skills)
        {
            candidates.Add(combatRaiseIndex[(int)skill], FocusSearch.MostPoints);
        }

        if (attributePool)
        {
            candidates.AddAttributePool();
        }

        // Spells and pools go before the skills' attributes so the candidate cap drops attributes first.
        AddSpell(candidates, model, BuffKind.Bless, attributePool);
        if (stat == CombatStat.Weapon)
        {
            AddSpell(candidates, model, BuffKind.Enhance, attributePool);
        }

        if (stat == CombatStat.Armor)
        {
            AddSpell(candidates, model, BuffKind.Protect, attributePool);
        }

        if (SpellBonusFeeds(model, stat))
        {
            AddPools(candidates, model.Effects);
        }

        if (attributePool)
        {
            return;
        }

        foreach (var skill in skills)
        {
            candidates.AddAttributes(combatSkills[(int)skill].Attributes);
        }

        if (AttributeSumFeedsItemValue(model, stat))
        {
            candidates.AddAttributes(Enum.GetValues<AttributeSlot>());
        }

        // Introspection turns aptitude into the Spell Bonus that Tachi and Odachi add.
        if (model.Effects.Introspection != 0 && ItemValueTakesSpellBonus(model.Effects, stat))
        {
            candidates.AddAptitude();
        }
    }

    // Each raise slot once, up to the search's limit.
    private sealed class CandidateList(PlannerGame game, List<FocusCandidate> candidates)
    {
        public void Add(int raiseIndex, int mostPoints)
        {
            if (raiseIndex < 0 || IsFullOrHas(raiseIndex))
            {
                return;
            }

            var candidate = new FocusCandidate { RaiseIndex = raiseIndex, MostPoints = mostPoints };
            for (var step = 0; step < FocusSearch.MostPoints; step++)
            {
                candidate.StepCosts[step] = step < mostPoints ? CostAfter(game, raiseIndex, step) : NoCost;
            }

            candidates.Add(candidate);
        }

        public void AddAttributes(IEnumerable<AttributeSlot> attributes)
        {
            foreach (var attribute in attributes)
            {
                Add((int)attribute, SkillFocus.MostSteps);
            }
        }

        public void AddAptitude() => AddAttributes([AttributeSlot.Willpower, AttributeSlot.Intuition]);

        // The pool's nth point costs what buying n points cheapest-first costs over buying n - 1.
        public void AddAttributePool()
        {
            if (IsFullOrHas(BuffFocus.AttributePool))
            {
                return;
            }

            var stepCosts = PoolStepCosts(game);
            var candidate = new FocusCandidate
                { RaiseIndex = BuffFocus.AttributePool, MostPoints = FoolPoolMostPoints };
            var previousTotal = 0;
            for (var pointCount = 1; pointCount <= FocusSearch.MostPoints; pointCount++)
            {
                if (pointCount > FoolPoolMostPoints || AttributePool.Cheapest(stepCosts, pointCount) is not { } pick)
                {
                    candidate.StepCosts[pointCount - 1] = NoCost;
                    continue;
                }

                candidate.StepCosts[pointCount - 1] = pick.TotalCost - previousTotal;
                previousTotal = pick.TotalCost;
            }

            candidates.Add(candidate);
        }

        private bool IsFullOrHas(int raiseIndex) =>
            candidates.Count >= FocusSearch.MostCandidates
            || candidates.Exists(candidate => candidate.RaiseIndex == raiseIndex);
    }
}
