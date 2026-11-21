using TheLastGate.State;

namespace TheLastGate.Planner;

// Everything the class tree, corruptors, enchants and gear add to the planner's maths. Each amount is the total over
// every copy; the models later in the planner say how each one is used.
public sealed record PlannerEffects
{
    // An attribute's total becomes total x (100 + p) / 100.
    public AttributeAmounts AttributePercent { get; init; } = new();

    // Braveness: passive skills; Willpower and Intuition: active spells; Agility and Strength: active melee skills.
    public AttributeAmounts SkillExtraPercent { get; init; } = new();

    public int HitPercent { get; set; }

    public int ParryPercent { get; set; }

    public int WeaponPercent { get; set; }

    public int ArmorPercent { get; set; }

    public int ImmunityPercent { get; set; }

    public int ResistPercent { get; set; }

    public int AptitudePercent { get; set; }

    public int SpellBonusPercent { get; set; }

    // WV and AV gain determination x attribute sum / 150.
    public int Determination { get; set; }

    // WV and AV gain determination nodes x (attribute sum / 75).
    public int DeterminationNodes { get; set; }

    // Spell Bonus gains brilliance x attribute sum / 150.
    public int Brilliance { get; set; }

    // Spell Bonus gains brilliance nodes x (attribute sum / 75).
    public int BrillianceNodes { get; set; }

    // Spell Bonus gains introspection x aptitude / 50.
    public int Introspection { get; set; }

    // Bless power becomes the server's more(power, Braveness x guardian angel, 20).
    public int GuardianAngel { get; set; }

    // Spell Bonus x (100 + p) / 100, after the spell bonus percent.
    public int SpellBonusEnchantPercent { get; set; }

    // Resistance x (100 + p) / 100, after the resist percent and before the Hermit.
    public int ResistEnchantPercent { get; set; }

    public int HitToParryPercent { get; set; }

    public int ParryToHitPercent { get; set; }

    // AV x 11 / 10 after the tarot steps, before the armor percent.
    public int ArmorEnchant { get; set; }

    // Resistance and Immunity gain the final shown AV / 10.
    public int ArmorToResist { get; set; }

    public int ArmorToImmunity { get; set; }

    // Bonus Braveness, not base, is added to Strength before the Reverse Fool.
    public int BraveIntoStrength { get; set; }

    // The lower of Braveness and Agility counts as the higher one for effects.
    public int TravelersGuide { get; set; }

    // The highest attribute(s) x 6 / 5 after the percents; Hit and Parry use the best weapon skill.
    public int FistsOfHeavens { get; set; }

    // Then the highest attribute(s) x 11 / 10.
    public int GrandeGrimoire { get; set; }

    public int SpellBonusToWeapon { get; set; }

    public int SpellBonusToArmor { get; set; }

    // WV gains effect Agility / 10 and AV effect Strength / 10.
    public int Evergreen { get; set; }

    // Spell Bonus gains the uncapped pool / 100.
    public int HitpointsToSpellBonus { get; set; }

    public int ManaToSpellBonus { get; set; }

    // Aptitude becomes the server's less(aptitude, 90, 1) after the aptitude percent.
    public int Superbia { get; set; }

    // Spell Bonus gains uncapped Mana x flow / 200.
    public int Flow { get; set; }
}
