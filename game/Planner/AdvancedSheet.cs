using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Buffs;
using TheLastGate.Rules;

namespace TheLastGate.Planner;

// A spell's row in the Advanced window; it can only be toggled once the buff model can show it.
public sealed record AdvancedSpell(BuffKind Kind, string Label, bool Ready, string? Warning);

// What the planner's Advanced window shows besides the choices themselves: the spells you know, the learned tree nodes
// in the math, and how many copies of each corruptor the tree already has.
public sealed class AdvancedSheet
{
    public const string CorruptorCountHelp =
        "Copies on tarot cards and sinbinder rings. A sinbinder with a Sign of Symmetry counts twice.";

    public const string EnchantCountHelp = "Copies on your gear. One on a two-handed weapon counts twice.";
    public const string TreeRowEffect = "From skill tree";

    private static readonly string[] Numerals =
    [
        "0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI",
        "XVII", "XVIII", "XIX", "XX", "XXI",
    ];

    public IReadOnlyList<AdvancedSpell> Spells { get; private init; } = [];

    public IReadOnlyList<string> TreeRows { get; private init; } = [];

    // By corruptor.
    public IReadOnlyList<int> TreeCopies { get; private init; } = [];

    public static AdvancedSheet For(PlannerSheet sheet)
    {
        var treeInput = sheet.TreeInput();
        return new AdvancedSheet
        {
            Spells = Enum.GetValues<BuffKind>()
                .Where(kind => sheet.Known(PlannerSheet.SpellSkillNumbers[(int)kind]))
                .Select(kind => Spell(sheet, kind))
                .ToArray(),
            TreeRows = sheet.TreeLines().ToArray(),
            TreeCopies = Enumerable.Range(0, TreeEffects.CorruptorCount)
                .Select(corruptor => TreeEffects.TreeCopies(treeInput, (Corruptor)corruptor))
                .ToArray(),
        };
    }

    public static string CardLabel(TarotCard card) =>
        $"{Numerals[(int)card % Numerals.Length]} {TarotMath.CardNames[(int)card]}";

    public static string CorruptorEffect(int corruptor) =>
        TreeEffects.Corruptors[corruptor] is { Modelled: true } info ? info.OneCopy : "(not modelled)";

    public static string GearEffect(int gear)
    {
        var info = ItemEffects.Gears[gear];
        return info.Modelled ? info.Effect : $"{info.Effect} (not modelled)";
    }

    public static string TreeCopiesText(int copies) => copies > 0 ? $"+{copies} tree" : "";

    // A buff on you can only be modelled once its power has been read, and a weaker cast of your own doesn't replace
    // it.
    private static AdvancedSpell Spell(PlannerSheet sheet, BuffKind kind)
    {
        var game = sheet.Game;
        var name = game.SkillTexts[PlannerSheet.SpellSkillNumbers[(int)kind]].Name;
        var reading = game.BuffReadings[(int)kind];
        var ready = sheet.Planned != null && (!game.BuffActive[(int)kind] || reading != null);
        var toggledOn = ((game.Choices.SpellToggles >> (int)kind) & 1) != 0;
        var castPower = sheet.Planned?.CastPower[(int)kind] ?? 0;
        var warning = ready && toggledOn && reading != null && castPower < reading.Power
            ? $"Your {name} ({castPower}) won't replace the active one ({reading.Power})"
            : null;
        return new AdvancedSpell(kind, ready ? name : $"{name} (reading buffs...)", ready, warning);
    }
}
