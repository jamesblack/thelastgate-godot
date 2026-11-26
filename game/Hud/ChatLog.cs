using System;
using Godot;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Hud;

public partial class ChatLog : Control
{
    private const int VisibleLines = 22;

    private readonly LogScroll scroll = new();
    private IFontSet fonts = null!;
    private bool hasLines;
    private TextLog? log;
    private RichTextLabel text = null!;

    public override void _Ready() => text = GetNode<RichTextLabel>("Text");

    public override void _ExitTree()
    {
        if (log != null)
        {
            log.LineAdded -= Add;
        }
    }

    public override void _Process(double delta)
    {
        var linesBack = scroll.LinesBack;
        scroll.Advance(TimeSpan.FromSeconds(delta));
        if (scroll.LinesBack != linesBack)
        {
            ScrollToShown();
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true } button)
        {
            return;
        }

        var lines = button.ButtonIndex switch
        {
            MouseButton.WheelUp => LogScroll.WheelLines,
            MouseButton.WheelDown => -LogScroll.WheelLines,
            _ => 0,
        };
        if (lines != 0)
        {
            AcceptEvent();
            ScrollBy(lines);
        }
    }

    public void ScrollPages(int pages) => ScrollBy(pages * LogScroll.PageLines);

    public void Show(TextLog shownLog, IFontSet fontSet)
    {
        log = shownLog;
        ShowAllLines(fontSet);
        log.LineAdded += Add;
    }

    public void ShowFonts(IFontSet fontSet)
    {
        if (log != null && fontSet != fonts)
        {
            ShowAllLines(fontSet);
        }
    }

    private void ShowAllLines(IFontSet fontSet)
    {
        fonts = fontSet;
        var defaultFont = fonts.For(TextColour.Yellow);
        text.AddThemeFontOverride("normal_font", defaultFont.Font);
        text.AddThemeFontSizeOverride("normal_font_size", defaultFont.Size);
        text.Clear();
        hasLines = false;
        foreach (var line in log!.Lines)
        {
            Append(line);
        }

        scroll.LinesChanged(0, OldestLinesBack());
        ScrollToShown();
    }

    private void Add(LogLine line)
    {
        var linesBefore = text.GetLineCount();
        Append(line);
        var linesAdded = text.GetLineCount() - linesBefore;
        if (text.GetParagraphCount() > TextLog.MaxLines)
        {
            text.RemoveParagraph(0);
        }

        scroll.LinesChanged(linesAdded, OldestLinesBack());
        ScrollToShown();
    }

    private void Append(LogLine line)
    {
        if (hasLines)
        {
            text.Newline();
        }

        var font = fonts.For((TextColour)line.Font);
        text.PushFont(font.Font, font.Size);
        text.PushColor(font.Tint);
        text.AddText(line.Text);
        text.Pop();
        text.Pop();
        hasLines = true;
    }

    private void ScrollBy(int lines)
    {
        scroll.Scroll(lines);
        ScrollToShown();
    }

    private int OldestLinesBack() => Math.Max(0, text.GetLineCount() - VisibleLines);

    private void ScrollToShown() => text.ScrollToLine(Math.Max(0, OldestLinesBack() - scroll.LinesBack));
}
