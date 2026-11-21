using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheLastGate.Buffs;
using TheLastGate.State;

namespace TheLastGate.Planner;

public enum SkillScale
{
    None,
    Resist,
    Tactics,
    Immunity,
}

public sealed record BuffModelSkill
{
    // The value the server sends.
    public int ServerTotal { get; set; }

    // Points planned for the skill itself.
    public int Planned { get; set; }

    public IReadOnlyList<AttributeSlot> Attributes { get; set; } = [];

    public SkillScale Scale { get; set; }

    public SkillGroups Groups { get; set; }

    public bool Equals(BuffModelSkill? other) =>
        other != null
        && (ServerTotal, Planned, Scale, Groups) == (other.ServerTotal, other.Planned, other.Scale, other.Groups)
        && Attributes.SequenceEqual(other.Attributes);

    public override int GetHashCode() => HashCode.Combine(ServerTotal, Planned, Scale, Groups);
}

// Equal to another input with the same numbers.
public sealed class BuffModelInput : IEquatable<BuffModelInput>
{
    public const int BuffCount = 3;

    // The totals the server sends.
    public AttributeAmounts ServerAttributes { get; } = new();

    // The bare values, without gear or buffs.
    public AttributeAmounts BaseAttributes { get; } = new();

    public AttributeAmounts PlannedAttributes { get; } = new();

    // 5, or 6 in hard mode.
    public int Divisor { get; set; } = 5;

    // In hundredths: 100 is 1.00x.
    public int SpellMultiplier { get; set; }

    public int SpellBonus { get; set; }

    public int SpellAptitude { get; set; }

    // A percent.
    public int RaceModifier { get; set; }

    // Enhance and Protect give power / 6 + 3 instead of power / 4 + 4.
    public bool ReducedItemBuffs { get; set; }

    public int ServerWeapon { get; set; }

    public int ServerArmor { get; set; }

    public bool[] SpellKnown { get; } = new bool[BuffCount];

    public BuffModelSkill[] SpellSkills { get; } = [new(), new(), new()];

    // A buff not on the player isn't Known.
    public BuffReading[] Active { get; } = [new(), new(), new()];

    public bool[] ShowBuffed { get; } = new bool[BuffCount];

    public ulong TarotCards { get; set; }

    public int UncappedHitpoints { get; set; }

    public int PlannedHitpoints { get; set; }

    public int UncappedMana { get; set; }

    public int PlannedMana { get; set; }

    public PlannerEffects Effects { get; set; } = new();

    public bool Equals(BuffModelInput? other) =>
        other != null
        && ServerAttributes.Equals(other.ServerAttributes)
        && BaseAttributes.Equals(other.BaseAttributes)
        && PlannedAttributes.Equals(other.PlannedAttributes)
        && (Divisor, SpellMultiplier, SpellBonus, SpellAptitude, RaceModifier, ReducedItemBuffs, ServerWeapon)
        == (other.Divisor, other.SpellMultiplier, other.SpellBonus, other.SpellAptitude, other.RaceModifier,
            other.ReducedItemBuffs, other.ServerWeapon)
        && (ServerArmor, TarotCards, UncappedHitpoints, PlannedHitpoints, UncappedMana, PlannedMana)
        == (other.ServerArmor, other.TarotCards, other.UncappedHitpoints, other.PlannedHitpoints, other.UncappedMana,
            other.PlannedMana)
        && SpellKnown.SequenceEqual(other.SpellKnown)
        && SpellSkills.SequenceEqual(other.SpellSkills)
        && Active.SequenceEqual(other.Active)
        && ShowBuffed.SequenceEqual(other.ShowBuffed)
        && Effects.Equals(other.Effects);

    // The attributes, flags and skills are copied, so planning on the copy leaves this one alone; the buff readings
    // and effects are shared, as nothing changes them.
    public BuffModelInput Copy()
    {
        var copy = new BuffModelInput
        {
            Divisor = Divisor,
            SpellMultiplier = SpellMultiplier,
            SpellBonus = SpellBonus,
            SpellAptitude = SpellAptitude,
            RaceModifier = RaceModifier,
            ReducedItemBuffs = ReducedItemBuffs,
            ServerWeapon = ServerWeapon,
            ServerArmor = ServerArmor,
            TarotCards = TarotCards,
            UncappedHitpoints = UncappedHitpoints,
            PlannedHitpoints = PlannedHitpoints,
            UncappedMana = UncappedMana,
            PlannedMana = PlannedMana,
            Effects = Effects,
        };
        foreach (var slot in Enum.GetValues<AttributeSlot>())
        {
            copy.ServerAttributes[slot] = ServerAttributes[slot];
            copy.BaseAttributes[slot] = BaseAttributes[slot];
            copy.PlannedAttributes[slot] = PlannedAttributes[slot];
        }

        for (var kind = 0; kind < BuffCount; kind++)
        {
            copy.SpellKnown[kind] = SpellKnown[kind];
            copy.SpellSkills[kind] = SpellSkills[kind] with { };
            copy.Active[kind] = Active[kind];
            copy.ShowBuffed[kind] = ShowBuffed[kind];
        }

        return copy;
    }

    public override bool Equals(object? other) => Equals(other as BuffModelInput);

    public override int GetHashCode() => HashCode.Combine(PlannedAttributes, SpellAptitude, TarotCards, Effects);
}

public sealed class BuffModelResult
{
    // The values the planner shows, plan included.
    public AttributeAmounts Attributes { get; internal set; } = new();

    // Aptitude with the planned base Willpower and Intuition.
    public int SpellAptitude { get; set; }

    // Spell Bonus with the shown attributes and planned aptitude.
    public int SpellBonus { get; set; }

    // The player's own planned cast, the second for Bless; 0 for a spell not known.
    public int[] CastPower { get; } = new int[BuffModelInput.BuffCount];

    // The power shown: the stronger of the cast and the active buff while shown, else 0.
    public int[] EffectivePower { get; } = new int[BuffModelInput.BuffCount];

    public int Weapon { get; set; }

    public int Armor { get; set; }
}

// What the planner shows with its buffs on or off. Bless is cast twice: first from the unbuffed (planned) stats, then
// from the stats the first cast gave, which raises the Bless skill through its own attributes. Spell power is
// (skill + Spell Bonus) x Spell Multiplier, capped at twice the Spell Aptitude, and aptitude only follows base Willpower
// and Intuition, so the attribute percents never reach it. Buffs already on the player are taken out of the server's
// totals first, using the power the buff list gave.
public static class BuffModel
{
    private const int PercentBase = 100;
    private const int ItemBonusCap = 127;
    private const int BlessBonusDivisor = 5;
    private const int ItemBonusDivisor = 4;
    private const int ReducedItemBonusDivisor = 6;
    private const int MoonManaPerPercent = 50;
    private const int HermitResistNumerator = 4;
    private const int HermitResistDenominator = 5;
    private const int AptitudeAttributeFactor = 5;
    private const int IntrospectionAptitudeDivisor = 50;
    private const int GuardianAngelDivisor = 20;
    private const int MaxBlessRecasts = 20;
    private const int FistsNumerator = 6;
    private const int FistsDenominator = 5;
    private const int GrimoireNumerator = 11;
    private const int GrimoireDenominator = 10;
    private const int SuperbiaPercentCut = 90;
    private const int ArmorShareDivisor = 10;
    private const int EvergreenDivisor = 10;
    private const int PoolPerSpellBonus = 100;
    private const int FlowManaPerSpellBonus = 200;
    private const int NodeAttributeSumDivisor = 75;
    private const int CopyAttributeSumDivisor = 150;

    private const int Bless = (int)BuffKind.Bless;
    private const int Enhance = (int)BuffKind.Enhance;
    private const int Protect = (int)BuffKind.Protect;

    private static readonly AttributeSlot[] Slots = Enum.GetValues<AttributeSlot>();

    public static int BlessBonus(int power, AttributeSlot attribute) =>
        power <= 0 ? 0 : Math.Min(ItemBonusCap, (power * 2 / 3 - (int)attribute) / BlessBonusDivisor + 3);

    public static int ItemBonus(int power, bool reduced)
    {
        if (power <= 0)
        {
            return 0;
        }

        return Math.Min(ItemBonusCap, reduced ? power / ReducedItemBonusDivisor + 3 : power / ItemBonusDivisor + 4);
    }

    // The skill's value with these attribute values, its own planned points included.
    public static int SkillValue(BuffModelInput input, BuffModelSkill skill, AttributeAmounts attributes) =>
        SkillValueWithArmor(input, skill, attributes, input.ServerArmor);

    public static int SkillValueWithArmor(
        BuffModelInput input,
        BuffModelSkill skill,
        AttributeAmounts attributes,
        int shownArmor)
    {
        var effects = input.Effects;
        var change = skill.Planned + AttributeTerm(input, skill, attributes)
            - AttributeTerm(input, skill, input.ServerAttributes);
        return skill.Scale switch
        {
            SkillScale.Resist => ResistanceValue(
                input,
                skill,
                effects.ResistPercent,
                effects.ResistEnchantPercent,
                effects.ArmorToResist != 0,
                shownArmor,
                change),
            SkillScale.Immunity => ResistanceValue(
                input,
                skill,
                effects.ImmunityPercent,
                0,
                effects.ArmorToImmunity != 0,
                shownArmor,
                change),
            SkillScale.Tactics when TarotMath.Has(input.TarotCards, TarotCard.MoonReversed) =>
                MoonScaledTactics(input, skill.ServerTotal, change),
            SkillScale.None or SkillScale.Tactics => skill.ServerTotal + change,
            _ => throw new UnreachableException(),
        };
    }

    public static BuffModelResult Compute(BuffModelInput input)
    {
        var effects = input.Effects;
        var result = new BuffModelResult();
        var unbuffedChanges = new AttributeAmounts();
        foreach (var slot in Slots)
        {
            unbuffedChanges[slot] = input.PlannedAttributes[slot] - ActiveBlessBonus(input.Active[Bless], slot);
        }

        var unbuffed = AttributesWithChanges(input, unbuffedChanges);
        result.SpellAptitude = PlannedAptitude(input, unbuffed);
        ComputeBless(input, result, unbuffedChanges, unbuffed);
        result.SpellBonus = PlannedSpellBonus(input, result.Attributes, result.SpellAptitude);
        result.Weapon = ComputeItemBuff(input, result, Enhance, input.ServerWeapon, input.Active[Enhance].WeaponBonus);
        result.Armor = ComputeItemBuff(input, result, Protect, input.ServerArmor, input.Active[Protect].ArmorBonus);

        var determination = AttributeSumBonusChange(
            input,
            effects.DeterminationNodes,
            effects.Determination,
            result.Attributes);
        var spellBonusChange = result.SpellBonus - input.SpellBonus;
        result.Weapon += determination + EvergreenChange(input, result.Attributes, AttributeSlot.Agility)
            + (effects.SpellBonusToWeapon != 0 ? spellBonusChange : 0);
        result.Armor += determination + EvergreenChange(input, result.Attributes, AttributeSlot.Strength)
            + (effects.SpellBonusToArmor != 0 ? spellBonusChange : 0);
        return result;
    }

    // Hit, Parry, Weapon and Armor Value as the planner shows them: the combat skills at their planned and buffed
    // values, on top of the buffed Weapon and Armor Value, then the tarot cards and item effects.
    public static CombatValues CombatShown(
        BuffModelInput model,
        BuffModelResult buffed,
        CombatModelInput combat,
        IReadOnlyList<BuffModelSkill> combatSkills)
    {
        var planned = combat.Copy();
        for (var skill = 0; skill < CombatModel.SkillCount; skill++)
        {
            if (planned.Known[skill])
            {
                planned.Shown[skill] = SkillValue(model, combatSkills[skill], buffed.Attributes);
            }
        }

        planned.BaseWeapon = buffed.Weapon;
        planned.BaseArmor = buffed.Armor;
        var raw = CombatModel.Compute(planned);
        var server = new CombatValues(combat.ServerHit, combat.ServerParry, model.ServerWeapon, model.ServerArmor);
        var rawChange = new CombatValues(
            raw.Hit - server.Hit,
            raw.Parry - server.Parry,
            raw.Weapon - server.Weapon,
            raw.Armor - server.Armor);
        return TarotMath.CombatShown(model.TarotCards, model.Effects, server, rawChange);
    }

    private static int AttributeSum(BuffModelSkill skill, AttributeAmounts attributes) =>
        skill.Attributes.Sum(attribute => attributes[attribute]);

    private static int SkillExtraBonus(BuffModelInput input, BuffModelSkill skill, AttributeAmounts attributes)
    {
        var percent = input.Effects.SkillExtraPercent;
        var extra = 0;
        if (skill.Groups.HasFlag(SkillGroups.Passive))
        {
            extra += percent[AttributeSlot.Braveness] * attributes[AttributeSlot.Braveness];
        }

        if (skill.Groups.HasFlag(SkillGroups.ActiveSpell))
        {
            extra += percent[AttributeSlot.Willpower] * attributes[AttributeSlot.Willpower]
                + percent[AttributeSlot.Intuition] * attributes[AttributeSlot.Intuition];
        }

        if (skill.Groups.HasFlag(SkillGroups.ActiveMelee))
        {
            extra += percent[AttributeSlot.Agility] * attributes[AttributeSlot.Agility]
                + percent[AttributeSlot.Strength] * attributes[AttributeSlot.Strength];
        }

        return extra;
    }

    private static int AttributeTerm(BuffModelInput input, BuffModelSkill skill, AttributeAmounts attributes) =>
        (AttributeSum(skill, attributes) + SkillExtraBonus(input, skill, attributes) / PercentBase) / input.Divisor;

    private static int ScaledForward(int rawValue, ScaleStep[] steps) =>
        steps.Aggregate(rawValue, (value, step) => value * step.Numerator / step.Denominator);

    // The server rounds down after each step. The raw value is found in one division, which loses less to rounding
    // than undoing the steps one at a time.
    private static int SteppedValue(int serverTotal, int change, ScaleStep[] steps)
    {
        var numerator = steps.Aggregate(1, (product, step) => product * step.Numerator);
        var denominator = steps.Aggregate(1, (product, step) => product * step.Denominator);
        var rawValue = serverTotal * denominator / numerator;
        return serverTotal + ScaledForward(rawValue + change, steps) - ScaledForward(rawValue, steps);
    }

    // The skill's percent, then the Resistance enchant, then the Hermit; the armor share is added after all of them.
    private static int ResistanceValue(
        BuffModelInput input,
        BuffModelSkill skill,
        int percent,
        int enchantPercent,
        bool armorShare,
        int shownArmor,
        int change)
    {
        ScaleStep[] steps =
        [
            ScaleStep.Percent(percent), ScaleStep.Percent(enchantPercent),
            ScaleStep.Optional(
                TarotMath.Has(input.TarotCards, TarotCard.Hermit),
                HermitResistNumerator,
                HermitResistDenominator),
        ];
        var serverShare = armorShare ? input.ServerArmor / ArmorShareDivisor : 0;
        var shownShare = armorShare ? shownArmor / ArmorShareDivisor : 0;
        return SteppedValue(skill.ServerTotal - serverShare, change, steps) + shownShare;
    }

    // The Moon Reversed raises Tactics by a percent for every 50 uncapped Mana.
    private static int MoonScaledTactics(BuffModelInput input, int serverTotal, int change)
    {
        var percentScale = PercentBase * MoonManaPerPercent;
        var rawValue = serverTotal * percentScale / (percentScale + input.UncappedMana);
        return (rawValue + change) * (percentScale + input.UncappedMana + input.PlannedMana) / percentScale;
    }

    private static int Highest(AttributeAmounts values) => Slots.Max(slot => values[slot]);

    private static void BoostHighest(AttributeAmounts values, int numerator, int denominator)
    {
        var highest = Highest(values);
        foreach (var slot in Slots.Where(slot => values[slot] == highest))
        {
            values[slot] = values[slot] * numerator / denominator;
        }
    }

    // Rounds up to the smallest value that boosts back to this one; rounding down could tie it with a lower attribute.
    private static void UnboostHighest(AttributeAmounts values, int numerator, int denominator)
    {
        var highest = Highest(values);
        foreach (var slot in Slots.Where(slot => values[slot] == highest))
        {
            values[slot] = (values[slot] * denominator + numerator - 1) / numerator;
        }
    }

    private static AttributeAmounts Boosted(BuffModelInput input, AttributeAmounts values)
    {
        var boosted = values.Copy();
        if (input.Effects.FistsOfHeavens != 0)
        {
            BoostHighest(boosted, FistsNumerator, FistsDenominator);
        }

        if (input.Effects.GrandeGrimoire != 0)
        {
            BoostHighest(boosted, GrimoireNumerator, GrimoireDenominator);
        }

        return boosted;
    }

    private static AttributeAmounts ServerAttributesBeforeBoosts(BuffModelInput input)
    {
        var attributes = input.ServerAttributes.Copy();
        if (input.Effects.GrandeGrimoire != 0)
        {
            UnboostHighest(attributes, GrimoireNumerator, GrimoireDenominator);
        }

        if (input.Effects.FistsOfHeavens != 0)
        {
            UnboostHighest(attributes, FistsNumerator, FistsDenominator);
        }

        return attributes;
    }

    // Improved Ajax Blue adds the bonus Braveness to Strength, but not the base Braveness the plan raises. Under the
    // Reverse Fool the server averages all five attributes, so only the sum of the changes matters, and the attribute
    // percent comes after that averaging. Fists of the Heavens and the Grande Grimoire then boost whichever
    // attributes are highest.
    private static AttributeAmounts AttributesWithChanges(BuffModelInput input, AttributeAmounts changes)
    {
        var changesWithAjax = changes.Copy();
        if (input.Effects.BraveIntoStrength != 0)
        {
            changesWithAjax[AttributeSlot.Strength] +=
                changes[AttributeSlot.Braveness] - input.PlannedAttributes[AttributeSlot.Braveness];
        }

        var changeSum = Slots.Sum(slot => changesWithAjax[slot]);
        var beforeBoosts = ServerAttributesBeforeBoosts(input);
        var planned = new AttributeAmounts();
        foreach (var slot in Slots)
        {
            var serverValue = beforeBoosts[slot];
            var percentScale = PercentBase + input.Effects.AttributePercent[slot];
            var unscaledValue = serverValue * PercentBase / percentScale;
            var rawChange = TarotMath.Has(input.TarotCards, TarotCard.FoolReversed)
                ? TarotMath.FoolReversedTotal(unscaledValue, changeSum) - unscaledValue
                : changesWithAjax[slot];
            planned[slot] = serverValue + TarotMath.ScaledChange(serverValue, rawChange, percentScale, PercentBase);
        }

        var boostedPlanned = Boosted(input, planned);
        var boostedServer = Boosted(input, beforeBoosts);
        var attributes = new AttributeAmounts();
        foreach (var slot in Slots)
        {
            attributes[slot] = input.ServerAttributes[slot] + boostedPlanned[slot] - boostedServer[slot];
        }

        return attributes;
    }

    private static int AptitudeTerm(BuffModelInput input, int willpower, int intuition) =>
        (willpower + intuition) * AptitudeAttributeFactor / input.Divisor * input.RaceModifier / PercentBase;

    // The Magician gives Intuition's effects Strength's base when Strength's total is higher.
    private static int AptitudeIntuition(BuffModelInput input, AttributeAmounts totals, bool withPlan)
    {
        var source = TarotMath.Has(input.TarotCards, TarotCard.Magician)
            && totals[AttributeSlot.Strength] > totals[AttributeSlot.Intuition]
                ? AttributeSlot.Strength
                : AttributeSlot.Intuition;
        return input.BaseAttributes[source] + (withPlan ? input.PlannedAttributes[source] : 0);
    }

    private static int PlannedAptitude(BuffModelInput input, AttributeAmounts unbuffed)
    {
        var baseWillpower = input.BaseAttributes[AttributeSlot.Willpower];
        var change = AptitudeTerm(
                input,
                baseWillpower + input.PlannedAttributes[AttributeSlot.Willpower],
                AptitudeIntuition(input, unbuffed, true))
            - AptitudeTerm(input, baseWillpower, AptitudeIntuition(input, input.ServerAttributes, false));
        ScaleStep[] steps =
        [
            ScaleStep.Percent(input.Effects.AptitudePercent),
            ScaleStep.Optional(input.Effects.Superbia != 0, PercentBase - SuperbiaPercentCut, PercentBase),
        ];
        return SteppedValue(input.SpellAptitude, change, steps);
    }

    // What the WV, AV and Spell Bonus effects read: the Traveler's Guide lets the higher of Braveness and Agility stand
    // in for the lower one, and the Magician does the same with Strength and Intuition. Skills still read the
    // attributes themselves.
    private static AttributeAmounts EffectAttributes(BuffModelInput input, AttributeAmounts attributes)
    {
        var effective = attributes.Copy();
        if (input.Effects.TravelersGuide != 0)
        {
            LiftLower(attributes, effective, AttributeSlot.Braveness, AttributeSlot.Agility);
        }

        if (TarotMath.Has(input.TarotCards, TarotCard.Magician))
        {
            LiftLower(attributes, effective, AttributeSlot.Intuition, AttributeSlot.Strength);
        }

        return effective;
    }

    private static void LiftLower(
        AttributeAmounts attributes,
        AttributeAmounts effective,
        AttributeSlot first,
        AttributeSlot second)
    {
        if (attributes[second] > attributes[first])
        {
            effective[first] = attributes[second];
        }
        else
        {
            effective[second] = attributes[first];
        }
    }

    private static int EffectAttributeSum(BuffModelInput input, AttributeAmounts attributes)
    {
        var effective = EffectAttributes(input, attributes);
        return Slots.Sum(slot => effective[slot]);
    }

    // The tree node and the corruptor copies each round down on their own.
    private static int AttributeSumBonus(int nodes, int copies, int sum) =>
        nodes * (sum / NodeAttributeSumDivisor) + copies * sum / CopyAttributeSumDivisor;

    private static int AttributeSumBonusChange(
        BuffModelInput input,
        int nodes,
        int copies,
        AttributeAmounts attributes) =>
        AttributeSumBonus(nodes, copies, EffectAttributeSum(input, attributes))
        - AttributeSumBonus(nodes, copies, EffectAttributeSum(input, input.ServerAttributes));

    private static int PoolSpellBonusChange(int weight, int divisor, int uncappedPool, int plannedPool) =>
        (uncappedPool + plannedPool) * weight / divisor - uncappedPool * weight / divisor;

    // Brilliance, Introspection, Flow and the pool gear are flat additions that the Spell Bonus percents then raise.
    private static int PlannedSpellBonus(BuffModelInput input, AttributeAmounts attributes, int aptitude)
    {
        var effects = input.Effects;
        var change = AttributeSumBonusChange(input, effects.BrillianceNodes, effects.Brilliance, attributes)
            + effects.Introspection * aptitude / IntrospectionAptitudeDivisor
            - effects.Introspection * input.SpellAptitude / IntrospectionAptitudeDivisor
            + PoolSpellBonusChange(
                effects.HitpointsToSpellBonus != 0 ? 1 : 0,
                PoolPerSpellBonus,
                input.UncappedHitpoints,
                input.PlannedHitpoints)
            + PoolSpellBonusChange(
                effects.ManaToSpellBonus != 0 ? 1 : 0,
                PoolPerSpellBonus,
                input.UncappedMana,
                input.PlannedMana)
            + PoolSpellBonusChange(effects.Flow, FlowManaPerSpellBonus, input.UncappedMana, input.PlannedMana);
        ScaleStep[] steps =
            [ScaleStep.Percent(effects.SpellBonusPercent), ScaleStep.Percent(effects.SpellBonusEnchantPercent)];
        return SteppedValue(input.SpellBonus, change, steps);
    }

    // The Star Reversed takes the Spell Bonus out of the cast power.
    private static int CastPower(BuffModelInput input, int skillValue, int aptitude, int plannedBonus)
    {
        var spellBonus = TarotMath.Has(input.TarotCards, TarotCard.StarReversed) ? 0 : plannedBonus;
        var power = Math.Min((skillValue + spellBonus) * input.SpellMultiplier / PercentBase, 2 * aptitude);
        return Math.Max(0, power);
    }

    // Each Bless cast reads the Spell Bonus and Braveness its attributes give; Guardian Angel comes after the cap.
    private static int BlessCastPower(BuffModelInput input, int aptitude, AttributeAmounts attributes)
    {
        var scale = PercentBase * GuardianAngelDivisor;
        var power = CastPower(
            input,
            SkillValue(input, input.SpellSkills[Bless], attributes),
            aptitude,
            PlannedSpellBonus(input, attributes, aptitude));
        return power * (scale + attributes[AttributeSlot.Braveness] * input.Effects.GuardianAngel) / scale;
    }

    private static int ActiveBlessBonus(BuffReading active, AttributeSlot slot)
    {
        if (!active.Known)
        {
            return 0;
        }

        return active.HasExact ? active.AttributeBonus[slot] : BlessBonus(active.Power, slot);
    }

    private static int ActiveItemBonus(BuffReading active, int exactBonus, bool reduced)
    {
        if (!active.Known)
        {
            return 0;
        }

        return active.HasExact ? exactBonus : ItemBonus(active.Power, reduced);
    }

    private static int ShownPower(BuffModelInput input, BuffModelResult result, int kind)
    {
        var activePower = input.Active[kind].Known ? input.Active[kind].Power : 0;
        return input.ShowBuffed[kind] ? Math.Max(result.CastPower[kind], activePower) : 0;
    }

    private static AttributeAmounts AttributesWithBless(
        BuffModelInput input,
        AttributeAmounts unbuffedChanges,
        int power)
    {
        var changes = new AttributeAmounts();
        foreach (var slot in Slots)
        {
            changes[slot] = unbuffedChanges[slot] + BlessBonus(power, slot);
        }

        return AttributesWithChanges(input, changes);
    }

    // The server casts Bless from the Bless skill you have, which an active Bless has already raised, so each recast
    // can come out stronger than the last until it settles.
    private static int SettledBlessPower(
        BuffModelInput input,
        int aptitude,
        AttributeAmounts unbuffedChanges,
        AttributeAmounts unbuffed)
    {
        var power = BlessCastPower(input, aptitude, unbuffed);
        for (var recast = 0; recast < MaxBlessRecasts; recast++)
        {
            var recastPower = BlessCastPower(input, aptitude, AttributesWithBless(input, unbuffedChanges, power));
            if (recastPower <= power)
            {
                break;
            }

            power = recastPower;
        }

        return power;
    }

    private static void ComputeBless(
        BuffModelInput input,
        BuffModelResult result,
        AttributeAmounts unbuffedChanges,
        AttributeAmounts unbuffed)
    {
        result.CastPower[Bless] = 0;
        if (input.SpellKnown[Bless])
        {
            result.CastPower[Bless] = SettledBlessPower(input, result.SpellAptitude, unbuffedChanges, unbuffed);
        }

        result.EffectivePower[Bless] = ShownPower(input, result, Bless);
        result.Attributes = AttributesWithBless(input, unbuffedChanges, result.EffectivePower[Bless]);
    }

    // Enhance or Protect, cast after Bless so their skills see the shown attributes.
    private static int ComputeItemBuff(
        BuffModelInput input,
        BuffModelResult result,
        int kind,
        int serverValue,
        int exactBonus)
    {
        result.CastPower[kind] = 0;
        if (input.SpellKnown[kind])
        {
            result.CastPower[kind] = CastPower(
                input,
                SkillValue(input, input.SpellSkills[kind], result.Attributes),
                result.SpellAptitude,
                result.SpellBonus);
        }

        result.EffectivePower[kind] = ShownPower(input, result, kind);
        return serverValue - ActiveItemBonus(input.Active[kind], exactBonus, input.ReducedItemBuffs)
            + ItemBonus(result.EffectivePower[kind], input.ReducedItemBuffs);
    }

    private static int EvergreenChange(BuffModelInput input, AttributeAmounts attributes, AttributeSlot slot)
    {
        if (input.Effects.Evergreen == 0)
        {
            return 0;
        }

        return EffectAttributes(input, attributes)[slot] / EvergreenDivisor
            - EffectAttributes(input, input.ServerAttributes)[slot] / EvergreenDivisor;
    }

    private readonly record struct ScaleStep(int Numerator, int Denominator)
    {
        public static ScaleStep Percent(int percent) => new(PercentBase + percent, PercentBase);

        public static ScaleStep Optional(bool applies, int numerator, int denominator) =>
            applies ? new ScaleStep(numerator, denominator) : new ScaleStep(1, 1);
    }
}
