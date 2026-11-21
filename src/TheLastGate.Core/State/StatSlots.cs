using System.Collections.Generic;
using TheLastGate.Net;

namespace TheLastGate.State;

// The server numbers the values a stat point can raise: the attributes first, then hitpoints, mana and the skills.
public static class StatSlots
{
    public const int HitPoints = 5;
    public const int Mana = 7;
    public const int FirstSkill = StatPackets.FirstSkillSlot;
    public const int Count = FirstSkill + PlayerState.SkillSlots;

    public static readonly IReadOnlyList<string> AttributeNames =
        ["Braveness", "Willpower", "Intuition", "Agility", "Strength"];

    public static int Skill(int skillNumber) => FirstSkill + skillNumber;

    public static int[] Values(PlayerState player, int slot) => slot switch
    {
        < PlayerState.AttributeCount => player.Attributes[slot],
        HitPoints => player.HitPoints,
        Mana => player.Mana,
        _ => player.Skills[slot - FirstSkill],
    };
}
