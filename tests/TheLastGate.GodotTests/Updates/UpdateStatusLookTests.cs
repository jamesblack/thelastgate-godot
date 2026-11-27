using GdUnit4;
using TheLastGate.Updates;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Updates;

[TestSuite]
public class UpdateStatusLookTests
{
    private static readonly Version Newest = new(1, 5, 0);

    private static UpdateStatusLook Look(
        UpdateCheckState state,
        bool ctrlHeld = false,
        bool hasRollbackChoices = true,
        int? downloadPercent = null) =>
        UpdateStatusLook.For(
            state,
            ctrlHeld,
            hasRollbackChoices,
            state == UpdateCheckState.UpdateAvailable ? Newest : null,
            downloadPercent);

    [TestCase]
    public void Checking_SaysSoWithoutAButton() =>
        AssertThat(Look(UpdateCheckState.Checking))
            .IsEqual(new UpdateStatusLook("Checking for updates...", null, "", UpdateButtonAction.None));

    [TestCase]
    public void UpToDate_SaysSoWithoutAButton() =>
        AssertThat(Look(UpdateCheckState.UpToDate))
            .IsEqual(new UpdateStatusLook("Up to date", null, "", UpdateButtonAction.None));

    [TestCase]
    public void NotInstalled_SaysUpdatesNeedSetup() =>
        AssertThat(Look(UpdateCheckState.NotInstalled))
            .IsEqual(
                new UpdateStatusLook(
                    "Updates only work in a copy installed with Setup.",
                    null,
                    "",
                    UpdateButtonAction.None));

    [TestCase]
    public void AFailedCheck_SaysSo() =>
        AssertThat(Look(UpdateCheckState.Failed))
            .IsEqual(
                new UpdateStatusLook(
                    "Couldn't check for updates. Reopen the launcher to try again.",
                    null,
                    "",
                    UpdateButtonAction.None));

    [TestCase]
    public void AnAvailableUpdate_OffersTheUpdateButton() =>
        AssertThat(Look(UpdateCheckState.UpdateAvailable))
            .IsEqual(
                new UpdateStatusLook(
                    "Version 1.5.0 is ready.",
                    "Update",
                    "See what's new, then update.",
                    UpdateButtonAction.ShowUpdate));

    [TestCase]
    public void Downloading_ShowsItsProgressWithoutAButton() =>
        AssertThat(Look(UpdateCheckState.UpdateAvailable, downloadPercent: 45))
            .IsEqual(new UpdateStatusLook("Downloading... 45%", null, "", UpdateButtonAction.None));

    [TestCase(UpdateCheckState.Failed)]
    [TestCase(UpdateCheckState.UpToDate)]
    [TestCase(UpdateCheckState.UpdateAvailable)]
    public void CtrlWithRollbackChoices_OffersRollbackBesideTheStatesMessage(UpdateCheckState state) =>
        AssertThat(Look(state, true))
            .IsEqual(
                Look(state) with
                {
                    ButtonText = "Rollback",
                    Tooltip = "Go back to an earlier version.",
                    Action = UpdateButtonAction.ShowRollback,
                });

    [TestCase(UpdateCheckState.UpToDate)]
    [TestCase(UpdateCheckState.UpdateAvailable)]
    public void CtrlWithoutRollbackChoices_ShowsNoButton(UpdateCheckState state) =>
        AssertThat(Look(state, true, false))
            .IsEqual(Look(state) with { ButtonText = null, Tooltip = "", Action = UpdateButtonAction.None });

    [TestCase]
    public void CtrlWhileDownloading_KeepsTheProgress() =>
        AssertThat(Look(UpdateCheckState.UpdateAvailable, true, downloadPercent: 45))
            .IsEqual(Look(UpdateCheckState.UpdateAvailable, downloadPercent: 45));
}
