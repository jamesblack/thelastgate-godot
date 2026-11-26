using System;

namespace TheLastGate.Assets;

public readonly record struct LineSpacing(int Top, int Bottom);

public static class LineFit
{
    public const int ClassicLineHeight = 9;

    public static LineSpacing Fit(float fontHeight, int lineHeight, int offset)
    {
        var extra = (int)Math.Ceiling(fontHeight) - lineHeight;
        var trimmedAbove = (int)Math.Floor(extra / 2.0);
        return new LineSpacing(offset - trimmedAbove, -offset - (extra - trimmedAbove));
    }
}
