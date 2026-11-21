using TheLastGate.Buffs;
using TheLastGate.Planner;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Tests;

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

    private static readonly int SwordSlot = StatPlan.SkillSlot(SwordSkill);
    private static readonly int BlessSlot = StatPlan.SkillSlot(BlessSkill);

    private readonly PlannerGame game = SwordGame();

    [Fact]
    public void ThePlainInput_ReadsTheShownAttributesAndTheirCosts()
    {
        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        Assert.Equal([AttributeSlot.Braveness, AttributeSlot.Agility, AttributeSlot.Strength], input.Attributes);
        Assert.Equal([61, 61, 61], input.AttributeValues);
        Assert.Equal([9600, 10338, 11113, 11926, 12777, 13668], input.AttributeStepCosts[2]);
        Assert.Equal(5, input.Divisor);
        Assert.Equal(4800, input.DirectCost);
        Assert.False(input.FoolReversed);
        Assert.Equal(9600, input.PoolStepCosts[Willpower][0]);
    }

    [Fact]
    public void ThePlainInput_MarksCappedPoints()
    {
        game.Attributes[Strength][2] = 42;
        game.Skills[SwordSkill][2] = 40;

        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        Assert.Equal([9600, 10338, NoCost, NoCost, NoCost, NoCost], input.AttributeStepCosts[2]);
        Assert.Equal(NoCost, input.DirectCost);
    }

    [Fact]
    public void ThePlainInput_WithoutTheBuffModelAddsThePlannedPoints()
    {
        game.MetaStats.Clear();
        game.Plan.Raise(Agility, game.ValuesFor(Agility), game.Points, 2);

        var input = FocusInputs.Plain(PlannerSheet.For(game), SwordSkill);

        Assert.Equal([50, 52, 50], input.AttributeValues);
        Assert.Equal(11113, input.AttributeStepCosts[1][0]);
    }

    [Fact]
    public void TheBuffedInput_TriesTheSkillItsAttributesAndBless()
    {
        var input = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!;

        Assert.Equal(SwordSlot, input.FocusedRaiseIndex);
        Assert.Equal([BlessSlot, BuffFocus.NoSlot, BuffFocus.NoSlot], input.SpellRaiseIndex);
        Assert.Equal(80, input.Focused.ServerTotal);
        Assert.False(input.ArmorFeedsFocused);
        Assert.Equal(
            [
                (SwordSlot, 1), (Braveness, 6), (Agility, 6), (Strength, 6), (Willpower, 6), (Intuition, 6),
                (BlessSlot, 16),
            ],
            input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)));
        Assert.Equal([4800, NoCost], input.Candidates[0].StepCosts.Take(2));
        Assert.Equal([9600, 10338, 11113, 11926, 12777, 13668, NoCost], input.Candidates[3].StepCosts.Take(7));
        Assert.Equal(2025, input.Candidates[6].StepCosts[0]);
    }

    [Fact]
    public void TheBuffedInput_UnderTheReverseFoolBuysAttributesFromOnePool()
    {
        game.Choices.TarotCards = 1ul << (int)TarotCard.FoolReversed;

        var input = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!;

        Assert.Equal(
            [(SwordSlot, 1), (BuffFocus.AttributePool, 10), (BlessSlot, 16)],
            input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)));
        var pool = input.Candidates[1].StepCosts;
        Assert.Equal(Enumerable.Repeat(9600, 5), pool.Take(5));
        Assert.Equal(Enumerable.Repeat(10338, 5), pool.Skip(5).Take(5));
        Assert.All(pool.Skip(10), cost => Assert.Equal(NoCost, cost));
    }

    [Fact]
    public void TheBuffedInput_NeedsTheBuffModel()
    {
        game.MetaStats.Clear();

        Assert.Null(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill));
    }

    [Fact]
    public void TheCombatInput_TriesTheSkillsThatMoveTheStat()
    {
        var input = FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!;

        Assert.Equal(CombatStat.Hit, input.Stat);
        Assert.Equal(CombatWeapon.Sword, input.Combat.Weapon);
        Assert.Equal(SwordSlot, input.CombatRaiseIndex[(int)CombatSkill.Sword]);
        Assert.Equal(BuffFocus.NoSlot, input.CombatRaiseIndex[(int)CombatSkill.Tactics]);
        Assert.Equal(
            [
                (SwordSlot, 16), (BlessSlot, 16), (Braveness, 6), (Willpower, 6), (Intuition, 6), (Agility, 6),
                (Strength, 6),
            ],
            input.Candidates.Select(candidate => (candidate.RaiseIndex, candidate.MostPoints)));
    }

    [Fact]
    public void TheCombatInput_NeedsHitAndParry()
    {
        game.MetaStats.Remove("Hit Score");

        Assert.Null(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit));
    }

    [Fact]
    public void InputsFromTheSameNumbers_AreEqual()
    {
        Assert.Equal(
            FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill),
            FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill));
        Assert.Equal(
            FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry),
            FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry));
    }

    [Fact]
    public void APlannedPoint_MakesTheInputsDiffer()
    {
        var buffed = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill);
        var combat = FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry);

        game.Plan.Raise(Intuition, game.ValuesFor(Intuition), game.Points, 1);

        Assert.NotEqual(buffed, FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill));
        Assert.NotEqual(combat, FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry));
    }

    [Fact]
    public void ANewBuffReading_MakesTheInputsDiffer()
    {
        var buffed = FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill);

        game.BuffActive[(int)BuffKind.Bless] = true;
        game.BuffReadings[(int)BuffKind.Bless] = new BuffReading { Known = true, Power = 90 };

        Assert.NotEqual(buffed, FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill));
    }

    [Fact]
    public void TheCache_SearchesOnlyWhenTheInputChanges()
    {
        var cache = new FocusCache();
        var first = cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!);

        Assert.Same(first, cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!));

        game.Plan.Raise(Intuition, game.ValuesFor(Intuition), game.Points, 1);
        Assert.NotSame(first, cache.Buff(FocusInputs.Buffed(PlannerSheet.For(game), SwordSkill)!));
    }

    [Fact]
    public void TheCache_KeepsCombatSearchesApart()
    {
        var cache = new FocusCache();
        var hit = cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!);

        Assert.Same(hit, cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Hit)!));
        Assert.NotSame(hit, cache.Combat(FocusInputs.Combat(PlannerSheet.For(game), CombatStat.Parry)!));
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
