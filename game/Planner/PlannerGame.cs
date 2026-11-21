using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Buffs;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Planner;

// A skill as the server's skill table describes it.
public sealed record PlannerSkillText(string Name, IReadOnlyList<int> Attributes, int Show);

// How a meta stat is written: a decimal one is sent in hundredths.
public sealed record MetaFormat(bool Decimal, string Affix);

// What the planner works from: the player's numbers copied out of the game, and the plan and choices it changes.
public sealed class PlannerGame
{
    public const int StatValueCount = PlayerState.StatValueCount;

    private const int ScoreHigh = 4;
    private const int ScoreLow = 5;

    public int Points { get; set; }

    public uint Kindred { get; set; }

    public bool HardMode { get; set; }

    public int Weapon { get; set; }

    public int Armor { get; set; }

    public int TreePoints { get; set; }

    public byte[] TreeNodes { get; } = new byte[PlayerState.TreeNodeCount];

    public int[] WornSprites { get; } = new int[PlayerState.WornSlots];

    public int[][] Attributes { get; } = Values(PlayerState.AttributeCount);

    public int[][] Skills { get; } = Values(PlayerState.SkillSlots);

    public int[] HitPoints { get; } = new int[StatValueCount];

    public int[] Mana { get; } = new int[StatValueCount];

    public PlannerSkillText[] SkillTexts { get; set; } = [];

    // The skill numbers in the order the skill list shows them.
    public IReadOnlyList<int> SkillOrder { get; set; } = [];

    // The meta stats that have arrived, by name.
    public Dictionary<string, int> MetaStats { get; } = [];

    public Dictionary<string, MetaFormat> MetaFormats { get; } = [];

    // The names of the class tree's nodes, first node first.
    public string[] TreeNodeNames { get; } = Enumerable.Repeat("", PlayerState.TreeNodeCount).ToArray();

    public bool[] BuffActive { get; } = new bool[BuffModelInput.BuffCount];

    public BuffReading?[] BuffReadings { get; } = new BuffReading?[BuffModelInput.BuffCount];

    public StatPlan Plan { get; set; } = new();

    public PlannerChoices Choices { get; set; } = new();

    public static PlannerGame From(
        PlayerState player,
        Terminology terminology,
        StatPlan plan,
        PlannerChoices choices,
        BuffWatch buffs,
        bool hardMode)
    {
        var game = new PlannerGame
        {
            Points = player.Points,
            Kindred = player.Kindred,
            HardMode = hardMode,
            Weapon = player.Weapon,
            Armor = player.Armor,
            TreePoints = player.TreePoints,
            SkillTexts = terminology.Skills
                .Select(text => new PlannerSkillText(text.Name, text.Attributes.ToArray(), text.Show))
                .ToArray(),
            SkillOrder = Stats.SkillOrder.Sorted(
                terminology.Skills.Select((text, number) =>
                    new SkillEntry(number, text.Name, text.SortKey, player.Skills[number][0] != 0)),
                player.Kindred),
            Plan = plan,
            Choices = choices,
        };
        player.TreeNodes.CopyTo(game.TreeNodes, 0);
        for (var slot = 0; slot < PlayerState.WornSlots; slot++)
        {
            game.WornSprites[slot] = player.Worn[slot].Sprite;
        }

        CopyValues(player.Attributes, game.Attributes);
        CopyValues(player.Skills, game.Skills);
        player.HitPoints.CopyTo(game.HitPoints, 0);
        player.Mana.CopyTo(game.Mana, 0);
        foreach (var meta in terminology.MetaStats)
        {
            if (meta.Name.Length > 0 && game.MetaStats.TryAdd(meta.Name, meta.Value))
            {
                game.MetaFormats[meta.Name] = new MetaFormat(meta.Flag != 0, meta.Affix);
            }
        }

        var classTree = terminology.Trees[0];
        for (var node = 0; node < PlayerState.TreeNodeCount; node++)
        {
            game.TreeNodeNames[node] = classTree[node].Name;
        }

        foreach (var kind in Enum.GetValues<BuffKind>())
        {
            game.BuffActive[(int)kind] = buffs.IsActive(kind);
            game.BuffReadings[(int)kind] = buffs.Reading(kind);
        }

        return game;
    }

    public int[] ValuesFor(int slot) => slot switch
    {
        < PlayerState.AttributeCount => Attributes[slot],
        StatPlan.HitPointsSlot => HitPoints,
        StatPlan.ManaSlot => Mana,
        _ => Skills[slot - StatPlan.FirstSkillSlot],
    };

    public static int Score(int[] values) => (values[ScoreHigh] << 8) | values[ScoreLow];

    private static void CopyValues(int[][] from, int[][] to)
    {
        for (var index = 0; index < from.Length; index++)
        {
            from[index].CopyTo(to[index], 0);
        }
    }

    private static int[][] Values(int count)
    {
        var values = new int[count][];
        for (var index = 0; index < count; index++)
        {
            values[index] = new int[StatValueCount];
        }

        return values;
    }
}
