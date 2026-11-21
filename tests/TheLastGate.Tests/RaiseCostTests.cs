using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class RaiseCostTests
{
    private const int Dagger = 3;

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 133)]
    public void AnAttribute_CostsItsValueCubedTimesItsDifficultyOverTwenty(int planned, int cost)
    {
        Assert.Equal(cost, RaiseCost.At(2, [10, 0, 50, 2], planned));
    }

    [Theory]
    [InlineData(StatSlots.HitPoints)]
    [InlineData(StatSlots.Mana)]
    public void HitPointsAndMana_CostTheirValueTimesTheirDifficulty(int slot)
    {
        Assert.Equal(180, RaiseCost.At(slot, [60, 0, 500, 3], 0));
    }

    [Fact]
    public void ASkill_CostsItsValueCubedTimesItsDifficultyOverFortyButAtLeastItsValue()
    {
        Assert.Equal(3, RaiseCost.At(StatSlots.Skill(Dagger), [3, 0, 50, 1], 0));
        Assert.Equal(50, RaiseCost.At(StatSlots.Skill(Dagger), [10, 0, 50, 2], 0));
    }

    [Fact]
    public void AValueTheCharacterLacks_CantBeRaised()
    {
        Assert.Null(RaiseCost.At(StatSlots.Skill(Dagger), [0, 0, 50, 2], 0));
    }

    [Fact]
    public void AValueAtItsLimit_CantBeRaised()
    {
        Assert.NotNull(RaiseCost.At(2, [49, 0, 50, 1], 0));
        Assert.Null(RaiseCost.At(2, [49, 0, 50, 1], 1));
    }
}
