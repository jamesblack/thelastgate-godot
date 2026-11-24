using Godot;
using TheLastGate.Assets;
using TheLastGate.State;
using TheLastGate.Windows;

namespace TheLastGate.Hud.TextWindow;

// The welcome shows over a tutorial over the message of the day: the order TextWindows' buttons act on them.
// The scene is saved visible so it can be laid out in the editor; the game hides it until a window opens.
[Tool]
public partial class TextWindowView : Control
{
    private const string CloseButtonName = "CloseButton";
    private const string LetsGo = "Let's Go!";

    private static readonly string[] PreviewLines =
    [
        "Welcome to The Last Gate!", "", "The message of the day is shown here,",
        "one line per row, in its own colour.",
    ];

    public ClickArea Button(string name) =>
        name == CloseButtonName ? GetNode<ClickArea>($"%{name}") : GetNode<ClickArea>($"%{name}/Click");

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            PreviewInEditor();
            return;
        }

        ShowMessageLayout(false);
        Visible = false;
    }

    // The preview's text and layout are set on this scene's own nodes, so they are reset around editor saves.
    public override void _Notification(int what)
    {
        if (what == NotificationEditorPreSave)
        {
            ShowMessageLayout(false);
            ClearMessageLines();
        }
        else if (what == NotificationEditorPostSave)
        {
            PreviewInEditor();
        }
    }

    public void Show(TextWindows windows, MessageOfTheDayLines messageLines)
    {
        Visible = windows.AnyOpen;
        if (!Visible)
        {
            return;
        }

        if (!windows.WelcomeOpen && windows.Tutorial is { } tutorial)
        {
            ShowTutorial(tutorial, windows.TutorialPage);
            return;
        }

        ShowMessageLayout(windows.WelcomeOpen);
        ShowMessageLines(messageLines);
    }

    private void ShowMessageLayout(bool welcome)
    {
        GetNode<CanvasItem>("%Message").Visible = true;
        GetNode<CanvasItem>("%Tutorial").Visible = false;
        GetNode<CanvasItem>("%PictureArea").Visible = false;
        if (welcome)
        {
            ShowButtons("Tutorial", null, "I'm Good");
        }
        else
        {
            ShowButtons(null, LetsGo, null);
        }
    }

    private void ShowMessageLines(MessageOfTheDayLines messageLines)
    {
        var message = GetNode<Node>("%Message");
        for (var index = 0; index < MessageOfTheDayLines.VisibleLines; index++)
        {
            var label = message.GetChild<HudLabel>(index);
            var line = messageLines.Lines[index];
            label.Text = line.Text;
            label.Colour = (TextColour)line.Font;
        }
    }

    private void ClearMessageLines()
    {
        var message = GetNode<Node>("%Message");
        for (var index = 0; index < MessageOfTheDayLines.VisibleLines; index++)
        {
            var label = message.GetChild<HudLabel>(index);
            label.Text = "";
            label.Colour = TextColour.Yellow;
        }
    }

    private void ShowTutorial(int tutorial, int page)
    {
        GetNode<CanvasItem>("%Message").Visible = false;
        GetNode<CanvasItem>("%Tutorial").Visible = true;
        GetNode<CanvasItem>("%PictureArea").Visible = true;
        GetNode<ArchiveSprite>("%PictureArea/Picture").SpriteNumber = TutorialPages.Picture(tutorial, page);
        var text = TutorialPages.Text(tutorial, page);
        var lines = GetNode<Node>("%Tutorial");
        for (var index = 0; index < TutorialPages.LinesPerPage; index++)
        {
            lines.GetChild<Label>(index).Text = text[index];
        }

        ShowButtons("Prev", null, "Next");
    }

    // The buttons' labels belong to the text button scene, so they are set here rather than saved in this one.
    private void ShowButtons(string? left, string? ok, string? right)
    {
        ShowButton("%LeftButton", left);
        ShowButton("%OkButton", ok);
        ShowButton("%RightButton", right);
    }

    private void ShowButton(string path, string? caption)
    {
        var button = GetNode<CanvasItem>(path);
        button.Visible = caption != null;
        button.GetNode<Label>("Label").Text = caption ?? "";
    }

    private void PreviewInEditor()
    {
        var preview = new MessageOfTheDayLines();
        foreach (var text in PreviewLines)
        {
            preview.Add(new LogLine(text, (int)TextColour.Yellow));
        }

        ShowMessageLayout(false);
        ShowMessageLines(preview);
    }
}
