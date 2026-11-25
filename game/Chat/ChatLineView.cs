using System;

namespace TheLastGate.Chat;

public sealed class ChatLineView
{
    public const int VisibleLength = 48;

    private const int CursorLeadRight = 45;
    private const int CursorLeadLeft = 5;

    private int viewStart;

    public (string Text, int CursorColumn) Show(string text, int caret)
    {
        if (caret - viewStart > CursorLeadRight)
        {
            viewStart = caret - CursorLeadRight;
        }

        if (caret - CursorLeadLeft < viewStart)
        {
            viewStart = Math.Max(0, caret - CursorLeadLeft);
        }

        var shown = viewStart < text.Length
            ? text.Substring(viewStart, Math.Min(VisibleLength, text.Length - viewStart))
            : "";
        return (shown, caret - viewStart);
    }
}
