using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Tests;

public class SkillClicksTests
{
    [Fact]
    public void Help_ShowsTheNameTheDescriptionAndTheAttributesThatAffectTheSkill()
    {
        var lines = SkillClicks.Help("Dagger", "Fight with a dagger.", [3, 4, 0]);

        Assert.Equal(
            [
                new LogLine("Dagger", (int)TextColour.Orange),
                new LogLine("Fight with a dagger.", (int)TextColour.Yellow),
                new LogLine("Affected by: Agility, Strength, Braveness", (int)TextColour.Orange),
            ],
            lines);
    }

    [Theory]
    [InlineData(0, "Braveness improves most skills and spells. It also improves your critical hit rate.")]
    [InlineData(
        1,
        "Willpower improves most support spells. It also improves the speed of casting spells and using skills, "
        + "and helps overwhelm spell suppression.")]
    [InlineData(2, "Intuition improves most offensive spells. It also reduces the duration of skill exhaustion.")]
    [InlineData(
        3,
        "Agility improves most combat skills. It also improves your movement speed and your attack speed.")]
    [InlineData(
        4,
        "Strength improves most combat skills. It also improves your movement speed and the damage dealt by your "
        + "attacks.")]
    [InlineData(StatSlots.HitPoints, "Your Hitpoints.")]
    [InlineData(StatSlots.Mana, "Your Mana.")]
    public void StatHelp_DescribesEachStat(int slot, string text)
    {
        Assert.Equal(new LogLine(text, (int)TextColour.Yellow), SkillClicks.StatHelp(slot));
    }
}
