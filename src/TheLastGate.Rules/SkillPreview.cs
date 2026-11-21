using System;

namespace TheLastGate.Rules;

// A skill gains (the sum of its three attributes) / divisor from them, truncated, so a planned attribute raise only
// shows on the skill once the sum crosses a multiple of the divisor.
public static class SkillPreview
{
    public const int NormalDivisor = 5;
    public const int HardModeDivisor = 6;

    public static int AttributeDivisor(bool hardMode) => hardMode ? HardModeDivisor : NormalDivisor;

    // Each share is truncated on its own, as the game rounds the skill value.
    public static int AttributeGain(ReadOnlySpan<int> values, ReadOnlySpan<int> raises, int divisor)
    {
        var currentSum = 0;
        var raisedSum = 0;
        for (var slot = 0; slot < values.Length; slot++)
        {
            currentSum += values[slot];
            raisedSum += values[slot] + raises[slot];
        }

        return raisedSum / divisor - currentSum / divisor;
    }

    public static bool UsesRaisedAttribute(ReadOnlySpan<int> raises)
    {
        foreach (var raise in raises)
        {
            if (raise != 0)
            {
                return true;
            }
        }

        return false;
    }
}
