using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class TreeEffectsTests
{
    [Fact]
    public void NoClass_HasNoEffects()
    {
        Assert.Equal(new PlannerEffects(), TreeEffects.Compute(new TreeEffectsInput()));
    }

    [Fact]
    public void NodeLearned_ReadsTheNodesBitInTheTreePoints()
    {
        var points = 1 << (12 - 5 + 4);

        Assert.True(TreeEffects.NodeLearned(points, 5));
        Assert.False(TreeEffects.NodeLearned(points, 4));
    }

    [Fact]
    public void SeyanNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Seyan, 5, 7, 8, 9, 11));

        Assert.Equal(4, effects.HitPercent);
        Assert.Equal(1, effects.DeterminationNodes);
        Assert.Equal(0, effects.Determination);
        foreach (var attribute in Enum.GetValues<AttributeSlot>())
        {
            Assert.Equal(5, effects.AttributePercent[attribute]);
        }

        Assert.Equal(1, effects.BrillianceNodes);
        Assert.Equal(0, effects.Brilliance);
        Assert.Equal(4, effects.ParryPercent);
    }

    [Fact]
    public void ArchTemplarNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.ArchTemplar, 7, 8, 11));

        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Strength]);
        Assert.Equal(6, effects.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(10, effects.ArmorPercent);
    }

    [Fact]
    public void SkaldNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Skald, 8, 9, 11));

        Assert.Equal(6, effects.AttributePercent[AttributeSlot.Agility]);
        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Agility]);
        Assert.Equal(10, effects.ImmunityPercent);
    }

    [Fact]
    public void WarriorNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Warrior, 8));

        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Agility]);
    }

    [Fact]
    public void SorcererNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Sorcerer, 4, 5, 8));

        Assert.Equal(2, effects.Introspection);
        Assert.Equal(10, effects.AptitudePercent);
        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Willpower]);
        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Intuition]);
    }

    [Fact]
    public void SummonerNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Summoner, 7, 8, 11));

        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Willpower]);
        Assert.Equal(6, effects.AttributePercent[AttributeSlot.Willpower]);
        Assert.Equal(10, effects.ResistPercent);
    }

    [Fact]
    public void ArchHarakimNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.ArchHarakim, 8, 9, 10));

        Assert.Equal(6, effects.AttributePercent[AttributeSlot.Intuition]);
        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Intuition]);
        Assert.Equal(2, effects.Flow);
    }

    [Fact]
    public void BraverNodes_GiveTheirEffects()
    {
        var effects = TreeEffects.Compute(ClassWithNodes(TreeClass.Braver, 5, 7, 8, 11));

        Assert.Equal(10, effects.WeaponPercent);
        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Braveness]);
        Assert.Equal(6, effects.AttributePercent[AttributeSlot.Braveness]);
        Assert.Equal(20, effects.SpellBonusPercent);
    }

    [Fact]
    public void CorruptorCopies_Stack()
    {
        Assert.Equal(6, Ticked(Corruptor.FalseStrength, 2).AttributePercent[AttributeSlot.Strength]);

        var harrier = Ticked(Corruptor.FalseHarrier, 1);
        Assert.Equal(2, harrier.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(2, harrier.AttributePercent[AttributeSlot.Agility]);

        Assert.Equal(3, Ticked(Corruptor.GuardianAngel, 3).GuardianAngel);
        Assert.Equal(2, Ticked(Corruptor.FalseFlow, 2).Flow);
    }

    [Fact]
    public void AnUntickedCount_IsIgnored()
    {
        var input = new TreeEffectsInput();
        input.CorruptorCounts[(int)Corruptor.FalseStrength] = 4;

        Assert.Equal(new PlannerEffects(), TreeEffects.Compute(input));
    }

    [Fact]
    public void ACorruptedNode_GivesItsCorruptorInsteadOfItsOwnEffect()
    {
        var input = ClassWithNodes(TreeClass.Seyan, 5);
        input.TreeNodes[4] = 89;

        var effects = TreeEffects.Compute(input);

        Assert.Equal(0, effects.HitPercent);
        Assert.Equal(5, effects.WeaponPercent);
        Assert.Equal(1, TreeEffects.TreeCopies(input, Corruptor.FalseValor));
    }

    [Fact]
    public void ANodeCorruptedWithAnUnknownCorruptor_GivesNothing()
    {
        var input = ClassWithNodes(TreeClass.Seyan, 5);
        input.TreeNodes[4] = 13;

        Assert.Equal(new PlannerEffects(), TreeEffects.Compute(input));
    }

    [Fact]
    public void TreeAndTickedCopies_AddUp()
    {
        var input = ClassWithNodes(TreeClass.Seyan, 5);
        input.TreeNodes[4] = 5;
        Tick(input, Corruptor.FalseRigor, 2);

        Assert.Equal(6, TreeEffects.Compute(input).HitPercent);
    }

    [Fact]
    public void ACorruptedNodeNotLearned_CountsNothing()
    {
        var input = new TreeEffectsInput { TreeClass = TreeClass.Seyan };
        input.TreeNodes[4] = 5;

        Assert.Equal(new PlannerEffects(), TreeEffects.Compute(input));
        Assert.Equal(0, TreeEffects.TreeCopies(input, Corruptor.FalseRigor));
    }

    [Fact]
    public void FalseTactician_IsListedButNotModelled()
    {
        Assert.False(TreeEffects.Corruptors[(int)Corruptor.FalseTactician].Modelled);
        Assert.Equal(new PlannerEffects(), Ticked(Corruptor.FalseTactician, 3));
    }

    [Fact]
    public void Corruptors_AreKnownByTheServersNumbers()
    {
        int[] expected =
            [20, 32, 68, 80, 92, 44, 56, 19, 33, 67, 81, 91, 5, 11, 89, 23, 7, 35, 71, 66, 53, 9, 52, 95, 96, 82];

        Assert.Equal(expected, TreeEffects.Corruptors.Select(corruptor => corruptor.ServerNumber));
        foreach (var corruptor in Enum.GetValues<Corruptor>())
        {
            Assert.Equal(corruptor, TreeEffects.CorruptorForServerNumber(expected[(int)corruptor]));
        }

        Assert.Null(TreeEffects.CorruptorForServerNumber(13));
    }

    [Theory]
    [InlineData(1u << 1, TreeClass.Seyan)]
    [InlineData(1u << 5, TreeClass.ArchTemplar)]
    [InlineData(1u << 12, TreeClass.Skald)]
    [InlineData(1u << 10, TreeClass.Warrior)]
    [InlineData(1u << 11, TreeClass.Sorcerer)]
    [InlineData(1u << 13, TreeClass.Summoner)]
    [InlineData(1u << 9, TreeClass.ArchHarakim)]
    [InlineData(1u << 14, TreeClass.Braver)]
    [InlineData(1u << 16, TreeClass.Lycanth)]
    [InlineData(0u, TreeClass.None)]
    public void ClassFromKindred_ReadsTheClassBit(uint kindred, TreeClass treeClass)
    {
        Assert.Equal(treeClass, TreeEffects.ClassFromKindred(kindred));
    }

    [Fact]
    public void NodeInMath_IsOnlyForNodesWithAnEffect()
    {
        Assert.True(TreeEffects.NodeInMath(TreeClass.Seyan, 5));
        Assert.False(TreeEffects.NodeInMath(TreeClass.Seyan, 6));
        Assert.False(TreeEffects.NodeInMath(TreeClass.Lycanth, 8));
    }

    private static TreeEffectsInput ClassWithNodes(TreeClass treeClass, params int[] nodes)
    {
        var input = new TreeEffectsInput { TreeClass = treeClass };
        foreach (var node in nodes)
        {
            input.TreePoints |= 1 << (12 - node + 4);
        }

        return input;
    }

    private static PlannerEffects Ticked(Corruptor corruptor, int count)
    {
        var input = new TreeEffectsInput();
        Tick(input, corruptor, count);
        return TreeEffects.Compute(input);
    }

    private static void Tick(TreeEffectsInput input, Corruptor corruptor, int count)
    {
        input.CorruptorTicks |= 1u << (int)corruptor;
        input.CorruptorCounts[(int)corruptor] = count;
    }
}
