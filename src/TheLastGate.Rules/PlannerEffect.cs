using System;
using System.Diagnostics;
using TheLastGate.State;

namespace TheLastGate.Rules;

internal enum EffectField
{
    AttributePercent,
    SkillExtraPercent,
    HitPercent,
    ParryPercent,
    WeaponPercent,
    ArmorPercent,
    ImmunityPercent,
    ResistPercent,
    AptitudePercent,
    SpellBonusPercent,
    Determination,
    DeterminationNodes,
    Brilliance,
    BrillianceNodes,
    Introspection,
    GuardianAngel,
    Flow,
    SpellBonusEnchantPercent,
    ResistEnchantPercent,
    HitToParryPercent,
    ParryToHitPercent,
    ArmorEnchant,
    ArmorToResist,
    ArmorToImmunity,
    BraveIntoStrength,
    TravelersGuide,
    FistsOfHeavens,
    GrandeGrimoire,
    SpellBonusToWeapon,
    SpellBonusToArmor,
    Evergreen,
    HitpointsToSpellBonus,
    ManaToSpellBonus,
    Superbia,
}

[Flags]
internal enum AttributeMask
{
    None = 0,
    Braveness = 1 << AttributeSlot.Braveness,
    Willpower = 1 << AttributeSlot.Willpower,
    Intuition = 1 << AttributeSlot.Intuition,
    Agility = 1 << AttributeSlot.Agility,
    Strength = 1 << AttributeSlot.Strength,
    All = Braveness | Willpower | Intuition | Agility | Strength,
}

// One copy's worth of something the planner counts; the attributes say which ones a per-attribute percent applies to.
internal readonly record struct PlannerEffect(
    EffectField Field,
    int Amount,
    AttributeMask Attributes = AttributeMask.None)
{
    public void AddTo(PlannerEffects effects, int copies)
    {
        var amount = Amount * copies;
        switch (Field)
        {
            case EffectField.AttributePercent:
                AddPerAttribute(effects.AttributePercent, amount);
                break;
            case EffectField.SkillExtraPercent:
                AddPerAttribute(effects.SkillExtraPercent, amount);
                break;
            case EffectField.HitPercent:
                effects.HitPercent += amount;
                break;
            case EffectField.ParryPercent:
                effects.ParryPercent += amount;
                break;
            case EffectField.WeaponPercent:
                effects.WeaponPercent += amount;
                break;
            case EffectField.ArmorPercent:
                effects.ArmorPercent += amount;
                break;
            case EffectField.ImmunityPercent:
                effects.ImmunityPercent += amount;
                break;
            case EffectField.ResistPercent:
                effects.ResistPercent += amount;
                break;
            case EffectField.AptitudePercent:
                effects.AptitudePercent += amount;
                break;
            case EffectField.SpellBonusPercent:
                effects.SpellBonusPercent += amount;
                break;
            case EffectField.Determination:
                effects.Determination += amount;
                break;
            case EffectField.DeterminationNodes:
                effects.DeterminationNodes += amount;
                break;
            case EffectField.Brilliance:
                effects.Brilliance += amount;
                break;
            case EffectField.BrillianceNodes:
                effects.BrillianceNodes += amount;
                break;
            case EffectField.Introspection:
                effects.Introspection += amount;
                break;
            case EffectField.GuardianAngel:
                effects.GuardianAngel += amount;
                break;
            case EffectField.Flow:
                effects.Flow += amount;
                break;
            case EffectField.SpellBonusEnchantPercent:
                effects.SpellBonusEnchantPercent += amount;
                break;
            case EffectField.ResistEnchantPercent:
                effects.ResistEnchantPercent += amount;
                break;
            case EffectField.HitToParryPercent:
                effects.HitToParryPercent += amount;
                break;
            case EffectField.ParryToHitPercent:
                effects.ParryToHitPercent += amount;
                break;
            case EffectField.ArmorEnchant:
                effects.ArmorEnchant += amount;
                break;
            case EffectField.ArmorToResist:
                effects.ArmorToResist += amount;
                break;
            case EffectField.ArmorToImmunity:
                effects.ArmorToImmunity += amount;
                break;
            case EffectField.BraveIntoStrength:
                effects.BraveIntoStrength += amount;
                break;
            case EffectField.TravelersGuide:
                effects.TravelersGuide += amount;
                break;
            case EffectField.FistsOfHeavens:
                effects.FistsOfHeavens += amount;
                break;
            case EffectField.GrandeGrimoire:
                effects.GrandeGrimoire += amount;
                break;
            case EffectField.SpellBonusToWeapon:
                effects.SpellBonusToWeapon += amount;
                break;
            case EffectField.SpellBonusToArmor:
                effects.SpellBonusToArmor += amount;
                break;
            case EffectField.Evergreen:
                effects.Evergreen += amount;
                break;
            case EffectField.HitpointsToSpellBonus:
                effects.HitpointsToSpellBonus += amount;
                break;
            case EffectField.ManaToSpellBonus:
                effects.ManaToSpellBonus += amount;
                break;
            case EffectField.Superbia:
                effects.Superbia += amount;
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void AddPerAttribute(AttributeAmounts amounts, int amount)
    {
        foreach (var attribute in Enum.GetValues<AttributeSlot>())
        {
            if (Attributes.HasFlag((AttributeMask)(1 << (int)attribute)))
            {
                amounts[attribute] += amount;
            }
        }
    }
}
