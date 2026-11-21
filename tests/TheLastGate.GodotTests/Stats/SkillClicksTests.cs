using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class SkillClicksTests
{
    [TestCase]
    public void Help_ShowsTheNameTheDescriptionAndTheAttributesThatAffectTheSkill()
    {
        var lines = SkillClicks.Help("Dagger", "Fight with a dagger.", [3, 4, 0]);

        AssertThat(lines)
            .ContainsExactly(
                new LogLine("Dagger", (int)TextColour.Orange),
                new LogLine("Fight with a dagger.", (int)TextColour.Yellow),
                new LogLine("Affected by: Agility, Strength, Braveness", (int)TextColour.Orange));
    }

    [TestCase(0, "Braveness improves most skills and spells. It also improves your critical hit rate.")]
    [TestCase(
        1,
        "Willpower improves most support spells. It also improves the speed of casting spells and using skills, "
        + "and helps overwhelm spell suppression.")]
    [TestCase(2, "Intuition improves most offensive spells. It also reduces the duration of skill exhaustion.")]
    [TestCase(
        3,
        "Agility improves most combat skills. It also improves your movement speed and your attack speed.")]
    [TestCase(
        4,
        "Strength improves most combat skills. It also improves your movement speed and the damage dealt by your "
        + "attacks.")]
    [TestCase(StatSlots.HitPoints, "Your Hitpoints.")]
    [TestCase(StatSlots.Mana, "Your Mana.")]
    public void StatHelp_DescribesEachStat(int slot, string text)
    {
        AssertThat(SkillClicks.StatHelp(slot)).IsEqual(new LogLine(text, (int)TextColour.Yellow));
    }
}
