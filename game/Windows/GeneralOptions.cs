using System;
using System.Collections.Generic;
using TheLastGate.World;

namespace TheLastGate.Windows;

public enum GeneralOption
{
    EscapeClosesWindowsFirst,
    GiveMore,
    UseQueue,
    EnterToTalk,
    ExpandedQuickStats,
    SkillPlanner,
    AdaptivePacing,
    ItemTooltips,
    ItemTooltipsAlways,
    GlideMovement,
    PickTileDrawnUnderMouse,
    StubbornUse,
}

public enum OptionSection
{
    Classic,
    Godot,
    Experimental,
}

public sealed record OptionRow(GeneralOption Option, string Label, bool Default, OptionSection Section)
{
    public GeneralOption? Under { get; init; }
}

// The General tab's checkboxes: the current client's in its own words, then this client's own, which start off.
public static class GeneralOptions
{
    public const string GodotHeading = "Godot Client";

    public static IReadOnlyList<OptionRow> Rows { get; } =
    [
        new(
            GeneralOption.EscapeClosesWindowsFirst,
            "Escape closes windows before canceling movement.",
            true,
            OptionSection.Classic),
        new(
            GeneralOption.GiveMore,
            "Holding Ctrl+Shift when giving an item will attempt to reload that item.",
            false,
            OptionSection.Classic),
        new(
            GeneralOption.UseQueue,
            "Holding Ctrl+Shift when using an item queues that use. Limit 5.",
            false,
            OptionSection.Classic),
        new(
            GeneralOption.EnterToTalk,
            "Press Enter to activate chat mode before typing. Press Enter again to send, or ESC to cancel.",
            false,
            OptionSection.Classic),
        new(
            GeneralOption.ExpandedQuickStats,
            "Show Hit/Parry Score with Weapon/Armor value.",
            true,
            OptionSection.Classic),
        new(
            GeneralOption.SkillPlanner,
            "Enable the skill planner: the Planner button below the skill list and its hotkey.",
            false,
            OptionSection.Classic),
        new(
            GeneralOption.AdaptivePacing,
            "Adaptive network smoothing: lower delay on steady connections, more buffering when the connection is uneven.",
            false,
            OptionSection.Classic),
        new(
            GeneralOption.ItemTooltips,
            "Show item tooltips for inventory and equipped gear.",
            true,
            OptionSection.Classic),
        new(
            GeneralOption.ItemTooltipsAlways,
            "Always show tooltips on hover. When off, hold the Item Tooltip hotkey to show them.",
            true,
            OptionSection.Classic) { Under = GeneralOption.ItemTooltips },
        new(
            GeneralOption.GlideMovement,
            "Interpolate character/map movement for smoothness",
            false,
            OptionSection.Godot),
        new(
            GeneralOption.PickTileDrawnUnderMouse,
            "When walking, click the tile drawn under the mouse rather than where the map was.",
            false,
            OptionSection.Godot),
        new(
            GeneralOption.StubbornUse,
            $"Keep retrying a use that doesn't go through, up to {StubbornUse.MostFailures} times (Stubborn use).",
            false,
            OptionSection.Experimental),
    ];

    public static bool IsShown(OptionRow row, Func<GeneralOption, bool> isOn) =>
        row.Under is not { } parent || isOn(parent);

    public static bool ShowsItemTooltips(bool itemTooltips, bool always, bool keyHeld) =>
        itemTooltips && (always || keyHeld);

    public static bool EscapeSendsReset(bool escapeClosesWindowsFirst, bool closedAWindow) =>
        !escapeClosesWindowsFirst || !closedAWindow;
}
