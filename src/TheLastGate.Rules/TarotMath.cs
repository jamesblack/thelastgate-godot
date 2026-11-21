using System.Collections.Generic;
using System.Linq;

namespace TheLastGate.Rules;

// A card's number is its bit in a tarot set; only these cards change anything the planner shows.
public enum TarotCard
{
    Magician = 1,
    Lovers = 6,
    Hermit = 9,
    FoolReversed = 22,
    LoversReversed = 28,
    StrengthReversed = 30,
    HangedReversed = 34,
    TemperanceReversed = 36,
    StarReversed = 39,
    MoonReversed = 40,
}

// How the tarot cards, and the percents from the tree, corruptors and items, turn a raw change in the combat numbers
// into the change the server would show. The server's value already has every step in it, so each step works out the
// raw value it came from, applies the change there, and scales the difference again.
public static class TarotMath
{
    public const int CardCount = 44;
    public const int TemperanceSicknessStacks = 4;
    public const int FoolPointsPerBreakpoint = 5;

    private const int PercentBase = 100;
    private const int FoolBonusDivisor = 20;
    private const int StrengthReversedHitNumerator = 4;
    private const int StrengthReversedHitDenominator = 5;
    private const int StrengthReversedWeaponNumerator = 6;
    private const int StrengthReversedWeaponDenominator = 5;
    private const int HermitArmorNumerator = 23;
    private const int HermitArmorDenominator = 20;
    private const int ArmorEnchantNumerator = 11;
    private const int ArmorEnchantDenominator = 10;
    private const int HangedReversedWeaponPercent = 85;
    private const int TemperanceReversedPercentPerStack = 10;

    private static readonly string[] UprightNames =
    [
        "The Fool", "The Magician", "The High Priestess", "The Empress", "The Emperor", "The Hierophant", "The Lovers",
        "The Chariot", "Strength", "The Hermit", "Wheel of Fortune", "Justice", "The Hanged Man", "Death", "Temperance",
        "The Devil", "The Tower", "The Star", "The Moon", "The Sun", "Judgement", "The World",
    ];

    private static readonly TarotCard[] Modelled =
    [
        TarotCard.Magician, TarotCard.Lovers, TarotCard.Hermit, TarotCard.FoolReversed, TarotCard.LoversReversed,
        TarotCard.StrengthReversed, TarotCard.HangedReversed, TarotCard.TemperanceReversed, TarotCard.StarReversed,
        TarotCard.MoonReversed,
    ];

    public static IReadOnlyList<TarotCard> ModelledCards => Modelled;

    // By card number: the 22 cards upright, then reversed.
    public static IReadOnlyList<string> CardNames { get; } =
        UprightNames.Concat(UprightNames.Select(name => name + ", Reversed")).ToArray();

    public static bool Has(ulong cards, int cardIndex) =>
        cardIndex is >= 0 and < CardCount && ((cards >> cardIndex) & 1) != 0;

    public static bool Has(ulong cards, TarotCard card) => Has(cards, (int)card);

    public static int FloorDivide(int dividend, int divisor)
    {
        var quotient = dividend / divisor;
        return dividend % divisor != 0 && dividend < 0 != divisor < 0 ? quotient - 1 : quotient;
    }

    // The Reverse Fool shares the attribute points out and adds a twentieth, so the total only moves when the sum of
    // the planned points crosses a breakpoint of five.
    public static int FoolReversedTotal(int shownTotal, int sumChange)
    {
        var share = FoolShareFor(shownTotal);
        var newShare = share + FloorDivide(sumChange, FoolPointsPerBreakpoint);
        return shownTotal + FoolTotalFor(newShare) - FoolTotalFor(share);
    }

    public static int ScaledChange(int serverValue, int rawChange, int numerator, int denominator)
    {
        var rawValue = serverValue * denominator / numerator;
        return (rawValue + rawChange) * numerator / denominator - rawValue * numerator / denominator;
    }

    // In the server's order: the Kwai enchants are taken off first and put back last, Hit and Parry go through the
    // Lovers Reversed, their percents and Strength Reversed, and Weapon and Armor Value through the Lovers, the Hermit,
    // the armor enchant, one combined weapon factor and their percents.
    public static CombatValues CombatShown(
        ulong cards,
        PlannerEffects effects,
        CombatValues server,
        CombatValues rawChange)
    {
        var change = rawChange;
        var beforeKwai = WithoutKwai(effects, server);
        if (Has(cards, TarotCard.LoversReversed))
        {
            var average = FloorDivide(change.Hit + change.Parry, 2);
            change = change with { Hit = average, Parry = average };
        }

        change = change with
        {
            Hit = PercentScaledChange(beforeKwai.Hit, change.Hit, effects.HitPercent),
            Parry = PercentScaledChange(beforeKwai.Parry, change.Parry, effects.ParryPercent),
        };
        if (Has(cards, TarotCard.StrengthReversed))
        {
            change = change with
            {
                Hit = ScaledChange(
                    beforeKwai.Hit,
                    change.Hit,
                    StrengthReversedHitNumerator,
                    StrengthReversedHitDenominator),
            };
        }

        if (Has(cards, TarotCard.Lovers))
        {
            var average = FloorDivide(change.Weapon + change.Armor, 2);
            change = change with { Weapon = average, Armor = average };
        }

        if (Has(cards, TarotCard.Hermit))
        {
            change = change with
            {
                Armor = ScaledChange(server.Armor, change.Armor, HermitArmorNumerator, HermitArmorDenominator),
            };
        }

        if (effects.ArmorEnchant != 0)
        {
            change = change with
            {
                Armor = ScaledChange(server.Armor, change.Armor, ArmorEnchantNumerator, ArmorEnchantDenominator),
            };
        }

        var (weaponNumerator, weaponDenominator) = WeaponFactor(cards);
        if (weaponNumerator != weaponDenominator)
        {
            change = change with
            {
                Weapon = ScaledChange(server.Weapon, change.Weapon, weaponNumerator, weaponDenominator),
            };
        }

        change = change with
        {
            Weapon = PercentScaledChange(server.Weapon, change.Weapon, effects.WeaponPercent),
            Armor = PercentScaledChange(server.Armor, change.Armor, effects.ArmorPercent),
        };
        var shown = WithKwai(
            effects,
            new CombatValues(beforeKwai.Hit + change.Hit, beforeKwai.Parry + change.Parry, 0, 0));
        return shown with { Weapon = server.Weapon + change.Weapon, Armor = server.Armor + change.Armor };
    }

    private static int FoolTotalFor(int share) => share + share / FoolBonusDivisor;

    private static int FoolShareFor(int shownTotal)
    {
        var share = shownTotal * FoolBonusDivisor / (FoolBonusDivisor + 1);
        while (FoolTotalFor(share + 1) <= shownTotal)
        {
            share++;
        }

        while (share > 0 && FoolTotalFor(share) > shownTotal)
        {
            share--;
        }

        return share;
    }

    private static int PercentScaledChange(int serverValue, int rawChange, int increasedPercent) =>
        increasedPercent == 0
            ? rawChange
            : ScaledChange(serverValue, rawChange, PercentBase + increasedPercent, PercentBase);

    // Strength Reversed, Hanged Reversed and Temperance Reversed's four stacks scale Weapon Value as one step.
    private static (int Numerator, int Denominator) WeaponFactor(ulong cards)
    {
        var numerator = 1;
        var denominator = 1;
        if (Has(cards, TarotCard.StrengthReversed))
        {
            numerator *= StrengthReversedWeaponNumerator;
            denominator *= StrengthReversedWeaponDenominator;
        }

        if (Has(cards, TarotCard.HangedReversed))
        {
            numerator *= HangedReversedWeaponPercent;
            denominator *= PercentBase;
        }

        if (Has(cards, TarotCard.TemperanceReversed))
        {
            numerator *= PercentBase + TemperanceReversedPercentPerStack * TemperanceSicknessStacks;
            denominator *= PercentBase;
        }

        return (numerator, denominator);
    }

    // Parry gains its share of Hit first, then Hit gains its share of that raised Parry.
    private static CombatValues WithKwai(PlannerEffects effects, CombatValues values)
    {
        var parry = values.Parry + values.Hit * effects.HitToParryPercent / PercentBase;
        var hit = values.Hit + parry * effects.ParryToHitPercent / PercentBase;
        return values with { Hit = hit, Parry = parry };
    }

    private static CombatValues WithoutKwai(PlannerEffects effects, CombatValues values)
    {
        var hit = values.Hit - values.Parry * effects.ParryToHitPercent / PercentBase;
        var parry = values.Parry - hit * effects.HitToParryPercent / PercentBase;
        return values with { Hit = hit, Parry = parry };
    }
}
