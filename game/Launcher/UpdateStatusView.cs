using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public partial class UpdateStatusView : HBoxContainer
{
    private ProceduralButton button = null!;
    private ClickArea buttonClick = null!;
    private UpdateCatalog? catalog;
    private bool ctrlHeld;
    private Version? current;
    private int? downloadPercent;
    private UpdateStatusLook look = new("", null, "", UpdateButtonAction.None);
    private HudLabel message = null!;
    private RollbackWindowView? rollbackWindow;
    private UpdateCheckState state = UpdateCheckState.Checking;
    private UpdateWindowView? updateWindow;
    private IUpdater? updater;

    public event Action? Restarting;

    public event Action<IReadOnlyList<AvailableRelease>>? NotesFound;

    public override void _Ready()
    {
        message = GetNode<HudLabel>("Message");
        button = GetNode<ProceduralButton>("Button");
        buttonClick = button.GetNode<ClickArea>("Click");
        buttonClick.StyledTooltip = true;
        button.Pressed += OnPressed;
        ShowLook();
    }

    public override void _Process(double delta)
    {
        var held = Input.IsKeyPressed(Key.Ctrl);
        if (held != ctrlHeld)
        {
            ctrlHeld = held;
            ShowLook();
        }
    }

    public async void Start(IUpdater chosenUpdater, UpdateWindowView changelogWindow, RollbackWindowView earlierWindow)
    {
        updater = chosenUpdater;
        updateWindow = changelogWindow;
        rollbackWindow = earlierWindow;
        current = chosenUpdater.CurrentVersion;
        if (!chosenUpdater.IsInstalled || current is null)
        {
            ShowState(UpdateCheckState.NotInstalled);
            return;
        }

        ShowState(UpdateCheckState.Checking);
        try
        {
            var releases = await Task.Run(() => chosenUpdater.ListReleases(CancellationToken.None));
            catalog = new UpdateCatalog(current, releases);
            NotesFound?.Invoke(catalog.NotesToShow);
            ShowState(catalog.Newest is null ? UpdateCheckState.UpToDate : UpdateCheckState.UpdateAvailable);
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Update check failed: {exception.Message}");
            ShowState(UpdateCheckState.Failed);
        }
    }

    private void ShowState(UpdateCheckState newState)
    {
        state = newState;
        ShowLook();
    }

    private void ShowDownload(int? percent)
    {
        downloadPercent = percent;
        ShowLook();
    }

    private void ShowLook()
    {
        look = UpdateStatusLook.For(
            state,
            ctrlHeld,
            catalog?.RollbackChoices.Count > 0,
            catalog?.Newest?.Version,
            downloadPercent);
        if (message.Text != look.Message)
        {
            message.Text = look.Message;
        }

        button.Visible = look.ButtonText != null;
        if (look.ButtonText is { } caption && button.Caption != caption)
        {
            button.Caption = caption;
        }

        buttonClick.TooltipText = look.Tooltip;
    }

    private void OnPressed()
    {
        switch (look.Action)
        {
            case UpdateButtonAction.ShowUpdate:
                OpenUpdateWindow();
                break;
            case UpdateButtonAction.ShowRollback:
                OpenRollbackWindow();
                break;
            case UpdateButtonAction.None:
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void OpenUpdateWindow()
    {
        if (updater is not { } chosenUpdater || updateWindow is null || catalog?.Newest is not { } newest)
        {
            return;
        }

        updateWindow.Open(
            catalog.NewerThanCurrent,
            (progress, cancel) => Download(chosenUpdater, newest.Version, progress, cancel),
            () => Restart(newest.Version));
    }

    private void OpenRollbackWindow()
    {
        if (updater is not { } chosenUpdater || rollbackWindow is null || catalog is null || current is null)
        {
            return;
        }

        rollbackWindow.Open(
            current,
            catalog,
            (target, progress, cancel) => Download(chosenUpdater, target, progress, cancel),
            Restart);
    }

    // The updater reports progress from its own thread, so the status line follows it on the main thread.
    private async Task Download(
        IUpdater chosenUpdater,
        Version target,
        Action<int> progress,
        CancellationToken cancel)
    {
        try
        {
            await Task.Run(
                () => chosenUpdater.Download(
                    target,
                    percent =>
                    {
                        progress(percent);
                        Callable.From(() => ShowDownload(percent)).CallDeferred();
                    },
                    cancel),
                cancel);
        }
        finally
        {
            Callable.From(() => ShowDownload(null)).CallDeferred();
        }
    }

    private void Restart(Version target)
    {
        updater?.ApplyAndRestart(target);
        Restarting?.Invoke();
    }
}
