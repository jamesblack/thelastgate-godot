using GdUnit4;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
public class GeneralOptionsTests
{
    [TestCase]
    public void Rows_AreGroupedChatInformationalNetworkingMiscellaneousExperimentalThenClassHelpers()
    {
        AssertThat(GeneralOptions.Rows.Select(row => (row.Option, row.Section)))
            .ContainsExactly(
                (GeneralOption.EnterToTalk, OptionSection.Chat),
                (GeneralOption.ExpandedQuickStats, OptionSection.Informational),
                (GeneralOption.SkillPlanner, OptionSection.Informational),
                (GeneralOption.ItemTooltips, OptionSection.Informational),
                (GeneralOption.ItemTooltipsAlways, OptionSection.Informational),
                (GeneralOption.AdaptivePacing, OptionSection.Networking),
                (GeneralOption.GlideMovement, OptionSection.Networking),
                (GeneralOption.PickTileDrawnUnderMouse, OptionSection.Networking),
                (GeneralOption.EscapeClosesWindowsFirst, OptionSection.Miscellaneous),
                (GeneralOption.GiveMore, OptionSection.Miscellaneous),
                (GeneralOption.UseQueue, OptionSection.Miscellaneous),
                (GeneralOption.DepotCharacterList, OptionSection.Miscellaneous),
                (GeneralOption.StubbornUse, OptionSection.Experimental),
                (GeneralOption.DivinerCard, OptionSection.ClassHelpers));
    }

    [TestCase(GeneralOption.EscapeClosesWindowsFirst, "Escape closes windows before canceling movement.", true)]
    [TestCase(
        GeneralOption.GiveMore,
        "Holding Ctrl+Shift when giving an item will attempt to reload that item.",
        false)]
    [TestCase(
        GeneralOption.UseQueue,
        "Queued Actions: holding Ctrl+Shift when using or taking an item queues it. Limit 5.",
        false)]
    [TestCase(
        GeneralOption.EnterToTalk,
        "Press Enter to activate chat mode before typing. Press Enter again to send, or ESC to cancel.",
        false)]
    [TestCase(GeneralOption.ExpandedQuickStats, "Show Hit/Parry Score with Weapon/Armor value.", true)]
    [TestCase(
        GeneralOption.AdaptivePacing,
        "Adaptive network smoothing: lower delay on steady connections, more buffering when the connection is uneven.",
        false)]
    [TestCase(GeneralOption.ItemTooltips, "Show item tooltips for inventory and equipped gear.", true)]
    [TestCase(
        GeneralOption.ItemTooltipsAlways,
        "Always show tooltips on hover. When off, hold the Item Tooltip hotkey to show them.",
        true)]
    public void Rows_UseTheCurrentClientsWordsAndDefaults(GeneralOption option, string label, bool isOn)
    {
        var row = GeneralOptions.Rows.Single(row => row.Option == option);

        AssertThat(row.Label).IsEqual(label);
        AssertThat(row.Default).IsEqual(isOn);
    }

    [TestCase(GeneralOption.GlideMovement)]
    [TestCase(GeneralOption.PickTileDrawnUnderMouse)]
    [TestCase(GeneralOption.StubbornUse)]
    [TestCase(GeneralOption.DivinerCard)]
    [TestCase(GeneralOption.DepotCharacterList)]
    public void Rows_LeaveTheGodotOptionsOffToBehaveLikeTheCurrentClient(GeneralOption option)
    {
        var row = GeneralOptions.Rows.Single(row => row.Option == option);

        AssertThat(row.Default).IsFalse();
        AssertThat(row.Label).IsNotEmpty();
    }

    [TestCase]
    public void Rows_SayHowLongAStubbornUseKeepsTrying()
    {
        AssertThat(GeneralOptions.Rows.Single(row => row.Option == GeneralOption.StubbornUse).Label)
            .IsEqual("Keep retrying a use that doesn't go through, up to 8 times (Stubborn use).");
    }

    [TestCase]
    public void Rows_SayWhatTheDivinerCardHelperDoes()
    {
        AssertThat(GeneralOptions.Rows.Single(row => row.Option == GeneralOption.DivinerCard).Label)
            .IsEqual(
                "Summoner with Diviner: hold your first tarot card while casting Ghost Companion or Shadow Copy, then put it back.");
    }

    [TestCase]
    public void Rows_SayWhatTheDepotCharacterListDoes()
    {
        AssertThat(GeneralOptions.Rows.Single(row => row.Option == GeneralOption.DepotCharacterList).Label)
            .IsEqual("Show your characters and their depot pages beside the depot window.");
    }

    [TestCase]
    public void Rows_PutAlwaysShowingTooltipsUnderTheTooltipOption()
    {
        AssertThat(GeneralOptions.Rows.Where(row => row.Under != null).Select(row => row.Option))
            .ContainsExactly(GeneralOption.ItemTooltipsAlways);
        AssertThat(GeneralOptions.Rows.Single(row => row.Option == GeneralOption.ItemTooltipsAlways).Under)
            .IsEqual(GeneralOption.ItemTooltips);
    }

    [TestCase(GeneralOption.ItemTooltips, false, true)]
    [TestCase(GeneralOption.ItemTooltipsAlways, true, true)]
    [TestCase(GeneralOption.ItemTooltipsAlways, false, false)]
    public void IsShown_HidesARowWhileTheOptionItIsUnderIsOff(GeneralOption option, bool tooltipsOn, bool shown)
    {
        var row = GeneralOptions.Rows.Single(row => row.Option == option);

        AssertThat(GeneralOptions.IsShown(row, other => other == GeneralOption.ItemTooltips && tooltipsOn))
            .IsEqual(shown);
    }

    [TestCase(true, true, false, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, true, false)]
    [TestCase(false, false, true, false)]
    public void ShowsItemTooltips_AlwaysOrWhileTheKeyIsHeld(bool tooltips, bool always, bool keyHeld, bool shows)
    {
        AssertThat(GeneralOptions.ShowsItemTooltips(tooltips, always, keyHeld)).IsEqual(shows);
    }

    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, true)]
    public void EscapeSendsReset_UnlessItClosedAWindowFirst(bool closesWindowsFirst, bool closedAWindow, bool resets)
    {
        AssertThat(GeneralOptions.EscapeSendsReset(closesWindowsFirst, closedAWindow)).IsEqual(resets);
    }

    [TestCase]
    public void TheSkillPlannerOption_IsInformationalAndStartsOff()
    {
        var rows = GeneralOptions.Rows.Select(row => row.Option).ToList();
        var row = GeneralOptions.Rows.Single(row => row.Option == GeneralOption.SkillPlanner);

        AssertThat(row.Label)
            .IsEqual("Enable the skill planner: the Planner button below the skill list and its hotkey.");
        AssertThat(row.Default).IsFalse();
        AssertThat(row.Section).IsEqual(OptionSection.Informational);
        AssertThat(rows.IndexOf(GeneralOption.SkillPlanner))
            .IsEqual(rows.IndexOf(GeneralOption.ExpandedQuickStats) + 1);
    }
}
