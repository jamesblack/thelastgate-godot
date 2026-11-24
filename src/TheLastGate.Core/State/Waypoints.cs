using System.Collections.Generic;

namespace TheLastGate.State;

public readonly record struct Waypoint(int Number, string Name, string Description);

// A waypoint's number is its bit in the found-waypoints mask and what the travel command carries; the window lists
// them roughly by region rather than by number.
public static class Waypoints
{
    public const int Count = 27;

    public static IReadOnlyList<Waypoint> All { get; } =
    [
        new(0, "Lynbore, Tavern", "\"Humble beginnings.\""),
        new(1, "Lynbore, East", "\"Ghosts and spiders duel.\""),
        new(2, "Aston, South", "\"The Weeping Woods.\""),
        new(3, "Aston, Crossroads", "\"Between ivory and violet.\""),
        new(4, "Aston, West", "\"The Strange Forest.\""),
        new(5, "Aston, East", "\"The Autumn Meadow.\""),
        new(6, "Forgotten Canyon", "\"Old secrets echo.\""),
        new(20, "Lizard Temple, South", "\"Where reptiles implore.\""),
        new(7, "Lizard Temple, North", "\"The Beryl Jungle.\""),
        new(21, "Lizard Settlement, East", "\"Betwixt emerald and magma.\""),
        new(22, "The Emerald Cavern, West", "\"A feudal audience.\""),
        new(8, "Temple of Osiris", "\"Gods toy with greed.\""),
        new(23, "The Tower", "\"Sky-piercing obelisk.\""),
        new(9, "Neiseer, West", "\"The Basalt Desert.\""),
        new(10, "Neiseer", "\"Twilit stars sing sweetly.\""),
        new(25, "Neiseer, South", "\"The Raging Rivers.\""),
        new(24, "Neiseer, East", "\"The Violet Bog.\""),
        new(11, "Neiseer, North", "\"The Ravaged Prairie.\""),
        new(12, "Mausoleum, Basement", "\"An accursed tomb.\""),
        new(13, "Pentagram Quest, Novice", "\"Endless adventure begins.\""),
        new(14, "Pentagram Quest, Earth", "\"Smells of dirt and soot.\""),
        new(15, "Pentagram Quest, Fire", "\"Boiling and burning.\""),
        new(16, "Pentagram Quest, Jungle", "\"A verdant expanse.\""),
        new(17, "Pentagram Quest, Ice", "\"Frozen still for eternity.\""),
        new(18, "Pentagram Quest, Underwater", "\"Embrace foul waters.\""),
        new(19, "Pentagram Quest, Onyx", "\"Depths known only by fear.\""),
        new(26, "The Burning Plains", "\"The archon waits.\""),
    ];

    public static bool Found(int foundMask, int number) => (foundMask & (1 << number)) != 0;
}
