using System;
using System.Globalization;
using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.World;

// The current client's gamma ran from 1.00 to 1.20 in steps of 0.05; the sliders go there in hundredths.
public static class Gamma
{
    public const float Normal = 1f;
    public const float Lowest = 1f;
    public const float Highest = 1.2f;
    public const float Step = 0.01f;

    private const float Hundredths = 100f;
    private const int HotkeyStepHundredths = 5;

    public static float Clamp(float gamma) => float.IsNaN(gamma) ? Normal : Math.Clamp(gamma, Lowest, Highest);

    public static string Text(float gamma) => gamma.ToString("0.00", CultureInfo.InvariantCulture);

    public static float NextStep(float gamma)
    {
        var hundredths = (int)MathF.Round(gamma * Hundredths);
        var next = (hundredths / HotkeyStepHundredths + 1) * HotkeyStepHundredths;
        return next > Highest * Hundredths ? Lowest : next / Hundredths;
    }

    public static LogLine WorldGammaLine(float gamma) =>
        new($"Set world gamma to {Text(gamma)}", (int)TextColour.Green);
}
