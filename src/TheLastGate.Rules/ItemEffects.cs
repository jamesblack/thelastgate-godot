using System;
using System.Collections.Generic;
using TheLastGate.State;

namespace TheLastGate.Rules;

// In the order the Advanced window lists them.
public enum Enchant
{
    MoreBraveness,
    MoreWillpower,
    MoreIntuition,
    MoreAgility,
    MoreStrength,
    MoreAttributes,
    MoreSpellBonus,
    HitToParry,
    ParryToHit,
    MoreArmor,
    MoreResistance,
    ArmorToResistance,
    ArmorToImmunity,
}

// In the order the Advanced window lists them.
public enum Gear
{
    ImprovedVioletGaze,
    ImprovedLionsPaws,
    ImprovedCobaltLance,
    Haladie,
    ImprovedShivasScepter,
    Muramasa,
    ImprovedBarbarianSword,
    ImprovedFellNight,
    ImprovedAjaxBlue,
    TravelersGuide,
    FistsOfTheHeavens,
    GrandeGrimoire,
    ImprovedBlackTachi,
    ImprovedWhiteOdachi,
    Evergreen,
    GestureOfGorn,
    ImprovedNeisClaw,
    Superbia,
    CudgelOfKwai,
    Quicksilver,
    Excalibur,
}

public sealed record EnchantInfo(int ServerNumber, string Name, string OneCopy, bool Counted);

public sealed record GearInfo(string Name, string Effect, bool Modelled);

// The enchants and gear ticked in the Advanced window; an enchant's count is kept while it is unticked.
public sealed class ItemEffectsInput
{
    public uint EnchantTicks { get; set; }

    public int[] EnchantCounts { get; } = new int[ItemEffects.EnchantCount];

    public uint GearTicks { get; set; }
}

// What enchants and gear add, on top of the class tree and corruptors. The per-attribute, enchant and Kwai percents stop
// at 200, as on the server.
public static class ItemEffects
{
    public const int EnchantCount = 13;
    public const int GearCount = 21;
    public const int Cap = 200;

    private static readonly EnchantInfo[] EnchantTable =
    [
        new(6, "More Braveness", "+3% Braveness", true),
        new(17, "More Willpower", "+3% Willpower", true),
        new(27, "More Intuition", "+3% Intuition", true),
        new(36, "More Agility", "+3% Agility", true),
        new(44, "More Strength", "+3% Strength", true),
        new(84, "More attributes", "+2% attributes", true),
        new(87, "More Spell Bonus", "+10% Spell Bonus", true),
        new(96, "Hit to Parry", "3% Hit to Parry", true),
        new(97, "Parry to Hit", "3% Parry to Hit", true),
        new(2, "More Armor Value", "Chest: AV x1.1", false),
        new(21, "More Resistance", "Cloak: Res x1.1", false),
        new(16, "Armor to Resistance", "Chest: AV/10 Res", false),
        new(35, "Armor to Immunity", "Chest: AV/10 Imm", false),
    ];

    private static readonly GearInfo[] GearTable =
    [
        new("Improved Violet Gaze", "+4% Wil & Int", true),
        new("Improved Lion's Paws", "+20% Brv passive", true),
        new("Improved Cobalt Lance", "+20% Brv passive", true),
        new("Haladie", "+10% Wil spells", true),
        new("Improved Shiva's Scepter", "+10% Int spells", true),
        new("Muramasa", "+20% Agl melee", true),
        new("Improved Barbarian Sword", "+10% Str melee", true),
        new("Improved Fell Night", "+10% Str melee", true),
        new("Improved Ajax Blue", "Brv bonus and % to Str", true),
        new("Traveler's Guide", "Higher of Brv/Agl for effects", true),
        new("Fists of the Heavens", "Top attribute x1.2, best weapon skill", true),
        new("Grande Grimoire", "Top attribute x1.1", true),
        new("Improved Black Tachi", "Spell Bonus to WV", true),
        new("Improved White Odachi", "Spell Bonus to AV", true),
        new("Evergreen", "Agl/10 to WV, Str/10 to AV", true),
        new("Gesture of Gorn", "HP/100 to Spell Bonus", true),
        new("Improved Nei's Claw", "Mana/100 to Spell Bonus", true),
        new("Superbia", "Aptitude x0.1", true),
        new("Cudgel of Kwai", "Hit and Parry use the higher", false),
        new("Quicksilver", "Move speed/30 to Hit and Parry", false),
        new("Excalibur", "Attack speed/5 to WV", false),
    ];

    private static readonly (Enchant Enchant, PlannerEffect Effect)[] EnchantEffects =
    [
        (Enchant.MoreBraveness, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Braveness)),
        (Enchant.MoreWillpower, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Willpower)),
        (Enchant.MoreIntuition, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Intuition)),
        (Enchant.MoreAgility, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Agility)),
        (Enchant.MoreStrength, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Strength)),
        (Enchant.MoreAttributes, new PlannerEffect(EffectField.AttributePercent, 2, AttributeMask.All)),
        (Enchant.MoreSpellBonus, new PlannerEffect(EffectField.SpellBonusEnchantPercent, 10)),
        (Enchant.HitToParry, new PlannerEffect(EffectField.HitToParryPercent, 3)),
        (Enchant.ParryToHit, new PlannerEffect(EffectField.ParryToHitPercent, 3)),
        (Enchant.MoreArmor, new PlannerEffect(EffectField.ArmorEnchant, 1)),
        (Enchant.MoreResistance, new PlannerEffect(EffectField.ResistEnchantPercent, 10)),
        (Enchant.ArmorToResistance, new PlannerEffect(EffectField.ArmorToResist, 1)),
        (Enchant.ArmorToImmunity, new PlannerEffect(EffectField.ArmorToImmunity, 1)),
    ];

    private static readonly (Gear Gear, PlannerEffect Effect)[] GearEffects =
    [
        (Gear.ImprovedVioletGaze,
            new PlannerEffect(EffectField.AttributePercent, 4, AttributeMask.Willpower | AttributeMask.Intuition)),
        (Gear.ImprovedLionsPaws, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Braveness)),
        (Gear.ImprovedCobaltLance, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Braveness)),
        (Gear.Haladie, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Willpower)),
        (Gear.ImprovedShivasScepter, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Intuition)),
        (Gear.Muramasa, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Agility)),
        (Gear.ImprovedBarbarianSword, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Strength)),
        (Gear.ImprovedFellNight, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Strength)),
        (Gear.ImprovedAjaxBlue, new PlannerEffect(EffectField.BraveIntoStrength, 1)),
        (Gear.TravelersGuide, new PlannerEffect(EffectField.TravelersGuide, 1)),
        (Gear.FistsOfTheHeavens, new PlannerEffect(EffectField.FistsOfHeavens, 1)),
        (Gear.GrandeGrimoire, new PlannerEffect(EffectField.GrandeGrimoire, 1)),
        (Gear.ImprovedBlackTachi, new PlannerEffect(EffectField.SpellBonusToWeapon, 1)),
        (Gear.ImprovedWhiteOdachi, new PlannerEffect(EffectField.SpellBonusToArmor, 1)),
        (Gear.Evergreen, new PlannerEffect(EffectField.Evergreen, 1)),
        (Gear.GestureOfGorn, new PlannerEffect(EffectField.HitpointsToSpellBonus, 1)),
        (Gear.ImprovedNeisClaw, new PlannerEffect(EffectField.ManaToSpellBonus, 1)),
        (Gear.Superbia, new PlannerEffect(EffectField.Superbia, 1)),
    ];

    public static IReadOnlyList<EnchantInfo> Enchants => EnchantTable;

    public static IReadOnlyList<GearInfo> Gears => GearTable;

    // An enchant that is only a tickbox counts one copy whatever its count says.
    public static int EnchantCopies(ItemEffectsInput input, Enchant enchant)
    {
        if ((input.EnchantTicks & (1u << (int)enchant)) == 0)
        {
            return 0;
        }

        return EnchantTable[(int)enchant].Counted ? input.EnchantCounts[(int)enchant] : 1;
    }

    // Ajax Blue adds Braveness's percent to Strength's before the caps.
    public static PlannerEffects Compute(TreeEffectsInput tree, ItemEffectsInput items)
    {
        var effects = TreeEffects.Compute(tree);
        foreach (var (enchant, effect) in EnchantEffects)
        {
            effect.AddTo(effects, EnchantCopies(items, enchant));
        }

        foreach (var (gear, effect) in GearEffects)
        {
            if ((items.GearTicks & (1u << (int)gear)) != 0)
            {
                effect.AddTo(effects, 1);
            }
        }

        if (effects.BraveIntoStrength > 0)
        {
            effects.AttributePercent[AttributeSlot.Strength] += effects.AttributePercent[AttributeSlot.Braveness];
        }

        foreach (var attribute in Enum.GetValues<AttributeSlot>())
        {
            effects.AttributePercent[attribute] = Math.Min(effects.AttributePercent[attribute], Cap);
            effects.SkillExtraPercent[attribute] = Math.Min(effects.SkillExtraPercent[attribute], Cap);
        }

        effects.SpellBonusEnchantPercent = Math.Min(effects.SpellBonusEnchantPercent, Cap);
        effects.HitToParryPercent = Math.Min(effects.HitToParryPercent, Cap);
        effects.ParryToHitPercent = Math.Min(effects.ParryToHitPercent, Cap);
        effects.ResistEnchantPercent = Math.Min(effects.ResistEnchantPercent, Cap);
        return effects;
    }
}
