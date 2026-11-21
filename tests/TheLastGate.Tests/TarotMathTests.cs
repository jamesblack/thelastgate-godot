using TheLastGate.Rules;

namespace TheLastGate.Tests;

public class TarotMathTests
{
    [Fact]
    public void Has_ReadsTheCardsBit()
    {
        var cards = Cards(TarotCard.Hermit) | (1ul << 43);

        Assert.True(TarotMath.Has(cards, TarotCard.Hermit));
        Assert.True(TarotMath.Has(cards, 43));
        Assert.False(TarotMath.Has(cards, TarotCard.Lovers));
        Assert.False(TarotMath.Has(cards, -1));
        Assert.False(TarotMath.Has(cards, TarotMath.CardCount));
    }

    [Theory]
    [InlineData(7, 5, 1)]
    [InlineData(-1, 5, -1)]
    [InlineData(-5, 5, -1)]
    [InlineData(-6, 5, -2)]
    public void FloorDivide_RoundsDown(int dividend, int divisor, int quotient)
    {
        Assert.Equal(quotient, TarotMath.FloorDivide(dividend, divisor));
    }

    [Theory]
    [InlineData(105, 0, 105)]
    [InlineData(105, 4, 105)]
    [InlineData(105, 5, 106)]
    [InlineData(105, 20, 109)]
    [InlineData(124, 5, 126)]
    public void FoolReversedTotal_MovesOnlyAtEachBreakpoint(int shownTotal, int sumChange, int total)
    {
        Assert.Equal(total, TarotMath.FoolReversedTotal(shownTotal, sumChange));
    }

    [Theory]
    [InlineData(105, -1, 103)]
    [InlineData(105, -5, 103)]
    [InlineData(105, -6, 102)]
    public void FoolReversedTotal_LosesABreakpointForAnyLoss(int shownTotal, int sumChange, int total)
    {
        Assert.Equal(total, TarotMath.FoolReversedTotal(shownTotal, sumChange));
    }

    [Theory]
    [InlineData(80, 10, 4, 5, 8)]
    [InlineData(80, 0, 4, 5, 0)]
    [InlineData(115, 20, 23, 20, 23)]
    public void ScaledChange_ScalesTheChangeOnTheRawValue(
        int serverValue,
        int rawChange,
        int numerator,
        int denominator,
        int change)
    {
        Assert.Equal(change, TarotMath.ScaledChange(serverValue, rawChange, numerator, denominator));
    }

    [Fact]
    public void NoCards_PassTheChangesThrough()
    {
        Assert.Equal(
            new CombatValues(103, 88, 65, 57),
            Shown(0, new CombatValues(100, 90, 60, 50), new CombatValues(3, -2, 5, 7)));
    }

    [Fact]
    public void TheLovers_AveragesWeaponAndArmorValue()
    {
        var shown = Shown(Cards(TarotCard.Lovers), new CombatValues(0, 0, 50, 50), new CombatValues(0, 0, 4, 0));

        Assert.Equal(52, shown.Weapon);
        Assert.Equal(52, shown.Armor);
    }

    [Fact]
    public void TheLoversReversed_AveragesHitAndParry()
    {
        var shown = Shown(
            Cards(TarotCard.LoversReversed),
            new CombatValues(100, 100, 0, 0),
            new CombatValues(6, 0, 0, 0));

        Assert.Equal(103, shown.Hit);
        Assert.Equal(103, shown.Parry);
    }

    [Fact]
    public void StrengthReversed_LowersHitAndRaisesWeaponValue()
    {
        var shown = Shown(
            Cards(TarotCard.StrengthReversed),
            new CombatValues(100, 100, 60, 0),
            new CombatValues(10, 0, 10, 0));

        Assert.Equal(72, shown.Weapon);
        Assert.Equal(108, shown.Hit);
        Assert.Equal(100, shown.Parry);
    }

    [Fact]
    public void TheHermit_RaisesArmorValue()
    {
        Assert.Equal(
            138,
            Shown(Cards(TarotCard.Hermit), new CombatValues(0, 0, 0, 115), new CombatValues(0, 0, 0, 20)).Armor);
    }

    [Fact]
    public void TemperanceReversed_CountsFourStacks()
    {
        Assert.Equal(4, TarotMath.TemperanceSicknessStacks);
        Assert.Equal(
            154,
            Shown(Cards(TarotCard.TemperanceReversed), new CombatValues(0, 0, 140, 0), new CombatValues(0, 0, 10, 0))
                .Weapon);
    }

    [Theory]
    [InlineData(3, 51)]
    [InlineData(-3, 48)]
    [InlineData(0, 50)]
    public void TheLovers_RoundsOddAndNegativeChangesDown(int weaponChange, int weapon)
    {
        Assert.Equal(
            weapon,
            Shown(Cards(TarotCard.Lovers), new CombatValues(0, 0, 50, 50), new CombatValues(0, 0, weaponChange, 0))
                .Weapon);
    }

    [Fact]
    public void TheHitPercent_ComesAfterLoversReversedAndBeforeStrengthReversed()
    {
        var effects = new PlannerEffects { HitPercent = 4 };

        var shown = TarotMath.CombatShown(
            Cards(TarotCard.LoversReversed, TarotCard.StrengthReversed),
            effects,
            new CombatValues(100, 100, 0, 0),
            new CombatValues(10, 0, 0, 0));

        Assert.Equal(104, shown.Hit);
        Assert.Equal(105, shown.Parry);
    }

    [Fact]
    public void TheArmorPercent_ComesLast()
    {
        var effects = new PlannerEffects { ArmorPercent = 10 };

        var shown = TarotMath.CombatShown(
            Cards(TarotCard.Hermit),
            effects,
            new CombatValues(0, 0, 0, 115),
            new CombatValues(0, 0, 0, 20));

        Assert.Equal(140, shown.Armor);
    }

    [Fact]
    public void HitToParry_AddsPartOfHitToParry()
    {
        var effects = new PlannerEffects { HitToParryPercent = 3 };

        var shown = TarotMath.CombatShown(0, effects, new CombatValues(200, 106, 0, 0), new CombatValues(100, 0, 0, 0));

        Assert.Equal(300, shown.Hit);
        Assert.Equal(109, shown.Parry);
    }

    [Fact]
    public void ParryToHit_UsesTheParryAlreadyRaisedByHit()
    {
        var effects = new PlannerEffects { HitToParryPercent = 3, ParryToHitPercent = 3 };

        var shown = TarotMath.CombatShown(0, effects, new CombatValues(203, 106, 0, 0), new CombatValues(0, 100, 0, 0));

        Assert.Equal(206, shown.Hit);
        Assert.Equal(206, shown.Parry);
    }

    [Fact]
    public void Kwai_ComesAfterStrengthReversed()
    {
        var effects = new PlannerEffects { HitToParryPercent = 3 };

        var shown = TarotMath.CombatShown(
            Cards(TarotCard.StrengthReversed),
            effects,
            new CombatValues(80, 102, 0, 0),
            new CombatValues(25, 0, 0, 0));

        Assert.Equal(100, shown.Hit);
        Assert.Equal(103, shown.Parry);
    }

    [Fact]
    public void TheArmorEnchant_ComesAfterTheHermitAndBeforeThePercent()
    {
        var effects = new PlannerEffects { ArmorEnchant = 1, ArmorPercent = 10 };

        var shown = TarotMath.CombatShown(
            Cards(TarotCard.Hermit),
            effects,
            new CombatValues(0, 0, 0, 138),
            new CombatValues(0, 0, 0, 20));

        // The Hermit makes the 20 into 23, the enchant makes that 25 and the percent makes it 28.
        Assert.Equal(166, shown.Armor);
    }

    [Fact]
    public void NoChange_LeavesTheServerValuesWhateverTheCardsAndEnchants()
    {
        var effects = new PlannerEffects { HitToParryPercent = 3, ParryToHitPercent = 3, ArmorEnchant = 1 };

        var shown = TarotMath.CombatShown(
            Cards(TarotCard.Hermit, TarotCard.StrengthReversed),
            effects,
            new CombatValues(203, 106, 50, 60),
            new CombatValues(0, 0, 0, 0));

        Assert.Equal(new CombatValues(203, 106, 50, 60), shown);
    }

    [Fact]
    public void ModelledCards_AreTheTenTheMathReads()
    {
        Assert.Equal(Enum.GetValues<TarotCard>(), TarotMath.ModelledCards);
    }

    private static ulong Cards(params TarotCard[] cards) =>
        cards.Aggregate(0ul, (set, card) => set | (1ul << (int)card));

    private static CombatValues Shown(ulong cards, CombatValues server, CombatValues rawChange) =>
        TarotMath.CombatShown(cards, new PlannerEffects(), server, rawChange);
}
