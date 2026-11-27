using System;
using System.Diagnostics;

namespace TheLastGate.Updates;

public sealed record UpdateStatusLook(string Message, string? ButtonText, string Tooltip, UpdateButtonAction Action)
{
    public static UpdateStatusLook For(
        UpdateCheckState state,
        bool ctrlHeld,
        bool hasRollbackChoices,
        Version? newest,
        int? downloadPercent)
    {
        if (downloadPercent is { } percent)
        {
            return MessageOnly($"Downloading... {percent}%");
        }

        var look = ForState(state, newest);
        if (!ctrlHeld)
        {
            return look;
        }

        return hasRollbackChoices
            ? look with
            {
                ButtonText = "Rollback",
                Tooltip = "Go back to an earlier version.",
                Action = UpdateButtonAction.ShowRollback,
            }
            : MessageOnly(look.Message);
    }

    private static UpdateStatusLook ForState(UpdateCheckState state, Version? newest) => state switch
    {
        UpdateCheckState.Checking => MessageOnly("Checking for updates..."),
        UpdateCheckState.NotInstalled => MessageOnly("Updates only work in a copy installed with Setup."),
        UpdateCheckState.Failed => MessageOnly("Couldn't check for updates. Reopen the launcher to try again."),
        UpdateCheckState.UpToDate => MessageOnly("Up to date"),
        UpdateCheckState.UpdateAvailable => new UpdateStatusLook(
            $"Version {newest} is ready.",
            "Update",
            "See what's new, then update.",
            UpdateButtonAction.ShowUpdate),
        _ => throw new UnreachableException(),
    };

    private static UpdateStatusLook MessageOnly(string message) => new(message, null, "", UpdateButtonAction.None);
}
