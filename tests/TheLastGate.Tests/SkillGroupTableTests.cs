using TheLastGate.Rules;

namespace TheLastGate.Tests;

public class SkillGroupTableTests
{
    [Theory]
    [InlineData(0, SkillGroups.Passive)]
    [InlineData(11, SkillGroups.ActiveSpell)]
    [InlineData(13, SkillGroups.ActiveMelee)]
    [InlineData(31, SkillGroups.Passive | SkillGroups.ActiveMelee)]
    [InlineData(50, SkillGroups.None)]
    public void Of_SaysWhichGroupsASkillIsIn(int skillNumber, SkillGroups groups)
    {
        Assert.Equal(groups, SkillGroupTable.Of(skillNumber));
    }
}
