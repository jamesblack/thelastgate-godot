using GdUnit4;
using TheLastGate.Buffs;
using TheLastGate.Planner;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class BuffFocusTests
{
    private const int FocusedSlot = 8;
    private const int BlessSlot = 9;
    private const int GearMasterySlot = 12;

    // Raise slots the model never reads, so points there change nothing.
    private const int FirstInertSlot = 20;

    private const int Bless = (int)BuffKind.Bless;

    [TestCase]
    public void BlessPoints_WinWhenCheapest()
    {
        var result = BuffFocus.Search(Fixture());

        AssertThat(result.CurrentValue).IsEqual(106);
        AssertSingle(result.Best, BlessSlot, 7, 140);
        AssertSingle(result.Direct, FocusedSlot, 1, 1000);
        AssertSingle(result.ThroughAttributes, 4, 1, 500);
        AssertSingle(result.ThroughBless, BlessSlot, 7, 140);
    }

    [TestCase]
    public void Direct_WinsWhenCheap()
    {
        var input = Fixture();
        input.Candidates[0].StepCosts[0] = 10;

        var result = BuffFocus.Search(input);

        AssertSingle(result.Best, FocusedSlot, 1, 10);
        AssertSingle(result.Direct, FocusedSlot, 1, 10);
        AssertSingle(result.ThroughAttributes, 4, 1, 500);
        AssertSingle(result.ThroughBless, BlessSlot, 7, 140);
    }

    [TestCase]
    public void ACappedBless_IsGoneAround()
    {
        var input = Fixture();
        for (var step = 4; step < FocusSearch.MostPoints; step++)
        {
            input.Candidates[6].StepCosts[step] = SkillFocus.NoCost;
        }

        var result = BuffFocus.Search(input);

        AssertSingle(result.Best, 4, 1, 500);
        AssertSingle(result.ThroughAttributes, 4, 1, 500);

        // A Bless point the route would succeed without doesn't make it a route through Bless.
        AssertThat(result.ThroughBless?.TotalCost).IsEqual(1180);
    }

    [TestCase]
    public void EverythingCapped_LeavesNoRoute()
    {
        var input = Fixture();
        foreach (var candidate in input.Candidates)
        {
            Array.Fill(candidate.StepCosts, SkillFocus.NoCost);
        }

        var result = BuffFocus.Search(input);

        AssertThat(result.Best).IsNull();
        AssertThat(result.Direct).IsNull();
        AssertThat(result.ThroughAttributes).IsNull();
        AssertThat(result.ThroughBless).IsNull();
    }

    [TestCase]
    public void FocusingBlessItself_MakesABlessPointTheDirectRaise()
    {
        var input = new BuffFocusInput
        {
            Model = Model(),
            Focused = Skill(60, AttributeSlot.Braveness, AttributeSlot.Willpower, AttributeSlot.Willpower),
            FocusedRaiseIndex = BlessSlot,
        };
        input.SpellRaiseIndex[Bless] = BlessSlot;
        AddCandidate(input, BlessSlot, 16, 20);
        AddCandidate(input, 0, 6, 300);

        var result = BuffFocus.Search(input);

        AssertThat(result.CurrentValue).IsEqual(66);
        AssertSingle(result.Best, BlessSlot, 1, 20);
        AssertSingle(result.Direct, BlessSlot, 1, 20);
        AssertThat(result.ThroughBless).IsNull();
    }

    [TestCase]
    public void FoolReversed_UsesTheAttributePool()
    {
        var model = Model();
        model.ServerAttributes.SetAll(105);
        model.SpellKnown[Bless] = false;
        model.ShowBuffed[Bless] = false;
        model.TarotCards = 1ul << (int)TarotCard.FoolReversed;
        var input = new BuffFocusInput
        {
            Model = model,
            Focused = Skill(100, AttributeSlot.Braveness, AttributeSlot.Willpower, AttributeSlot.Intuition),
            FocusedRaiseIndex = FocusedSlot,
        };
        AddCandidate(input, FocusedSlot, 1, 1000);
        AddCandidate(input, BuffFocus.AttributePool, 10, 10);

        var result = BuffFocus.Search(input);

        AssertSingle(result.Best, BuffFocus.AttributePool, 10, 100);
    }

    [TestCase]
    public void Armor_FeedsResistanceThroughGearMastery()
    {
        var input = ResistFixture();

        // 59 to 60 Gear Mastery adds 1 AV, which takes the shown AV from 109 to 110 and its tenth from 10 to 11.
        var gearMastery = (int)CombatSkill.GearMastery;
        input.Combat.Known[gearMastery] = true;
        input.Combat.Current[gearMastery] = 59;
        input.Combat.Shown[gearMastery] = 59;
        input.CombatRaiseIndex[gearMastery] = GearMasterySlot;
        input.CombatSkills[gearMastery] = Skill(59);
        AddCandidate(input, GearMasterySlot, 16, 5);

        var result = BuffFocus.Search(input);

        AssertThat(result.CurrentValue).IsEqual(70);
        AssertSingle(result.Best, GearMasterySlot, 1, 5);
    }

    [TestCase]
    public void AHitpointRaise_ReachesResistanceThroughTheOdachi()
    {
        var input = ResistFixture();

        // 499 to 500 Hitpoints adds 1 Spell Bonus, which the Odachi adds to AV.
        input.Model.Effects.SpellBonusToArmor = 1;
        input.Model.Effects.HitpointsToSpellBonus = 1;
        input.Model.UncappedHitpoints = 499;
        AddCandidate(input, BuffFocus.HitpointsRaise, 16, 3);

        var result = BuffFocus.Search(input);

        AssertThat(result.CurrentValue).IsEqual(70);
        AssertSingle(result.Best, BuffFocus.HitpointsRaise, 1, 3);
    }

    [TestCase]
    public void ManyCandidates_StillFindACheapRoute()
    {
        var input = Fixture();
        for (var inert = 0; input.Candidates.Count < FocusSearch.MostCandidates; inert++)
        {
            AddCandidate(input, FirstInertSlot + inert, FocusSearch.MostPoints, 1000);
        }

        var result = BuffFocus.Search(input);

        AssertThat(input.Candidates.Count).IsEqual(FocusSearch.MostCandidates);
        AssertSingle(result.Best, BlessSlot, 7, 140);
        AssertSingle(result.Direct, FocusedSlot, 1, 1000);
        AssertSingle(result.ThroughAttributes, 4, 1, 500);
        AssertSingle(result.ThroughBless, BlessSlot, 7, 140);
    }

    [TestCase]
    public void ASearchWithoutARoute_StopsAtTheStateLimit()
    {
        var input = Fixture();
        input.Candidates.Clear();
        while (input.Candidates.Count < FocusSearch.MostCandidates)
        {
            AddCandidate(input, FirstInertSlot + input.Candidates.Count, FocusSearch.MostPoints, 1);
        }

        var result = BuffFocus.Search(input);

        AssertThat(result.CurrentValue).IsEqual(106);
        AssertThat(result.Best).IsNull();
        AssertThat(result.Direct).IsNull();
        AssertThat(result.ThroughAttributes).IsNull();
        AssertThat(result.ThroughBless).IsNull();
    }

    private static BuffModelInput Model()
    {
        var model = new BuffModelInput
        {
            Divisor = 5,
            SpellMultiplier = 100,
            SpellAptitude = 200,
            RaceModifier = 100,
            ServerWeapon = 30,
            ServerArmor = 30,
        };
        model.ServerAttributes.SetAll(50);
        model.BaseAttributes.SetAll(40);
        model.SpellKnown[Bless] = true;
        model.SpellSkills[Bless] = Skill(60, AttributeSlot.Braveness, AttributeSlot.Willpower, AttributeSlot.Willpower);
        model.ShowBuffed[Bless] = true;
        return model;
    }

    // The focused skill uses Agility, Strength and Strength; Bless is the lever.
    private static BuffFocusInput Fixture()
    {
        var input = new BuffFocusInput
        {
            Model = Model(),
            Focused = Skill(100, AttributeSlot.Agility, AttributeSlot.Strength, AttributeSlot.Strength),
            FocusedRaiseIndex = FocusedSlot,
        };
        input.SpellRaiseIndex[Bless] = BlessSlot;
        AddCandidate(input, FocusedSlot, 1, 1000);
        AddCandidate(input, 0, 6, 300);
        AddCandidate(input, 1, 6, 300);
        AddCandidate(input, 2, 6, 300);
        AddCandidate(input, 3, 6, 500);
        AddCandidate(input, 4, 6, 500);
        AddCandidate(input, BlessSlot, 16, 20);
        return input;
    }

    // A Resistance that gains a tenth of the shown AV; the focused point costs far more than any other route.
    private static BuffFocusInput ResistFixture()
    {
        var model = Model();
        model.ShowBuffed[Bless] = false;
        model.ServerArmor = 109;
        model.Effects.ArmorToResist = 1;
        var input = new BuffFocusInput
        {
            Model = model,
            Focused = Skill(70) with { Scale = SkillScale.Resist },
            FocusedRaiseIndex = FocusedSlot,
            ArmorFeedsFocused = true,
        };
        Array.Fill(input.CombatRaiseIndex, BuffFocus.NoSlot);
        AddCandidate(input, FocusedSlot, 1, 100);
        return input;
    }

    private static BuffModelSkill Skill(int serverTotal, params AttributeSlot[] attributes) =>
        new() { ServerTotal = serverTotal, Attributes = attributes };

    private static void AddCandidate(BuffFocusInput input, int raiseIndex, int mostPoints, int cost)
    {
        var candidate = new FocusCandidate { RaiseIndex = raiseIndex, MostPoints = mostPoints };
        Array.Fill(candidate.StepCosts, cost);
        input.Candidates.Add(candidate);
    }

    private static void AssertSingle(FocusRoute? route, int raiseIndex, int points, int cost)
    {
        AssertThat(route).IsNotNull();
        AssertThat(route!.TotalCost).IsEqual(cost);
        AssertThat(route.Raises).ContainsExactly(new FocusRaise(raiseIndex, points, cost));
    }
}
