using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public partial class UpdateWindowView : ModalBackdrop
{
    private Func<Action<int>, CancellationToken, Task>? download;
    private CancellationTokenSource? downloadCancel;
    private Action? restart;

    private ProceduralButton UpdateButton => GetNode<ProceduralButton>("%ActionButton");

    private Label Progress => GetNode<Label>("%Progress");

    private Label Error => GetNode<Label>("%Error");

    public ILauncherFontSet Fonts { get; set; } = new PixelOperatorFonts();

    public override void _Ready()
    {
        UpdateButton.Pressed += OnUpdatePressed;
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
        IReadOnlyList<AvailableRelease> newer,
        Func<Action<int>, CancellationToken, Task> download,
        Action restart)
    {
        this.download = download;
        this.restart = restart;
        ChangelogText.Fill(GetNode<RichTextLabel>("%Changelog"), newer, Fonts);
        Progress.Text = "";
        Error.Visible = false;
        UpdateButton.Disabled = false;
        GetViewport().GuiReleaseFocus();
        Visible = true;
    }

    private void Close()
    {
        downloadCancel?.Cancel();
        Visible = false;
    }

    private async void OnUpdatePressed()
    {
        if (download is null || restart is null || downloadCancel != null)
        {
            return;
        }

        UpdateButton.Disabled = true;
        Error.Visible = false;
        ShowProgress(0);
        using var cancel = new CancellationTokenSource();
        downloadCancel = cancel;
        try
        {
            await download(percent => Callable.From(() => ShowProgress(percent)).CallDeferred(), cancel.Token);
            Visible = false;
            restart();
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            Progress.Text = "";
        }
        catch (Exception exception)
        {
            GD.PushWarning($"Update download failed: {exception.Message}");
            Progress.Text = "";
            Error.Visible = true;
        }
        finally
        {
            downloadCancel = null;
            UpdateButton.Disabled = false;
        }
    }

    private void ShowProgress(int percent) => Progress.Text = $"Downloading... {percent}%";
}
