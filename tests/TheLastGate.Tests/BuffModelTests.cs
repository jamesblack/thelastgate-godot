using TheLastGate.Buffs;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class BuffModelTests
{
    private const int Bless = (int)BuffKind.Bless;
    private const int Enhance = (int)BuffKind.Enhance;
    private const int Protect = (int)BuffKind.Protect;

    private static readonly AttributeSlot Braveness = AttributeSlot.Braveness;
    private static readonly AttributeSlot Willpower = AttributeSlot.Willpower;
    private static readonly AttributeSlot Intuition = AttributeSlot.Intuition;
    private static readonly AttributeSlot Agility = AttributeSlot.Agility;
    private static readonly AttributeSlot Strength = AttributeSlot.Strength;

    [Fact]
    public void BlessAndItemBonuses_FollowTheServersFormulas()
    {
        Assert.Equal(11, BuffModel.BlessBonus(60, Braveness));
        Assert.Equal(10, BuffModel.BlessBonus(60, Willpower));
        Assert.Equal(0, BuffModel.BlessBonus(0, Braveness));
        Assert.Equal(16, BuffModel.ItemBonus(50, false));
        Assert.Equal(11, BuffModel.ItemBonus(50, true));
        Assert.Equal(127, BuffModel.ItemBonus(1000, false));
    }

    [Fact]
    public void Bless_StopsWhereARecastNoLongerRaisesIt()
    {
        var result = BuffModel.Compute(Fixture());

        Assert.Equal(66, result.CastPower[Bless]);
        Assert.Equal(66, result.EffectivePower[Bless]);
        Assert.Equal(AttributeAmounts.Of(61, 61, 61, 61, 61), result.Attributes);
    }

    [Fact]
    public void BlessOff_ShowsTheUnbuffedAttributes()
    {
        var input = Fixture();
        input.ShowBuffed[Bless] = false;

        var result = BuffModel.Compute(input);

        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 50, 50), result.Attributes);
        Assert.Equal(0, result.EffectivePower[Bless]);
    }

    [Fact]
    public void Aptitude_CapsThePower()
    {
        var input = Fixture();
        input.SpellAptitude = 20;

        Assert.Equal(AttributeAmounts.Of(58, 58, 57, 57, 57), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void TheMultiplierAndSpellBonus_RaiseThePower()
    {
        var input = Fixture();
        input.SpellMultiplier = 150;
        input.SpellBonus = 10;

        var result = BuffModel.Compute(input);

        Assert.Equal(121, result.CastPower[Bless]);
        Assert.Equal(AttributeAmounts.Of(69, 68, 68, 68, 68), result.Attributes);
    }

    [Fact]
    public void PlannedWillpower_FeedsBless()
    {
        var input = Fixture();
        input.PlannedAttributes[Willpower] = 5;

        var result = BuffModel.Compute(input);

        Assert.Equal(68, result.CastPower[Bless]);
        Assert.Equal(AttributeAmounts.Of(62, 66, 61, 61, 61), result.Attributes);
    }

    [Fact]
    public void PlannedIntuition_RaisesAptitude()
    {
        var input = Fixture();
        input.SpellAptitude = 20;
        input.PlannedAttributes[Intuition] = 10;
        Assert.Equal(30, BuffModel.Compute(input).SpellAptitude);

        input.RaceModifier = 115;
        Assert.Equal(31, BuffModel.Compute(input).SpellAptitude);
    }

    [Fact]
    public void AnActiveBless_IsTakenOutOfTheServersTotals()
    {
        var input = Fixture();
        input.ServerAttributes.SetAll(61);
        input.SpellSkills[Bless].ServerTotal = 66;
        input.Active[Bless].Known = true;
        input.Active[Bless].Power = 66;

        input.ShowBuffed[Bless] = false;
        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 50, 50), BuffModel.Compute(input).Attributes);

        input.ShowBuffed[Bless] = true;
        var result = BuffModel.Compute(input);
        Assert.Equal(AttributeAmounts.Of(61, 61, 61, 61, 61), result.Attributes);
        Assert.Equal(66, result.CastPower[Bless]);
    }

    [Fact]
    public void AStrongerActiveBless_IsKept()
    {
        var input = Fixture();
        SetAttributes(input.ServerAttributes, 66, 66, 65, 65, 65);
        input.SpellSkills[Bless].ServerTotal = 69;
        input.Active[Bless].Known = true;
        input.Active[Bless].Power = 100;

        var result = BuffModel.Compute(input);

        Assert.Equal(AttributeAmounts.Of(66, 66, 65, 65, 65), result.Attributes);
        Assert.Equal(66, result.CastPower[Bless]);
        Assert.Equal(100, result.EffectivePower[Bless]);
    }

    [Fact]
    public void TheExactBonus_IsUsedWhenKnown()
    {
        var input = Fixture();
        SetAttributes(input.ServerAttributes, 62, 61, 61, 61, 61);
        var active = input.Active[Bless];
        active.Known = true;
        active.Power = 66;
        active.HasExact = true;
        SetAttributes(active.AttributeBonus, 12, 11, 11, 11, 11);
        input.ShowBuffed[Bless] = false;

        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 50, 50), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void Enhance_RaisesWeaponValue()
    {
        var input = EnhanceFixture();
        var result = BuffModel.Compute(input);
        Assert.Equal(46, result.Weapon);
        Assert.Equal(30, result.Armor);

        input.ReducedItemBuffs = true;
        Assert.Equal(41, BuffModel.Compute(input).Weapon);

        input = EnhanceFixture();
        input.Active[Enhance].Known = true;
        input.Active[Enhance].Power = 40;
        input.ServerWeapon = 44;
        input.ShowBuffed[Enhance] = false;
        Assert.Equal(30, BuffModel.Compute(input).Weapon);
    }

    [Fact]
    public void Enhance_UsesTheBlessedAttributes()
    {
        var input = EnhanceFixture();
        input.ShowBuffed[Bless] = true;

        Assert.Equal(48, BuffModel.Compute(input).Weapon);
    }

    [Fact]
    public void FoolReversed_SpreadsPlannedPointsOverEveryAttribute()
    {
        var input = FoolReversedFixture();
        input.PlannedAttributes[Braveness] = 5;

        Assert.Equal(AttributeAmounts.Of(106, 106, 106, 106, 106), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void FoolReversed_FourPointsChangeNothing()
    {
        var input = FoolReversedFixture();
        input.PlannedAttributes[Braveness] = 4;

        Assert.Equal(AttributeAmounts.Of(105, 105, 105, 105, 105), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void FoolReversed_TakingOutAnActiveBlessDropsWholeBreakpoints()
    {
        var input = FoolReversedFixture();
        var active = input.Active[Bless];
        active.Known = true;
        active.Power = 10;
        active.HasExact = true;
        SetAttributes(active.AttributeBonus, 2, 1, 1, 1, 1);

        Assert.Equal(AttributeAmounts.Of(102, 102, 102, 102, 102), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void TheMagician_UsesStrengthsBaseForAptitude()
    {
        var input = Fixture();
        input.ShowBuffed[Bless] = false;
        input.ServerAttributes[Strength] = 60;
        input.PlannedAttributes[Strength] = 10;
        Assert.Equal(200, BuffModel.Compute(input).SpellAptitude);

        input.TarotCards = Card(TarotCard.Magician);
        Assert.Equal(210, BuffModel.Compute(input).SpellAptitude);
    }

    [Fact]
    public void StarReversed_DropsTheSpellBonusFromTheCastPower()
    {
        var input = Fixture();
        var withoutBonus = BuffModel.Compute(input);
        input.SpellBonus = 20;
        input.TarotCards = Card(TarotCard.StarReversed);

        Assert.Equal(withoutBonus.CastPower[Bless], BuffModel.Compute(input).CastPower[Bless]);
    }

    [Fact]
    public void TheHermit_ScalesAResistanceSkillsChange()
    {
        var input = Fixture();
        var skill = Skill(80);
        skill.Planned = 10;
        skill.Scale = SkillScale.Resist;
        Assert.Equal(90, BuffModel.SkillValue(input, skill, input.ServerAttributes));

        input.TarotCards = Card(TarotCard.Hermit);
        Assert.Equal(88, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void MoonReversed_ScalesTacticsWithMana()
    {
        var input = Fixture();
        var skill = Skill(110);
        skill.Planned = 10;
        skill.Scale = SkillScale.Tactics;
        input.TarotCards = Card(TarotCard.MoonReversed);
        input.UncappedMana = 500;
        Assert.Equal(121, BuffModel.SkillValue(input, skill, input.ServerAttributes));

        input.PlannedMana = 500;
        Assert.Equal(132, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void NoEffects_MatchTheOldModel()
    {
        var input = Fixture();
        input.SpellSkills[Bless].Groups = SkillGroups.Passive | SkillGroups.ActiveSpell | SkillGroups.ActiveMelee;
        input.PlannedAttributes[Willpower] = 5;
        var result = BuffModel.Compute(input);
        Assert.Equal(68, result.CastPower[Bless]);
        Assert.Equal(AttributeAmounts.Of(62, 66, 61, 61, 61), result.Attributes);

        var immunity = Skill(80);
        immunity.Planned = 10;
        immunity.Scale = SkillScale.Immunity;
        Assert.Equal(90, BuffModel.SkillValue(input, immunity, input.ServerAttributes));
    }

    [Fact]
    public void TheAttributePercent_ScalesPlannedPoints()
    {
        var input = UnbuffedFixture();
        input.ServerAttributes[Strength] = 100;
        input.Effects.AttributePercent[Strength] = 6;
        input.PlannedAttributes[Strength] = 10;

        var result = BuffModel.Compute(input);

        Assert.Equal(111, result.Attributes[Strength]);
        Assert.Equal(50, result.Attributes[Agility]);
    }

    [Fact]
    public void TheAttributePercent_RoundsAsTheServerDoes()
    {
        var input = UnbuffedFixture();
        input.ServerAttributes[Strength] = 106;
        input.Effects.AttributePercent[Strength] = 6;
        Assert.Equal(106, BuffModel.Compute(input).Attributes[Strength]);

        input.PlannedAttributes[Strength] = 1;
        Assert.Equal(107, BuffModel.Compute(input).Attributes[Strength]);
    }

    [Fact]
    public void TheSkillExtraPercent_AddsToItsGroupsSkills()
    {
        // (52 + 52 + 105 + 2100 / 100) / 5 = 46 against (52 + 52 + 100 + 2000 / 100) / 5 = 44.
        Assert.Equal(62, StrengthRaiseValue(SkillGroups.ActiveMelee));
    }

    [Fact]
    public void TheSkillExtraPercent_LeavesOtherGroupsAlone()
    {
        // 209 / 5 = 41 against 204 / 5 = 40, as without the effect.
        Assert.Equal(61, StrengthRaiseValue(SkillGroups.Passive));
    }

    [Fact]
    public void TheImmunityPercent_ScalesTheChange()
    {
        Assert.Equal(121, ImmunityWithBravenessRaise(0));
    }

    [Fact]
    public void TheImmunityPercent_WithTheHermit()
    {
        // Raw 110 x 500 / 440 = 125 shows 109 and 135 shows 118, so the change is 9.
        Assert.Equal(119, ImmunityWithBravenessRaise(Card(TarotCard.Hermit)));
    }

    [Fact]
    public void TheImmunityPercentAndHermit_RoundInTwoSteps()
    {
        var input = Fixture();
        input.TarotCards = Card(TarotCard.Hermit);
        input.Effects.ImmunityPercent = 10;
        var skill = Skill(88);
        skill.Planned = 29;
        skill.Scale = SkillScale.Immunity;

        // Raw 100 becomes 129: 129 x 110 / 100 = 141, then 141 x 4 / 5 = 112 (one fraction would give 113).
        Assert.Equal(112, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void TheResistPercent_ScalesADirectRaise()
    {
        var input = Fixture();
        input.Effects.ResistPercent = 10;
        var skill = Skill(110);
        skill.Scale = SkillScale.Resist;
        Assert.Equal(110, BuffModel.SkillValue(input, skill, input.ServerAttributes));

        // Raw 110 x 100 / 110 = 100 becomes 110, shown as 110 x 110 / 100 = 121.
        skill.Planned = 10;
        Assert.Equal(121, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void FoolReversed_AveragesBeforeTheAttributePercent()
    {
        var input = FoolReversedFixture();

        // Strength 50 is 40 before +25%, where the next Reverse Fool step adds two, so 40 becomes 42.
        input.ServerAttributes[Strength] = 50;
        input.Effects.AttributePercent[Strength] = 25;
        input.PlannedAttributes[Braveness] = 5;

        Assert.Equal(AttributeAmounts.Of(106, 106, 106, 106, 52), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void TheAptitudePercent_ScalesTheChange()
    {
        var input = UnbuffedFixture();

        // Raw 110 x 100 / 110 = 100 becomes 110, which shows 121.
        input.SpellAptitude = 110;
        input.Effects.AptitudePercent = 10;
        input.PlannedAttributes[Willpower] = 10;

        Assert.Equal(121, BuffModel.Compute(input).SpellAptitude);
    }

    [Fact]
    public void TheAttributePercent_DoesNotReachAptitude()
    {
        var input = UnbuffedFixture();
        input.SpellAptitude = 110;
        input.Effects.AptitudePercent = 10;
        input.Effects.AttributePercent[Willpower] = 10;
        input.PlannedAttributes[Willpower] = 10;

        var result = BuffModel.Compute(input);

        Assert.Equal(61, result.Attributes[Willpower]);
        Assert.Equal(121, result.SpellAptitude);
    }

    [Fact]
    public void Brilliance_FollowsTheAttributeSum()
    {
        var input = BrillianceFixture();
        Assert.Equal(30, BuffModel.Compute(input).SpellBonus);

        input.PlannedAttributes[Strength] = 1;
        Assert.Equal(31, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void BrillianceNodeAndCopies_RoundApart()
    {
        var input = BrillianceFixture();
        input.Effects.BrillianceNodes = 1;
        Assert.Equal(30, BuffModel.Compute(input).SpellBonus);

        // 450 / 75 + 450 / 150 = 9 against 449 / 75 + 449 / 150 = 7; one 3 x sum / 150 would only go from 8 to 9.
        input.PlannedAttributes[Strength] = 1;
        Assert.Equal(32, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void Brilliance_WithTheMagician()
    {
        var input = UnbuffedFixture();
        input.ServerAttributes[Strength] = 100;
        input.SpellBonus = 10;
        input.Effects.Brilliance = 1;
        input.PlannedAttributes[Strength] = 50;
        Assert.Equal(10, BuffModel.Compute(input).SpellBonus);

        // Strength 150 also stands in for Intuition 50, so the sum is 450 against 350.
        input.TarotCards = Card(TarotCard.Magician);
        Assert.Equal(11, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void Introspection_FollowsAptitude()
    {
        var input = UnbuffedFixture();
        input.SpellAptitude = 99;
        input.SpellBonus = 40;
        input.Effects.Introspection = 2;
        input.PlannedAttributes[Willpower] = 1;

        var result = BuffModel.Compute(input);

        Assert.Equal(100, result.SpellAptitude);
        Assert.Equal(41, result.SpellBonus);
    }

    [Fact]
    public void TheSpellBonusPercent_ScalesTheChange()
    {
        var input = UnbuffedFixture();

        // Introspection adds 5 x 300 / 50 - 5 x 200 / 50 = 10 to raw 50, which shows 72.
        input.SpellBonus = 60;
        input.Effects.SpellBonusPercent = 20;
        input.Effects.Introspection = 5;
        input.PlannedAttributes[Willpower] = 100;

        var result = BuffModel.Compute(input);

        Assert.Equal(300, result.SpellAptitude);
        Assert.Equal(72, result.SpellBonus);
    }

    [Fact]
    public void ThePlannedSpellBonus_RaisesTheCastPower()
    {
        var input = Fixture();

        // The first cast lifts the attribute sum from 250 to 301, past 300, so the second cast gets one more bonus.
        input.Effects.Brilliance = 1;
        var result = BuffModel.Compute(input);
        Assert.Equal(67, result.CastPower[Bless]);
        Assert.Equal(1, result.SpellBonus);

        input.SpellAptitude = 33;
        Assert.Equal(66, BuffModel.Compute(input).CastPower[Bless]);
    }

    [Fact]
    public void GuardianAngel_ComesAfterTheAptitudeCap()
    {
        var input = Fixture();

        // Skill 250 is capped at 200. The first cast sees Braveness 70 and gives 207, which lifts Braveness to 100.
        input.ServerAttributes[Braveness] = 70;
        input.SpellAptitude = 100;
        input.SpellSkills[Bless] = Skill(250);
        Assert.Equal(200, BuffModel.Compute(input).CastPower[Bless]);

        input.Effects.GuardianAngel = 1;
        var result = BuffModel.Compute(input);
        Assert.Equal(210, result.CastPower[Bless]);
        Assert.Equal(101, result.Attributes[Braveness]);
    }

    [Fact]
    public void Determination_FollowsTheAttributeSum()
    {
        var input = BrillianceFixture();
        input.Effects.Brilliance = 0;
        input.Effects.Determination = 2;
        AssertWeaponAndArmor(30, 30, BuffModel.Compute(input));

        // 2 x 450 / 150 = 6 against 2 x 449 / 150 = 5.
        input.PlannedAttributes[Strength] = 1;
        AssertWeaponAndArmor(31, 31, BuffModel.Compute(input));

        // 2 x 525 / 150 = 7.
        input.PlannedAttributes[Strength] = 76;
        AssertWeaponAndArmor(32, 32, BuffModel.Compute(input));
    }

    [Fact]
    public void DeterminationNodeAndCopies_RoundApart()
    {
        var input = BrillianceFixture();
        input.Effects.Brilliance = 0;
        input.Effects.DeterminationNodes = 1;
        input.Effects.Determination = 1;
        AssertWeaponAndArmor(30, 30, BuffModel.Compute(input));

        // 6 + 3 against 5 + 2.
        input.PlannedAttributes[Strength] = 1;
        AssertWeaponAndArmor(32, 32, BuffModel.Compute(input));
    }

    [Fact]
    public void Fists_BoostsTheHighestAttribute()
    {
        var input = FistsFixture();

        // Strength 120 is 100 before the boost; 105 boosts to 126.
        input.ServerAttributes[Strength] = 120;
        input.PlannedAttributes[Strength] = 5;
        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 50, 126), BuffModel.Compute(input).Attributes);

        input.PlannedAttributes[Strength] = 0;
        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 50, 120), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void Fists_MovesToTheNewHighest()
    {
        var input = FistsFixture();

        // Agility 105 overtakes Strength 100, so Agility is boosted to 126 and Strength loses its boost.
        input.ServerAttributes[Agility] = 95;
        input.ServerAttributes[Strength] = 120;
        input.PlannedAttributes[Agility] = 10;

        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 126, 100), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void Fists_BoostsEveryAttributeTiedAtTheTop()
    {
        var input = FistsFixture();
        input.ServerAttributes[Agility] = 120;
        input.ServerAttributes[Strength] = 120;
        input.PlannedAttributes[Agility] = 1;

        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 121, 100), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void Fists_RecoversTheExactValueBeforeTheBoost()
    {
        var input = FistsFixture();

        // Strength 121 came from 101; rounding down would give 100 and tie it with Agility.
        input.ServerAttributes[Agility] = 100;
        input.ServerAttributes[Strength] = 121;
        input.PlannedAttributes[Strength] = 1;

        Assert.Equal(AttributeAmounts.Of(50, 50, 50, 100, 122), BuffModel.Compute(input).Attributes);
    }

    [Fact]
    public void TheGrandeGrimoire_BoostsTheHighestAttribute()
    {
        var input = UnbuffedFixture();
        input.Effects.GrandeGrimoire = 1;
        input.ServerAttributes[Strength] = 110;
        input.PlannedAttributes[Strength] = 10;

        Assert.Equal(121, BuffModel.Compute(input).Attributes[Strength]);
    }

    [Fact]
    public void Fists_ComesBeforeTheGrimoire()
    {
        var input = FistsFixture();

        // 100 -> 120 -> 132 on the server; 105 -> 126 -> 138 with the plan.
        input.Effects.GrandeGrimoire = 1;
        input.ServerAttributes[Strength] = 132;
        input.PlannedAttributes[Strength] = 5;

        Assert.Equal(138, BuffModel.Compute(input).Attributes[Strength]);
    }

    [Fact]
    public void Fists_ComesAfterTheAttributePercent()
    {
        var input = FistsFixture();

        // 132 is 110 before the boost, raw 100 before the percent; raw 110 shows 121, which boosts to 145.
        input.ServerAttributes[Strength] = 132;
        input.Effects.AttributePercent[Strength] = 10;
        input.PlannedAttributes[Strength] = 10;

        Assert.Equal(145, BuffModel.Compute(input).Attributes[Strength]);
    }

    [Fact]
    public void AjaxBlue_MovesOnlyBonusBraveness()
    {
        var input = UnbuffedFixture();
        input.Effects.BraveIntoStrength = 1;
        input.PlannedAttributes[Braveness] = 10;
        Assert.Equal(AttributeAmounts.Of(60, 50, 50, 50, 50), BuffModel.Compute(input).Attributes);

        // Bless's 11 Braveness also reaches Strength.
        input = Fixture();
        input.Effects.BraveIntoStrength = 1;
        var result = BuffModel.Compute(input);
        Assert.Equal(66, result.CastPower[Bless]);
        Assert.Equal(AttributeAmounts.Of(61, 61, 61, 61, 72), result.Attributes);
    }

    [Fact]
    public void TheTravelersGuide_FeedsDetermination()
    {
        var input = UnbuffedFixture();

        // The sum goes from 300 to 350 (2 both times), but with Agility standing in for Braveness from 350 to 450.
        input.ServerAttributes[Agility] = 100;
        input.Effects.Determination = 1;
        input.PlannedAttributes[Agility] = 50;
        AssertWeaponAndArmor(30, 30, BuffModel.Compute(input));

        input.Effects.TravelersGuide = 1;
        AssertWeaponAndArmor(31, 31, BuffModel.Compute(input));
    }

    [Fact]
    public void Evergreen_AddsAgilityToWeaponAndStrengthToArmor()
    {
        var input = UnbuffedFixture();
        input.Effects.Evergreen = 1;
        input.PlannedAttributes[Agility] = 10;
        AssertWeaponAndArmor(31, 30, BuffModel.Compute(input));

        input.PlannedAttributes[Agility] = 0;
        input.PlannedAttributes[Strength] = 9;
        Assert.Equal(30, BuffModel.Compute(input).Armor);

        input.PlannedAttributes[Strength] = 10;
        Assert.Equal(31, BuffModel.Compute(input).Armor);
    }

    [Fact]
    public void Evergreen_WithTheTravelersGuide()
    {
        var input = UnbuffedFixture();

        // Braveness 90 stands in for Agility 50.
        input.Effects.Evergreen = 1;
        input.Effects.TravelersGuide = 1;
        input.ServerAttributes[Braveness] = 80;
        input.PlannedAttributes[Braveness] = 10;

        Assert.Equal(31, BuffModel.Compute(input).Weapon);
    }

    [Fact]
    public void TheBlackTachiAndWhiteOdachi_AddThePlannedSpellBonus()
    {
        var input = UnbuffedFixture();

        // Brilliance goes from 250 / 150 = 1 to 300 / 150 = 2.
        input.Effects.Brilliance = 1;
        input.SpellBonus = 10;
        input.PlannedAttributes[Strength] = 50;
        input.Effects.SpellBonusToWeapon = 1;
        var result = BuffModel.Compute(input);
        Assert.Equal(11, result.SpellBonus);
        AssertWeaponAndArmor(31, 30, result);

        input.Effects.SpellBonusToWeapon = 0;
        input.Effects.SpellBonusToArmor = 1;
        AssertWeaponAndArmor(30, 31, BuffModel.Compute(input));
    }

    [Fact]
    public void TheGestureOfGorn_AddsHitpointsToSpellBonus()
    {
        var input = UnbuffedFixture();
        input.Effects.HitpointsToSpellBonus = 1;
        input.SpellBonus = 10;
        input.UncappedHitpoints = 450;
        input.PlannedHitpoints = 50;
        Assert.Equal(11, BuffModel.Compute(input).SpellBonus);

        input.PlannedHitpoints = 49;
        Assert.Equal(10, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void NeisClaw_AddsManaToSpellBonus()
    {
        var input = UnbuffedFixture();
        input.Effects.ManaToSpellBonus = 1;
        input.SpellBonus = 10;
        input.UncappedMana = 199;
        input.PlannedMana = 1;

        Assert.Equal(11, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void Flow_AddsManaToSpellBonus()
    {
        var input = UnbuffedFixture();

        // The node and one corruptor give 1000 x 3 / 200 = 15; 1067 Mana gives 16, 1066 still 15.
        input.Effects.Flow = 3;
        input.SpellBonus = 15;
        input.UncappedMana = 1000;
        input.PlannedMana = 67;
        Assert.Equal(16, BuffModel.Compute(input).SpellBonus);

        input.PlannedMana = 66;
        Assert.Equal(15, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void FlowAndNeisClaw_RoundApart()
    {
        var input = UnbuffedFixture();
        input.Effects.Flow = 1;
        input.Effects.ManaToSpellBonus = 1;
        input.SpellBonus = 10;
        input.UncappedMana = 199;
        input.PlannedMana = 1;

        Assert.Equal(12, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void TheSpellBonusEnchant_ComesAfterThePercent()
    {
        var input = UnbuffedFixture();

        // Raw 100 shows 100 x 1.2 = 120, then 132. Gorn adds 10: 110 x 1.2 = 132, then 145.
        input.SpellBonus = 132;
        input.Effects.SpellBonusPercent = 20;
        input.Effects.SpellBonusEnchantPercent = 10;
        input.Effects.HitpointsToSpellBonus = 1;
        input.UncappedHitpoints = 1000;
        input.PlannedHitpoints = 1000;

        Assert.Equal(145, BuffModel.Compute(input).SpellBonus);
    }

    [Fact]
    public void Superbia_CutsAptitude()
    {
        var input = UnbuffedFixture();

        // Raw 200 shows 20; ten more Willpower makes raw 210, which shows 21.
        input.Effects.Superbia = 1;
        input.SpellAptitude = 20;
        input.PlannedAttributes[Willpower] = 10;
        Assert.Equal(21, BuffModel.Compute(input).SpellAptitude);

        input.PlannedAttributes[Willpower] = 5;
        Assert.Equal(20, BuffModel.Compute(input).SpellAptitude);

        // Raw 200 shows 220, then 22; raw 210 shows 231, then 23.
        input.Effects.AptitudePercent = 10;
        input.SpellAptitude = 22;
        input.PlannedAttributes[Willpower] = 10;
        Assert.Equal(23, BuffModel.Compute(input).SpellAptitude);
    }

    [Fact]
    public void TheResistEnchant_ComesAfterTheTreePercent()
    {
        var input = Fixture();

        // Raw 100 shows 110, then 121; raw 110 shows 121, then 133.
        input.Effects.ResistPercent = 10;
        input.Effects.ResistEnchantPercent = 10;
        var skill = Skill(121);
        skill.Planned = 10;
        skill.Scale = SkillScale.Resist;

        Assert.Equal(133, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void ArmorToResistance_AddsATenthOfTheShownArmor()
    {
        var input = Fixture();
        input.Effects.ArmorToResist = 1;
        input.ServerArmor = 100;
        var skill = Skill(70);
        skill.Scale = SkillScale.Resist;
        Assert.Equal(70, BuffModel.SkillValue(input, skill, input.ServerAttributes));
        Assert.Equal(73, BuffModel.SkillValueWithArmor(input, skill, input.ServerAttributes, 130));

        skill.Planned = 5;
        Assert.Equal(75, BuffModel.SkillValue(input, skill, input.ServerAttributes));

        // 76 less the armor's 10 is 66, raw 60; raw 70 shows 77, and the armor's 10 goes back on.
        input.Effects.ResistPercent = 10;
        skill.ServerTotal = 76;
        skill.Planned = 10;
        Assert.Equal(87, BuffModel.SkillValue(input, skill, input.ServerAttributes));
    }

    [Fact]
    public void EveryItemEffect_KeepsAnEmptyPlanAtTheServersValues()
    {
        var input = Fixture();
        input.ShowBuffed[Bless] = false;
        SetAttributes(input.ServerAttributes, 60, 70, 80, 90, 132);
        input.SpellBonus = 50;
        input.SpellAptitude = 30;
        input.UncappedHitpoints = 450;
        input.UncappedMana = 300;
        var effects = input.Effects;
        effects.AttributePercent.SetAll(10);
        effects.FistsOfHeavens = 1;
        effects.GrandeGrimoire = 1;
        effects.TravelersGuide = 1;
        effects.BraveIntoStrength = 1;
        effects.Evergreen = 1;
        effects.SpellBonusToWeapon = 1;
        effects.SpellBonusToArmor = 1;
        effects.HitpointsToSpellBonus = 1;
        effects.ManaToSpellBonus = 1;
        effects.Superbia = 1;
        effects.SpellBonusEnchantPercent = 10;
        effects.ResistEnchantPercent = 10;
        effects.ArmorToResist = 1;
        effects.Determination = 1;

        var result = BuffModel.Compute(input);

        Assert.Equal(AttributeAmounts.Of(60, 70, 80, 90, 132), result.Attributes);
        Assert.Equal(50, result.SpellBonus);
        Assert.Equal(30, result.SpellAptitude);
        AssertWeaponAndArmor(30, 30, result);
        var skill = Skill(77);
        skill.Scale = SkillScale.Resist;
        Assert.Equal(77, BuffModel.SkillValue(input, skill, result.Attributes));
    }

    [Fact]
    public void CombatShown_WithNoPlan_IsTheServersValues()
    {
        var model = UnbuffedFixture();
        var (combat, combatSkills) = SwordFighter(model, 0);

        Assert.Equal(
            new CombatValues(100, 90, 30, 30),
            BuffModel.CombatShown(model, BuffModel.Compute(model), combat, combatSkills));
    }

    [Fact]
    public void CombatShown_CountsAPlannedWeaponSkill()
    {
        var model = UnbuffedFixture();
        var (combat, combatSkills) = SwordFighter(model, 10);

        Assert.Equal(
            new CombatValues(110, 100, 30, 30),
            BuffModel.CombatShown(model, BuffModel.Compute(model), combat, combatSkills));
    }

    private static BuffModelInput Fixture()
    {
        var input = new BuffModelInput
        {
            Divisor = 5,
            SpellMultiplier = 100,
            SpellAptitude = 200,
            RaceModifier = 100,
            ServerWeapon = 30,
            ServerArmor = 30,
        };
        input.ServerAttributes.SetAll(50);
        input.BaseAttributes.SetAll(40);
        input.SpellKnown[Bless] = true;
        input.SpellSkills[Bless] = Skill(60, Braveness, Willpower, Willpower);
        input.SpellSkills[Enhance] = Skill(0);
        input.SpellSkills[Protect] = Skill(0);
        input.ShowBuffed[Bless] = true;
        return input;
    }

    private static BuffModelInput UnbuffedFixture()
    {
        var input = Fixture();
        input.SpellKnown[Bless] = false;
        input.ShowBuffed[Bless] = false;
        return input;
    }

    private static BuffModelInput EnhanceFixture()
    {
        var input = Fixture();
        input.ShowBuffed[Bless] = false;
        input.SpellKnown[Enhance] = true;
        input.SpellSkills[Enhance] = Skill(50, Braveness, Willpower, Intuition);
        input.ShowBuffed[Enhance] = true;
        return input;
    }

    private static BuffModelInput FoolReversedFixture()
    {
        var input = Fixture();
        input.ServerAttributes.SetAll(105);
        input.SpellKnown[Bless] = false;
        input.ShowBuffed[Bless] = false;
        input.TarotCards = Card(TarotCard.FoolReversed);
        return input;
    }

    private static BuffModelInput BrillianceFixture()
    {
        var input = UnbuffedFixture();
        input.ServerAttributes.SetAll(90);
        input.ServerAttributes[Strength] = 89;
        input.SpellBonus = 30;
        input.Effects.Brilliance = 1;
        return input;
    }

    private static BuffModelInput FistsFixture()
    {
        var input = UnbuffedFixture();
        input.Effects.FistsOfHeavens = 1;
        return input;
    }

    private static BuffModelSkill Skill(int serverTotal, params AttributeSlot[] attributes) =>
        new() { ServerTotal = serverTotal, Attributes = attributes };

    private static ulong Card(TarotCard card) => 1ul << (int)card;

    private static void SetAttributes(
        AttributeAmounts amounts,
        int braveness,
        int willpower,
        int intuition,
        int agility,
        int strength)
    {
        amounts[Braveness] = braveness;
        amounts[Willpower] = willpower;
        amounts[Intuition] = intuition;
        amounts[Agility] = agility;
        amounts[Strength] = strength;
    }

    private static void AssertWeaponAndArmor(int weapon, int armor, BuffModelResult result)
    {
        Assert.Equal(weapon, result.Weapon);
        Assert.Equal(armor, result.Armor);
    }

    private static int StrengthRaiseValue(SkillGroups groups)
    {
        var input = Fixture();
        input.ServerAttributes[Agility] = 52;
        input.ServerAttributes[Strength] = 100;
        input.Effects.SkillExtraPercent[Strength] = 20;
        var skill = Skill(60, Agility, Agility, Strength);
        skill.Groups = groups;
        var planned = input.ServerAttributes.Copy();
        planned[Strength] += 5;
        return BuffModel.SkillValue(input, skill, planned);
    }

    private static int ImmunityWithBravenessRaise(ulong tarotCards)
    {
        var input = Fixture();
        input.TarotCards = tarotCards;
        input.Effects.ImmunityPercent = 10;
        var skill = Skill(110, Braveness, Willpower, Intuition);
        skill.Scale = SkillScale.Immunity;
        var planned = input.ServerAttributes.Copy();
        planned[Braveness] += 50;
        return BuffModel.SkillValue(input, skill, planned);
    }

    private static (CombatModelInput Combat, BuffModelSkill[] Skills) SwordFighter(BuffModelInput model, int planned)
    {
        var combat = new CombatModelInput { Weapon = CombatWeapon.Sword, ServerHit = 100, ServerParry = 90 };
        combat.Known[(int)CombatSkill.Sword] = true;
        combat.Current[(int)CombatSkill.Sword] = 80;
        var skills = Enumerable.Range(0, CombatModel.SkillCount).Select(_ => Skill(0)).ToArray();
        skills[(int)CombatSkill.Sword] = new BuffModelSkill { ServerTotal = 80, Planned = planned };
        model.ServerWeapon = 30;
        model.ServerArmor = 30;
        return (combat, skills);
    }
}
