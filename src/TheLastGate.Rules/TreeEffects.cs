using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.State;

namespace TheLastGate.Rules;

public enum TreeClass
{
    None,
    Seyan,
    ArchTemplar,
    Skald,
    Warrior,
    Sorcerer,
    Summoner,
    ArchHarakim,
    Braver,
    Lycanth,
}

public enum Corruptor
{
    FalseStrength,
    FalseAgility,
    FalseWillpower,
    FalseIntuition,
    FalseBraveness,
    FalseHarrier,
    FalsePragmatism,
    FalseBarbarism,
    FalseCelerity,
    FalseMysticism,
    FalseConcentration,
    FalseVirtuosity,
    FalseRigor,
    FalseFlexibility,
    FalseValor,
    FalseUnbreakable,
    FalseDetermination,
    FalseIndifference,
    FalseConstitution,
    FalseTactician,
    FalseZealotry,
    FalseBrilliance,
    FalseIntrospection,
    FalseWizardry,
    GuardianAngel,
    FalseFlow,
}

public sealed record CorruptorInfo(int ServerNumber, string Name, string OneCopy, bool Modelled);

// The class tree as the player has it, and the corruptor copies ticked in the Advanced window.
public sealed class TreeEffectsInput
{
    public TreeClass TreeClass { get; set; }

    public int TreePoints { get; set; }

    public byte[] TreeNodes { get; } = new byte[PlayerState.TreeNodeCount];

    public uint CorruptorTicks { get; set; }

    public int[] CorruptorCounts { get; } = new int[TreeEffects.CorruptorCount];
}

// What the class tree and corruptors add. A learned node gives its own effect only while it is uncorrupted; a corrupted
// node gives a copy of its corruptor instead, on top of any copies ticked in the Advanced window.
public static class TreeEffects
{
    public const int CorruptorCount = 26;

    private const int NodeCount = PlayerState.TreeNodeCount;

    private static readonly CorruptorInfo[] CorruptorTable =
    [
        new(20, "False Strength", "+3% Strength", true),
        new(32, "False Agility", "+3% Agility", true),
        new(68, "False Willpower", "+3% Willpower", true),
        new(80, "False Intuition", "+3% Intuition", true),
        new(92, "False Braveness", "+3% Braveness", true),
        new(44, "False Harrier", "+2% Str & Agl", true),
        new(56, "False Pragmatism", "+2% Wil & Int", true),
        new(19, "False Barbarism", "+10% Str melee", true),
        new(33, "False Celerity", "+10% Agl melee", true),
        new(67, "False Mysticism", "+10% Wil spells", true),
        new(81, "False Concentration", "+10% Int spells", true),
        new(91, "False Virtuosity", "+10% Brv passive", true),
        new(5, "False Rigor", "+2% Hit Score", true),
        new(11, "False Flexibility", "+2% Parry Score", true),
        new(89, "False Valor", "+5% Weapon Val", true),
        new(23, "False Unbreakable", "+5% Armor Val", true),
        new(7, "False Determination", "+1 WV/AV /150", true),
        new(35, "False Indifference", "+5% Immunity", true),
        new(71, "False Constitution", "+5% Resistance", true),
        new(66, "False Tactician", "Tactics by speed", false),
        new(53, "False Zealotry", "+5% Aptitude", true),
        new(9, "False Brilliance", "+1 SpB /150", true),
        new(52, "False Introspection", "+1 SpB /50 Apt", true),
        new(95, "False Wizardry", "+10% Spell Bonus", true),
        new(96, "Guardian Angel", "Bless by Brv", true),
        new(82, "False Flow", "+1 SpB /200 Mana", true),
    ];

    private static readonly (TreeClass TreeClass, int Node, PlannerEffect Effect)[] NodeEffects =
    [
        (TreeClass.Seyan, 5, new PlannerEffect(EffectField.HitPercent, 4)),
        (TreeClass.Seyan, 7, new PlannerEffect(EffectField.DeterminationNodes, 1)),
        (TreeClass.Seyan, 8, new PlannerEffect(EffectField.AttributePercent, 5, AttributeMask.All)),
        (TreeClass.Seyan, 9, new PlannerEffect(EffectField.BrillianceNodes, 1)),
        (TreeClass.Seyan, 11, new PlannerEffect(EffectField.ParryPercent, 4)),
        (TreeClass.ArchTemplar, 7, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Strength)),
        (TreeClass.ArchTemplar, 8, new PlannerEffect(EffectField.AttributePercent, 6, AttributeMask.Strength)),
        (TreeClass.ArchTemplar, 11, new PlannerEffect(EffectField.ArmorPercent, 10)),
        (TreeClass.Skald, 8, new PlannerEffect(EffectField.AttributePercent, 6, AttributeMask.Agility)),
        (TreeClass.Skald, 9, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Agility)),
        (TreeClass.Skald, 11, new PlannerEffect(EffectField.ImmunityPercent, 10)),
        (TreeClass.Warrior, 8,
            new PlannerEffect(EffectField.AttributePercent, 4, AttributeMask.Strength | AttributeMask.Agility)),
        (TreeClass.Sorcerer, 4, new PlannerEffect(EffectField.Introspection, 2)),
        (TreeClass.Sorcerer, 5, new PlannerEffect(EffectField.AptitudePercent, 10)),
        (TreeClass.Sorcerer, 8,
            new PlannerEffect(EffectField.AttributePercent, 4, AttributeMask.Willpower | AttributeMask.Intuition)),
        (TreeClass.Summoner, 7, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Willpower)),
        (TreeClass.Summoner, 8, new PlannerEffect(EffectField.AttributePercent, 6, AttributeMask.Willpower)),
        (TreeClass.Summoner, 11, new PlannerEffect(EffectField.ResistPercent, 10)),
        (TreeClass.ArchHarakim, 8, new PlannerEffect(EffectField.AttributePercent, 6, AttributeMask.Intuition)),
        (TreeClass.ArchHarakim, 9, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Intuition)),
        (TreeClass.ArchHarakim, 10, new PlannerEffect(EffectField.Flow, 2)),
        (TreeClass.Braver, 5, new PlannerEffect(EffectField.WeaponPercent, 10)),
        (TreeClass.Braver, 7, new PlannerEffect(EffectField.SkillExtraPercent, 20, AttributeMask.Braveness)),
        (TreeClass.Braver, 8, new PlannerEffect(EffectField.AttributePercent, 6, AttributeMask.Braveness)),
        (TreeClass.Braver, 11, new PlannerEffect(EffectField.SpellBonusPercent, 20)),
    ];

    private static readonly (Corruptor Corruptor, PlannerEffect Effect)[] CorruptorEffects =
    [
        (Corruptor.FalseStrength, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Strength)),
        (Corruptor.FalseAgility, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Agility)),
        (Corruptor.FalseWillpower, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Willpower)),
        (Corruptor.FalseIntuition, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Intuition)),
        (Corruptor.FalseBraveness, new PlannerEffect(EffectField.AttributePercent, 3, AttributeMask.Braveness)),
        (Corruptor.FalseHarrier,
            new PlannerEffect(EffectField.AttributePercent, 2, AttributeMask.Strength | AttributeMask.Agility)),
        (Corruptor.FalsePragmatism,
            new PlannerEffect(EffectField.AttributePercent, 2, AttributeMask.Willpower | AttributeMask.Intuition)),
        (Corruptor.FalseBarbarism, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Strength)),
        (Corruptor.FalseCelerity, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Agility)),
        (Corruptor.FalseMysticism, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Willpower)),
        (Corruptor.FalseConcentration,
            new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Intuition)),
        (Corruptor.FalseVirtuosity, new PlannerEffect(EffectField.SkillExtraPercent, 10, AttributeMask.Braveness)),
        (Corruptor.FalseRigor, new PlannerEffect(EffectField.HitPercent, 2)),
        (Corruptor.FalseFlexibility, new PlannerEffect(EffectField.ParryPercent, 2)),
        (Corruptor.FalseValor, new PlannerEffect(EffectField.WeaponPercent, 5)),
        (Corruptor.FalseUnbreakable, new PlannerEffect(EffectField.ArmorPercent, 5)),
        (Corruptor.FalseDetermination, new PlannerEffect(EffectField.Determination, 1)),
        (Corruptor.FalseIndifference, new PlannerEffect(EffectField.ImmunityPercent, 5)),
        (Corruptor.FalseConstitution, new PlannerEffect(EffectField.ResistPercent, 5)),
        (Corruptor.FalseZealotry, new PlannerEffect(EffectField.AptitudePercent, 5)),
        (Corruptor.FalseBrilliance, new PlannerEffect(EffectField.Brilliance, 1)),
        (Corruptor.FalseIntrospection, new PlannerEffect(EffectField.Introspection, 1)),
        (Corruptor.FalseWizardry, new PlannerEffect(EffectField.SpellBonusPercent, 10)),
        (Corruptor.GuardianAngel, new PlannerEffect(EffectField.GuardianAngel, 1)),
        (Corruptor.FalseFlow, new PlannerEffect(EffectField.Flow, 1)),
    ];

    private static readonly (int KindredBit, TreeClass TreeClass)[] KindredClasses =
    [
        (1, TreeClass.Seyan), (5, TreeClass.ArchTemplar), (12, TreeClass.Skald), (10, TreeClass.Warrior),
        (11, TreeClass.Sorcerer), (13, TreeClass.Summoner), (9, TreeClass.ArchHarakim), (14, TreeClass.Braver),
        (16, TreeClass.Lycanth),
    ];

    public static IReadOnlyList<CorruptorInfo> Corruptors => CorruptorTable;

    public static TreeClass ClassFromKindred(uint kindred)
    {
        foreach (var (kindredBit, treeClass) in KindredClasses)
        {
            if ((kindred & (1u << kindredBit)) != 0)
            {
                return treeClass;
            }
        }

        return TreeClass.None;
    }

    // Nodes count from 1, as the tree window shows them.
    public static bool NodeLearned(int treePoints, int node) =>
        node is >= 1 and <= NodeCount && SkillTrees.IsLearned(treePoints, node - 1);

    public static int TreeCopies(TreeEffectsInput input, Corruptor corruptor)
    {
        var copies = 0;
        for (var node = 1; node <= NodeCount; node++)
        {
            var serverNumber = input.TreeNodes[node - 1];
            if (serverNumber != 0 && NodeLearned(input.TreePoints, node)
                && CorruptorForServerNumber(serverNumber) == corruptor)
            {
                copies++;
            }
        }

        return copies;
    }

    public static bool NodeInMath(TreeClass treeClass, int node) =>
        NodeEffects.Any(effect => effect.TreeClass == treeClass && effect.Node == node);

    public static Corruptor? CorruptorForServerNumber(int serverNumber)
    {
        var index = Array.FindIndex(CorruptorTable, corruptor => corruptor.ServerNumber == serverNumber);
        return index < 0 ? null : (Corruptor)index;
    }

    public static PlannerEffects Compute(TreeEffectsInput input)
    {
        var effects = new PlannerEffects();
        foreach (var (treeClass, node, effect) in NodeEffects)
        {
            if (treeClass == input.TreeClass && NodeLearned(input.TreePoints, node) && input.TreeNodes[node - 1] == 0)
            {
                effect.AddTo(effects, 1);
            }
        }

        foreach (var (corruptor, effect) in CorruptorEffects)
        {
            var copies = TreeCopies(input, corruptor);
            if ((input.CorruptorTicks & (1u << (int)corruptor)) != 0)
            {
                copies += input.CorruptorCounts[(int)corruptor];
            }

            effect.AddTo(effects, copies);
        }

        return effects;
    }
}
