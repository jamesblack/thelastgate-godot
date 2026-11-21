using GdUnit4;
using TheLastGate.Buffs;
using TheLastGate.Planner;
using TheLastGate.Rules;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class FocusInputsTests
{
    private const int SwordSkill = 3;
    private const int BlessSkill = 21;
    private const int Braveness = (int)AttributeSlot.Braveness;
    private const int Willpower = (int)AttributeSlot.Willpower;
    private const int Intuition = (int)AttributeSlot.Intuition;
    private const int Agility = (int)AttributeSlot.Agility;
    private const int Strength = (int)AttributeSlot.Strength;
    private const int NoCost = SkillFocus.NoCost;

    private static readonly int SwordSlot = StatSlots.Skill(SwordSkill);
    private static readonly int BlessSlot = StatSlots.Skill(BlessSkill);

    private PlannerGame game = null!;

    [BeforeTest]
    public void StartAGame() => game = SwordGame();

    [TestCase]
    public void ThePlainInput_ReadsTheShownAttributesAndTheirCosts()
    {
        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        AssertThat(input.Attributes)
            .ContainsExactly(AttributeSlot.Braveness, AttributeSlot.Agility, AttributeSlot.Strength);
        AssertThat(input.AttributeValues).ContainsExactly(61, 61, 61);
        AssertThat(input.AttributeStepCosts[2]).ContainsExactly(9600, 10338, 11113, 11926, 12777, 13668);
        AssertThat(input.Divisor).IsEqual(5);
        AssertThat(input.DirectCost).IsEqual(4800);
        AssertThat(input.FoolReversed).IsFalse();
        AssertThat(input.PoolStepCosts[Willpower][0]).IsEqual(9600);
    }

    [TestCase]
    public void ThePlainInput_MarksCappedPoints()
    {
        game.Attributes[Strength][2] = 42;
        game.Skills[SwordSkill][2] = 40;

        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        AssertThat(input.AttributeStepCosts[2]).ContainsExactly(9600, 10338, NoCost, NoCost, NoCost, NoCost);
        AssertThat(input.DirectCost).IsEqual(NoCost);
    }

    [TestCase]
    public void ThePlainInput_WithoutTheBuffModelAddsThePlannedPoints()
    {
        game.MetaStats.Clear();
        game.Plan.Raise(Agility, game.ValuesFor(Agility), game.Points, 2);

        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        AssertThat(input.AttributeValues).ContainsExactly(50, 52, 50);
        AssertThat(input.AttributeStepCosts[1][0]).IsEqual(11113);
    }

    [TestCase]
    public void TheBuffedInput_TriesTheSkillItsAttributesAndBless()
    {
        var input = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!;

        AssertThat(input.FocusedRaiseIndex).IsEqual(SwordSlot);
        AssertThat(input.SpellRaiseIndex).ContainsExactly(BlessSlot, BuffFocus.NoSlot, BuffFocus.NoSlot);
        AssertThat(input.Focused.ServerTotal).IsEqual(80);
        AssertThat(input.ArmorFeedsFocused).IsFalse();
        AssertThat(input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)))
            .ContainsExactly(
                (SwordSlot, 1),
                (Braveness, 6),
                (Agility, 6),
                (Strength, 6),
                (Willpower, 6),
                (Intuition, 6),
                (BlessSlot, 16));
        AssertThat(input.Candidates[0].StepCosts.Take(2)).ContainsExactly(4800, NoCost);
        AssertThat(input.Candidates[3].StepCosts.Take(7))
            .ContainsExactly(9600, 10338, 11113, 11926, 12777, 13668, NoCost);
        AssertThat(input.Candidates[6].StepCosts[0]).IsEqual(2025);
    }

    [TestCase]
    public void TheBuffedInput_UnderTheReverseFoolBuysAttributesFromOnePool()
    {
        game.Choices.TarotCards = 1ul << (int)TarotCard.FoolReversed;

        var input = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!;

        AssertThat(input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)))
            .ContainsExactly((SwordSlot, 1), (BuffFocus.AttributePool, 10), (BlessSlot, 16));
        var pool = input.Candidates[1].StepCosts;
        AssertThat(pool.Take(5).ToArray()).ContainsExactly(Enumerable.Repeat(9600, 5).ToArray());
        AssertThat(pool.Skip(5).Take(5).ToArray()).ContainsExactly(Enumerable.Repeat(10338, 5).ToArray());
        AssertThat(pool.Skip(10).Distinct().ToArray()).ContainsExactly(NoCost);
    }

    [TestCase]
    public void TheBuffedInput_NeedsTheBuffModel()
    {
        game.MetaStats.Clear();

        AssertThat(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)).IsNull();
    }

    [TestCase]
    public void TheCombatInput_TriesTheSkillsThatMoveTheStat()
    {
        var input = FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!;

        AssertThat(input.Stat).IsEqual(CombatStat.Hit);
        AssertThat(input.Combat.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(input.CombatRaiseIndex[(int)CombatSkill.Sword]).IsEqual(SwordSlot);
        AssertThat(input.CombatRaiseIndex[(int)CombatSkill.Tactics]).IsEqual(BuffFocus.NoSlot);
        AssertThat(input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)))
            .ContainsExactly(
                (SwordSlot, 16),
                (BlessSlot, 16),
                (Braveness, 6),
                (Willpower, 6),
                (Intuition, 6),
                (Agility, 6),
                (Strength, 6));
    }

    [TestCase]
    public void TheCombatInput_NeedsHitAndParry()
    {
        game.MetaStats.Remove("Hit Score");

        AssertThat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)).IsNull();
    }

    [TestCase]
    public void InputsFromTheSameNumbers_AreEqual()
    {
        AssertThat(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill))
            .IsEqual(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill));
        AssertThat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry))
            .IsEqual(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry));
    }

    [TestCase]
    public void APlannedPoint_MakesTheInputsDiffer()
    {
        var buffed = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill);
        var combat = FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry);

        game.Plan.Raise(Intuition, game.ValuesFor(Intuition), game.Points, 1);

        AssertThat(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)).IsNotEqual(buffed);
        AssertThat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry)).IsNotEqual(combat);
    }

    [TestCase]
    public void ANewBuffReading_MakesTheInputsDiffer()
    {
        var buffed = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill);

        game.BuffActive[(int)BuffKind.Bless] = true;
        game.BuffReadings[(int)BuffKind.Bless] = new BuffReading { Known = true, Power = 90 };

        AssertThat(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)).IsNotEqual(buffed);
    }

    [TestCase]
    public void TheCache_SearchesOnlyWhenTheInputChanges()
    {
        var cache = new FocusCache();
        var first = cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!);

        AssertThat(cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!)).IsSame(first);

        game.Plan.Raise(Intuition, game.ValuesFor(Intuition), game.Points, 1);
        AssertThat(cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!)).IsNotSame(first);
    }

    [TestCase]
    public void TheCache_KeepsCombatSearchesApart()
    {
        var cache = new FocusCache();
        var hit = cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!);

        AssertThat(cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!)).IsSame(hit);
        AssertThat(cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry)!)).IsNotSame(hit);
    }

    // The sheet's game with Bless shown and a learned Sword using Braveness, Agility and Strength.
    internal static PlannerGame SwordGame()
    {
        var game = PlannerSheetTests.Game();
        game.SkillTexts[SwordSkill] = new PlannerSkillText("Sword", [Braveness, Agility, Strength], 1);
        int[] sword = [40, 0, 200, 3, 0, 80];
        sword.CopyTo(game.Skills[SwordSkill], 0);
        game.SkillOrder = [BlessSkill, SwordSkill];
        return game;
    }
}
