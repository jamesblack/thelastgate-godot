using System;
using Godot;
using Godot.Collections;
using TheLastGate.Assets;
using TheLastGate.Chat;
using TheLastGate.Net;
using TheLastGate.Profiling;

namespace TheLastGate.Hud;

// Godot's LineEdit does the typing and editing; its own text is hidden and drawn here with the current client's
// scrolling and cursor glyph.
[Tool]
public partial class ChatLine : LineEdit, IHudContextUser
{
    private const string CursorGlyph = "\u007f";

    private readonly TabCompletion completion = new();
    private readonly ChatHistory history = new();
    private readonly ChatMode mode = new();
    private readonly ChatLineView view = new();
    private GameFont? font;

    public ChatLine()
    {
        AddToGroup(HudContext.Group);
        MaxLength = ChatPackets.MaxLength;
    }

    [Export] public string PreviewText { get; set; } = "";

    public bool EnterToTalk
    {
        get => mode.EnterToTalk;
        set
        {
            if (mode.EnterToTalk != value)
            {
                mode.EnterToTalk = value;
                FollowMode();
            }
        }
    }

    public bool TakesTyping => mode.TakesTyping;

    public bool RunsHotkeys => mode.RunsHotkeys;

    public void ShowContext(HudContext context)
    {
        if (context.Fonts is not { } fonts)
        {
            return;
        }

        font = fonts.For(TextColour.Yellow);
        AddThemeFontOverride("font", font.Value.Font);
        AddThemeFontSizeOverride("font_size", font.Value.Size);
        AddThemeColorOverride("font_color", Colors.Transparent);
    }

    public event Action<string>? LineSent;

    public event Action? EscapePressed;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            return;
        }

        TextSubmitted += Send;
        GetViewport().GuiFocusChanged += TakeFocusFrom;
    }

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
        {
            GetViewport().GuiFocusChanged -= TakeFocusFrom;
        }
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsString();
        if (name is "theme_override_fonts/font" or "theme_override_font_sizes/font_size"
            or "theme_override_colors/font_color")
        {
            property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (!mode.TakesTyping || inputEvent is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.Up:
                ShowLine(history.Older(Text));
                break;
            case Key.Down:
                ShowLine(history.Newer(Text));
                break;
            case Key.Tab:
                if (completion.Complete(Text, CaretColumn) is { } completed)
                {
                    Text = completed.Text;
                    CaretColumn = completed.Caret;
                }

                break;
            case Key.Escape:
                if (mode.Press(ChatKey.Escape) == ChatAction.Cancel)
                {
                    Clear();
                    history.Abandon();
                    FollowMode();
                }
                else
                {
                    EscapePressed?.Invoke();
                }

                break;
            default:
                return;
        }

        AcceptEvent();
    }

    // Asked only while the line is closed, when it can't be typed in and its keys reach the HUD instead.
    public bool Open(ChatKey key)
    {
        switch (mode.Press(key))
        {
            case ChatAction.Open:
                break;
            case ChatAction.OpenWithSlash:
                InsertTextAtCaret("/");
                break;
            case ChatAction.OpenWithOlder:
                ShowLine(history.Older(Text));
                break;
            default:
                return false;
        }

        FollowMode();
        return true;
    }

    public override void _Draw()
    {
        using var timing = Timings.Measure("chat.draw");
        if (font is not { } shownFont)
        {
            return;
        }

        var editorPreview = Engine.IsEditorHint();
        var text = editorPreview ? PreviewText : Text;
        var (shown, cursorColumn) = view.Show(text, editorPreview ? text.Length : CaretColumn);
        var top = new Vector2(0, shownFont.Font.GetAscent(shownFont.Size));
        var advance = shownFont.Font.GetCharSize('M', shownFont.Size).X;
        DrawText(shown, top, shownFont);
        if (editorPreview || mode.TakesTyping)
        {
            DrawText(CursorGlyph, top + new Vector2(cursorColumn * advance, 0), shownFont);
        }
    }

    private void DrawText(string text, Vector2 position, GameFont shownFont) =>
        DrawString(shownFont.Font, position, text, HorizontalAlignment.Left, -1, shownFont.Size, shownFont.Tint);

    private void ShowLine(string? line)
    {
        if (line != null)
        {
            Text = line;
            CaretColumn = line.Length;
        }
    }

    // With enter to talk, Enter closes the line even when there is nothing to send.
    private void Send(string line)
    {
        var closes = mode.Press(ChatKey.Enter) == ChatAction.Send;
        if (line.Length > 0)
        {
            history.Sent(line);
            completion.Learn(line);
            Clear();
            LineSent?.Invoke(line);
        }

        if (closes)
        {
            FollowMode();
        }
    }

    // The line keeps focus while it's closed; it only can't be typed in, so its keys go on to the hotkeys.
    private void FollowMode() => Editable = mode.TakesTyping;

    // Only a window's text field may take focus from the line, and the line takes it back when the field lets go. A
    // line field stops editing on Escape or Enter but would keep focus, so it lets go then.
    private void TakeFocusFrom(Control owner)
    {
        if (owner == this)
        {
            return;
        }

        if (owner is LineEdit field)
        {
            LetGoWhenEditingEnds(field);
        }

        if (owner is LineEdit or TextEdit)
        {
            owner.Connect(
                Control.SignalName.FocusExited,
                Callable.From(TakeFocusIfNobodyHasIt),
                (uint)ConnectFlags.OneShot | (uint)ConnectFlags.Deferred);
            return;
        }

        CallDeferred(Control.MethodName.GrabFocus);
    }

    private static void LetGoWhenEditingEnds(LineEdit field)
    {
        field.EditingToggled += LetGo;
        field.FocusExited += StopWatching;
        return;

        void LetGo(bool editing)
        {
            if (!editing)
            {
                field.ReleaseFocus();
            }
        }

        void StopWatching()
        {
            field.EditingToggled -= LetGo;
            field.FocusExited -= StopWatching;
        }
    }

    private void TakeFocusIfNobodyHasIt()
    {
        if (IsInsideTree() && GetViewport().GuiGetFocusOwner() == null)
        {
            GrabFocus();
        }
    }
}
