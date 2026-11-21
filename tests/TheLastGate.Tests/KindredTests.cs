using TheLastGate.State;

namespace TheLastGate.Tests;

public class KindredTests
{
    [Theory]
    [InlineData(3, true, 0u, true)]
    [InlineData(3, false, 0u, false)]
    [InlineData(50, false, 0u, true)]
    [InlineData(51, false, 0u, true)]
    [InlineData(52, false, 0u, false)]
    [InlineData(52, false, Kindred.KnowsIdentify, true)]
    [InlineData(53, false, 0u, false)]
    [InlineData(53, false, Kindred.Lycanthrope, true)]
    [InlineData(54, false, Kindred.Lycanthrope, true)]
    public void IsKnown_CountsLearntSkillsAndTheClassSkillsTheCharacterHas(
        int skillNumber,
        bool learnt,
        uint kindred,
        bool known)
    {
        Assert.Equal(known, Kindred.IsKnown(skillNumber, learnt, kindred));
    }
}
