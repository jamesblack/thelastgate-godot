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
    DivinerCard,
}

public enum OptionSection
{
    Chat,
    Informational,
    Networking,
    Miscellaneous,
    Experimental,
    ClassHelpers,
}

public sealed record OptionRow(GeneralOption Option, string Label, bool Default, OptionSection Section)
{
    public GeneralOption? Under { get; init; }
}

// The General tab's checkboxes by section, then the Experimental tab's.
public static class GeneralOptions
{
    public static IReadOnlyList<OptionRow> Rows { get; } =
    [
        new(
            GeneralOption.EnterToTalk,
            "Press Enter to activate chat mode before typing. Press Enter again to send, or ESC to cancel.",
            false,
            OptionSection.Chat),
        new(
            GeneralOption.ExpandedQuickStats,
            "Show Hit/Parry Score with Weapon/Armor value.",
            true,
            OptionSection.Informational),
        new(
            GeneralOption.SkillPlanner,
            "Enable the skill planner: the Planner button below the skill list and its hotkey.",
            false,
            OptionSection.Informational),
        new(
            GeneralOption.ItemTooltips,
            "Show item tooltips for inventory and equipped gear.",
            true,
            OptionSection.Informational),
        new(
            GeneralOption.ItemTooltipsAlways,
            "Always show tooltips on hover. When off, hold the Item Tooltip hotkey to show them.",
            true,
            OptionSection.Informational) { Under = GeneralOption.ItemTooltips },
        new(
            GeneralOption.AdaptivePacing,
            "Adaptive network smoothing: lower delay on steady connections, more buffering when the connection is uneven.",
            false,
            OptionSection.Networking),
        new(
            GeneralOption.GlideMovement,
            "Interpolate character/map movement for smoothness",
            false,
            OptionSection.Networking),
        new(
            GeneralOption.PickTileDrawnUnderMouse,
            "When walking, click the tile drawn under the mouse rather than where the map was.",
            false,
            OptionSection.Networking),
        new(
            GeneralOption.EscapeClosesWindowsFirst,
            "Escape closes windows before canceling movement.",
            true,
            OptionSection.Miscellaneous),
        new(
            GeneralOption.GiveMore,
            "Holding Ctrl+Shift when giving an item will attempt to reload that item.",
            false,
            OptionSection.Miscellaneous),
        new(
            GeneralOption.UseQueue,
            "Queued Actions: holding Ctrl+Shift when using or taking an item queues it. Limit 5.",
            false,
            OptionSection.Miscellaneous),
        new(
            GeneralOption.StubbornUse,
            $"Keep retrying a use that doesn't go through, up to {StubbornUse.MostFailures} times (Stubborn use).",
            false,
            OptionSection.Experimental),
        new(
            GeneralOption.DivinerCard,
            "Summoner with Diviner: hold your first tarot card while casting Ghost Companion or Shadow Copy, then put it back.",
            false,
            OptionSection.ClassHelpers),
    ];

    public static bool IsShown(OptionRow row, Func<GeneralOption, bool> isOn) =>
        row.Under is not { } parent || isOn(parent);

    public static bool ShowsItemTooltips(bool itemTooltips, bool always, bool keyHeld) =>
        itemTooltips && (always || keyHeld);

    public static bool EscapeSendsReset(bool escapeClosesWindowsFirst, bool closedAWindow) =>
        !escapeClosesWindowsFirst || !closedAWindow;
}
