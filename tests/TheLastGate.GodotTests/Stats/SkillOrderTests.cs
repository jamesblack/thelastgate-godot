using GdUnit4;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Stats;

[TestSuite]
public class SkillOrderTests
{
    private const uint SeyanDu = 1u << 1;
    private const uint Lycanthrope = 1u << 16;
    private const uint KnowsIdentify = 1u << 18;

    [TestCase]
    public void Sorted_OrdersBySortKeyThenName()
    {
        var order = SkillOrder.Sorted(
            [Learnt(1, "Sword", 5), Learnt(2, "Dagger", 5), Learnt(3, "Axe", 2)],
            0);

        AssertThat(order).ContainsExactly(3, 2, 1);
    }

    [TestCase]
    public void Sorted_PutsLearntSkillsFirst()
    {
        var order = SkillOrder.Sorted([Unlearnt(1, "Axe", 0), Learnt(2, "Sword", 9)], 0);

        AssertThat(order).ContainsExactly(2, 1);
    }

    [TestCase]
    public void Sorted_PutsClassSkillsBeforeOtherSkills()
    {
        var order = SkillOrder.Sorted([Learnt(1, "Axe", 0), Learnt(50, "Zeal", 9)], 0);

        AssertThat(order).ContainsExactly(50, 1);
    }

    [TestCase]
    public void Sorted_PutsIdentifyLastUnlessTheCharacterKnowsIt()
    {
        SkillEntry[] skills = [Learnt(52, "Identify", 0), Learnt(1, "Axe", 9)];

        AssertThat(SkillOrder.Sorted(skills, 0)).ContainsExactly(1, 52);
        AssertThat(SkillOrder.Sorted(skills, KnowsIdentify)).ContainsExactly(52, 1);
    }

    [TestCase]
    public void Sorted_PutsTheLycanthropeSkillsLastForOthers()
    {
        SkillEntry[] skills = [Learnt(53, "Shift", 0), Learnt(1, "Axe", 9)];

        AssertThat(SkillOrder.Sorted(skills, 0)).ContainsExactly(1, 53);
        AssertThat(SkillOrder.Sorted(skills, Lycanthrope)).ContainsExactly(53, 1);
    }

    [TestCase]
    public void Sorted_PutsSkillsThatWorkUnlearntFirstAmongTheUnlearnt()
    {
        var order = SkillOrder.Sorted([Unlearnt(1, "Axe", 0), Unlearnt(8, "Stealth", 9)], 0);

        AssertThat(order).ContainsExactly(8, 1);
    }

    [TestCase]
    public void Sorted_PutsTheSeyanDuSkillLastForOthers()
    {
        SkillEntry[] skills = [Learnt(44, "Proximity", 0), Learnt(1, "Axe", 9)];

        AssertThat(SkillOrder.Sorted(skills, 0)).ContainsExactly(1, 44);
        AssertThat(SkillOrder.Sorted(skills, SeyanDu)).ContainsExactly(44, 1);
    }

    [TestCase]
    public void Sorted_PutsNegativeSortKeysFirst()
    {
        var order = SkillOrder.Sorted([Learnt(1, "Axe", 1), Learnt(2, "Bow", -1)], 0);

        AssertThat(order).ContainsExactly(2, 1);
    }

    private static SkillEntry Learnt(int number, string name, int sortKey) => new(number, name, sortKey, true);

    private static SkillEntry Unlearnt(int number, string name, int sortKey) => new(number, name, sortKey, false);
}
