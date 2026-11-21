using System.Collections.Generic;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Stats;

public static class SkillClicks
{
    private static readonly string[] AttributeHelp =
    [
        "Braveness improves most skills and spells. It also improves your critical hit rate.",
        "Willpower improves most support spells. It also improves the speed of casting spells and using skills, "
        + "and helps overwhelm spell suppression.",
        "Intuition improves most offensive spells. It also reduces the duration of skill exhaustion.",
        "Agility improves most combat skills. It also improves your movement speed and your attack speed.",
        "Strength improves most combat skills. It also improves your movement speed and the damage dealt by your "
        + "attacks.",
    ];

    public static IReadOnlyList<LogLine> Help(string name, string description, int[] attributes) =>
    [
        new(name, (int)TextColour.Orange),
        new(description, (int)TextColour.Yellow),
        new(
            $"Affected by: {StatSlots.AttributeNames[attributes[0]]}, {StatSlots.AttributeNames[attributes[1]]}, "
            + $"{StatSlots.AttributeNames[attributes[2]]}",
            (int)TextColour.Orange),
    ];

    public static LogLine StatHelp(int slot) => new(
        slot switch
        {
            StatSlots.HitPoints => "Your Hitpoints.",
            StatSlots.Mana => "Your Mana.",
            _ => AttributeHelp[slot],
        },
        (int)TextColour.Yellow);
}
