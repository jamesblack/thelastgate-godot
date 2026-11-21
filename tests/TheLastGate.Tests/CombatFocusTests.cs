using TheLastGate.Planner;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class CombatFocusTests
{
    private const int SwordSlot = 8;
    private const int TacticsSlot = 9;
    private const int AxeSlot = 10;
    private const int ShieldSlot = 11;

    private const int Agility = (int)AttributeSlot.Agility;
    private const int Strength = (int)AttributeSlot.Strength;

    [Fact]
    public void ACheapWeaponSkillPoint_Wins()
    {
        var input = Fixture(CombatStat.Hit);
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        AddCandidate(input, SwordSlot, 1, 10);
        AddCandidate(input, Agility, 5, 10);
        AddCandidate(input, Strength, 5, 10);

        var result = CombatFocus.Search(input);

        Assert.Equal(100, result.CurrentValue);
        Assert.Equal(10, result.Best?.TotalCost);
        Assert.Equal([new FocusRaise(SwordSlot, 1, 10)], result.Best!.Raises);
    }

    [Fact]
    public void AnAttributeRoute_WinsWhenCheaper()
    {
        var input = Fixture(CombatStat.Parry);

        // Agility counts twice, so three points push the sum past the next multiple of five.
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        AddCandidate(input, SwordSlot, 1, 100);
        AddCandidate(input, Agility, 5, 20);
        AddCandidate(input, Strength, 5, 20);

        var result = CombatFocus.Search(input);

        Assert.Equal(90, result.CurrentValue);
        Assert.Equal(60, result.Best?.TotalCost);
        Assert.Equal([new FocusRaise(Agility, 3, 60)], result.Best!.Raises);
    }

    [Fact]
    public void AnEqualCost_GoesToFewerPoints()
    {
        var input = Fixture(CombatStat.Hit);
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        AddCandidate(input, SwordSlot, 1, 30);
        AddCandidate(input, Agility, 5, 10);

        var result = CombatFocus.Search(input);

        Assert.Equal(30, result.Best?.TotalCost);
        Assert.Equal([new FocusRaise(SwordSlot, 1, 30)], result.Best!.Raises);
    }

    [Fact]
    public void Tactics_NeedsSeveralPoints()
    {
        var input = Fixture(CombatStat.Hit);

        // Tactics adds a sixth of its value, so 60 needs six more points to add one.
        SetCombatSkill(input, CombatSkill.Tactics, TacticsSlot, 60);
        input.CombatSkills[(int)CombatSkill.Tactics] = input.CombatSkills[(int)CombatSkill.Tactics] with
        {
            Attributes = [],
        };
        AddCandidate(input, TacticsSlot, FocusSearch.MostPoints, 5);

        var result = CombatFocus.Search(input);

        Assert.Equal(30, result.Best?.TotalCost);
        Assert.Equal(6, Assert.Single(result.Best!.Raises).Points);
    }

    [Fact]
    public void ACappedSkill_HasNoRoute()
    {
        var input = Fixture(CombatStat.Hit);
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        AddCandidate(input, SwordSlot, 0, 10);

        var result = CombatFocus.Search(input);

        Assert.Equal(100, result.CurrentValue);
        Assert.Null(result.Best);
    }

    [Fact]
    public void AStatNothingCanMove_HasNoRoute()
    {
        var input = Fixture(CombatStat.Armor);
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        AddCandidate(input, SwordSlot, FocusSearch.MostPoints, 10);
        AddCandidate(input, Agility, 5, 10);

        var result = CombatFocus.Search(input);

        Assert.Equal(20, result.CurrentValue);
        Assert.Null(result.Best);
    }

    [Fact]
    public void TheBestWeaponSkill_CanBeOvertaken()
    {
        var input = Fixture(CombatStat.Hit);
        input.Combat.BestWeaponSkill = true;
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        SetCombatSkill(input, CombatSkill.Axe, AxeSlot, 58);
        AddCandidate(input, SwordSlot, 1, 100);
        AddCandidate(input, AxeSlot, FocusSearch.MostPoints, 10);

        var result = CombatFocus.Search(input);

        Assert.Equal(30, result.Best?.TotalCost);
        Assert.Equal([new FocusRaise(AxeSlot, 3, 30)], result.Best!.Raises);
    }

    [Fact]
    public void ParryToHit_LetsShieldPointsRaiseHit()
    {
        var input = Fixture(CombatStat.Hit);
        input.Combat.Offhand = CombatOffhand.Shield;
        input.Model.Effects.ParryToHitPercent = 50;
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);
        SetCombatSkill(input, CombatSkill.Shield, ShieldSlot, 100);
        AddCandidate(input, SwordSlot, 1, 100);
        AddCandidate(input, ShieldSlot, FocusSearch.MostPoints, 5);

        var result = CombatFocus.Search(input);

        Assert.Equal(100, result.CurrentValue);
        Assert.Equal(40, result.Best?.TotalCost);
        Assert.Equal([new FocusRaise(ShieldSlot, 8, 40)], result.Best!.Raises);
    }

    [Fact]
    public void AnEmptyPlan_ShowsTheServersValues()
    {
        var input = Fixture(CombatStat.Hit);
        input.Model.Effects.FistsOfHeavens = 1;
        input.Model.Effects.HitToParryPercent = 3;
        input.Model.Effects.ParryToHitPercent = 3;
        input.Model.Effects.ArmorEnchant = 1;
        input.Model.TarotCards = Card(TarotCard.Hermit);
        input.Combat.BestWeaponSkill = true;
        SetCombatSkill(input, CombatSkill.Sword, SwordSlot, 60);

        Assert.Equal(new CombatValues(100, 90, 30, 20), Shown(input));
    }

    [Fact]
    public void EvergreensWeaponValue_GoesThroughTheLoversAndTheHermit()
    {
        var input = Fixture(CombatStat.Weapon);

        // Twenty more Agility gives Evergreen +2 WV; the Lovers splits it and the Hermit scales the armor half.
        input.Model.Effects.Evergreen = 1;
        input.Model.TarotCards = Card(TarotCard.Lovers) | Card(TarotCard.Hermit);
        input.Model.ServerWeapon = 40;
        input.Model.ServerArmor = 46;
        input.Model.PlannedAttributes[AttributeSlot.Agility] = 20;

        var shown = Shown(input);

        Assert.Equal(41, shown.Weapon);
        Assert.Equal(47, shown.Armor);
    }

    [Fact]
    public void AHitpointRaise_ReachesWeaponValueThroughGornAndTheTachi()
    {
        var input = Fixture(CombatStat.Weapon);

        // 499 to 500 Hitpoints adds 1 Spell Bonus, which the Tachi adds to WV.
        input.Model.Effects.SpellBonusToWeapon = 1;
        input.Model.Effects.HitpointsToSpellBonus = 1;
        input.Model.UncappedHitpoints = 499;
        AddCandidate(input, BuffFocus.HitpointsRaise, FocusSearch.MostPoints, 3);

        var result = CombatFocus.Search(input);

        Assert.Equal(30, result.CurrentValue);
        Assert.Equal(3, result.Best?.TotalCost);
        Assert.Equal(BuffFocus.HitpointsRaise, result.Best!.Raises[0].RaiseIndex);
    }

    [Fact]
    public void AManaRaise_ReachesWeaponValueThroughNeisClaw()
    {
        var input = Fixture(CombatStat.Weapon);

        // 298 Mana needs two points to reach 300 and the next point of Spell Bonus.
        input.Model.Effects.SpellBonusToWeapon = 1;
        input.Model.Effects.ManaToSpellBonus = 1;
        input.Model.UncappedMana = 298;
        AddCandidate(input, BuffFocus.ManaRaise, FocusSearch.MostPoints, 3);

        var result = CombatFocus.Search(input);

        Assert.Equal(6, result.Best?.TotalCost);
        Assert.Equal(2, result.Best!.Raises[0].Points);
    }

    private static CombatFocusInput Fixture(CombatStat stat)
    {
        var model = new BuffModelInput
        {
            Divisor = 5,
            SpellMultiplier = 100,
            SpellAptitude = 200,
            RaceModifier = 100,
            ServerWeapon = 30,
            ServerArmor = 20,
        };
        model.ServerAttributes.SetAll(50);
        model.BaseAttributes.SetAll(40);
        var input = new CombatFocusInput
        {
            Model = model,
            Combat = new CombatModelInput { Weapon = CombatWeapon.Sword, ServerHit = 100, ServerParry = 90 },
            Stat = stat,
        };
        return input;
    }

    private static void SetCombatSkill(CombatFocusInput input, CombatSkill skill, int raiseIndex, int serverTotal)
    {
        input.Combat.Known[(int)skill] = true;
        input.Combat.Current[(int)skill] = serverTotal;
        input.Combat.Shown[(int)skill] = serverTotal;
        input.CombatRaiseIndex[(int)skill] = raiseIndex;
        input.CombatSkills[(int)skill] = new BuffModelSkill
        {
            ServerTotal = serverTotal,
            Attributes = [AttributeSlot.Agility, AttributeSlot.Agility, AttributeSlot.Strength],
        };
    }

    private static void AddCandidate(CombatFocusInput input, int raiseIndex, int mostPoints, int cost)
    {
        var candidate = new FocusCandidate { RaiseIndex = raiseIndex, MostPoints = mostPoints };
        for (var step = 0; step < FocusSearch.MostPoints; step++)
        {
            candidate.StepCosts[step] = step < mostPoints ? cost : SkillFocus.NoCost;
        }

        input.Candidates.Add(candidate);
    }

    private static ulong Card(TarotCard card) => 1ul << (int)card;

    private static CombatValues Shown(CombatFocusInput input) =>
        BuffModel.CombatShown(input.Model, BuffModel.Compute(input.Model), input.Combat, input.CombatSkills);
}
