using System;
using Godot;
using TheLastGate.Procedural;

namespace TheLastGate.Launcher;

public partial class LauncherPrompt : ModalBackdrop
{
    private Action? onYes;

    private Label Title => GetNode<Label>("%Title");

    private Label Question => GetNode<Label>("%Question");

    private Label Explanation => GetNode<Label>("%Explanation");

    private Label Warning => GetNode<Label>("%Warning");

    private ProceduralButton Ok => GetNode<ProceduralButton>("%Ok");

    private ProceduralButton Yes => GetNode<ProceduralButton>("%Yes");

    private ProceduralButton No => GetNode<ProceduralButton>("%No");

    public bool CanClose
    {
        get => !Ok.Disabled;
        set => Ok.Disabled = !value;
    }

    public override void _Ready()
    {
        Ok.Pressed += Close;
        Yes.Pressed += Agree;
        No.Pressed += Close;
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (Visible && inputEvent.IsActionPressed("ui_cancel") && (onYes != null || CanClose))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Tell(string title, string text)
    {
        onYes = null;
        Show(title, text, "", "");
        CanClose = true;
    }

    public void ShowText(string text) => Question.Text = text;

    public void Ask(string title, string question, string explanation, string warning, Action agreed)
    {
        onYes = agreed;
        Show(title, question, explanation, warning);
    }

    private void Show(string title, string question, string explanation, string warning)
    {
        Title.Text = title;
        Question.Text = question;
        Explanation.Text = explanation;
        Explanation.Visible = explanation.Length > 0;
        Warning.Text = warning;
        Warning.Visible = warning.Length > 0;
        var asking = onYes != null;
        Ok.Visible = !asking;
        Yes.Visible = asking;
        No.Visible = asking;
        GetViewport().GuiReleaseFocus();
        Visible = true;
    }

    private void Agree()
    {
        var agreed = onYes;
        Close();
        agreed?.Invoke();
    }

    private void Close()
    {
        onYes = null;
        Visible = false;
    }
}
