using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public partial class RollbackWindowView : ModalBackdrop
{
    private const string DownloadFailedText = "The download failed. Check your connection and try again.";
    private const double BytesPerMegabyte = 1_000_000;

    private static readonly TableColumn[] VersionColumns = [new("Version"), new("Released")];

    private UpdateCatalog? catalog;
    private Version? chosen;
    private Version? current;
    private Func<Version, Action<int>, CancellationToken, Task>? download;
    private CancellationTokenSource? downloadCancel;
    private Action<Version>? restart;

    private ProceduralButton RollbackButton => GetNode<ProceduralButton>("%ActionButton");

    private DataTable Versions => GetNode<DataTable>("%Versions");

    private Label Progress => GetNode<Label>("%Progress");

    private Label Error => GetNode<Label>("%Error");

    public ILauncherFontSet Fonts { get; set; } = new PixelOperatorFonts();

    public override void _Ready()
    {
        Versions.SetColumns(VersionColumns);
        Versions.RowClicked += OnVersionClicked;
        RollbackButton.Pressed += OnRollBackPressed;
        GetNode<ProceduralButton>("%FooterClose").Pressed += Close;
        GetNode<ClickArea>("%CloseButton").Released += _ => Close();
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (Visible && inputEvent.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open(
        Version currentVersion,
        UpdateCatalog releases,
        Func<Version, Action<int>, CancellationToken, Task> downloadRelease,
        Action<Version> restartOn)
    {
        current = currentVersion;
        catalog = releases;
        download = downloadRelease;
        restart = restartOn;
        Progress.Text = "";
        Error.Visible = false;
        RollbackButton.Disabled = false;
        ShowVersions();
        Choose(0);
        GetViewport().GuiReleaseFocus();
        Visible = true;
    }

    private void Close()
    {
        downloadCancel?.Cancel();
        Visible = false;
    }

    // The first row is the version already installed, which can't be chosen.
    private void ShowVersions()
    {
        var choices = catalog!.RollbackChoices.Select(release => new TableRow(
        [
            new TableCell(release.Version.ToString()),
            new TableCell(release.Notes.Date is { } date ? $"{date:yyyy-MM-dd}" : "", CellTone.Dim),
        ]));
        Versions.ShowRows(
        [
            new TableRow([new TableCell($"{current}", CellTone.Zero), new TableCell("you have this", CellTone.Zero)]),
            .. choices,
        ]);
    }

    private void OnVersionClicked(int row)
    {
        if (row > 0 && downloadCancel is null)
        {
            Choose(row - 1);
        }
    }

    private void Choose(int choice)
    {
        var release = catalog!.RollbackChoices[choice];
        chosen = release.Version;
        Versions.SelectedRow = choice + 1;
        GetNode<Label>("%Caption").Text = $"Rolling back to {release.Version} undoes these changes:";
        ChangelogText.Fill(GetNode<RichTextLabel>("%Undone"), catalog.UndoneBy(release.Version), Fonts);
        var sizeInMegabytes = Math.Round(release.FullSizeBytes / BytesPerMegabyte);
        GetNode<Label>("%Info").Text = $"Downloads the full game ({sizeInMegabytes} MB). Your settings, characters and "
            + "map notes are kept, and a copy of them is saved in "
            + $"{UserFolderBackup.BackupsFolder(OS.GetUserDataDir())} first. The launcher restarts when it's done.";
        RollbackButton.Caption = $"Roll back to {chosen}";
    }

    private async void OnRollBackPressed()
    {
        if (chosen is not { } target || current is null || download is null || restart is null
            || downloadCancel != null)
        {
            return;
        }

        RollbackButton.Disabled = true;
        Error.Visible = false;
        try
        {
            UserFolderBackup.Create(OS.GetUserDataDir(), current, DateTime.Now);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"Couldn't back up your settings, so nothing was changed: {exception.Message}");
            return;
        }

        ShowProgress(0);
        using var cancel = new CancellationTokenSource();
        downloadCancel = cancel;
        try
        {
            await download(target, percent => Callable.From(() => ShowProgress(percent)).CallDeferred(), cancel.Token);
            Visible = false;
            restart(target);
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            Progress.Text = "";
            RollbackButton.Disabled = false;
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Rollback download failed: {exception.Message}");
            ShowError(DownloadFailedText);
        }
        finally
        {
            downloadCancel = null;
        }
    }

    private void ShowError(string message)
    {
        Progress.Text = "";
        Error.Text = message;
        Error.Visible = true;
        RollbackButton.Disabled = false;
    }

    private void ShowProgress(int percent) => Progress.Text = $"Downloading... {percent}%";
}
