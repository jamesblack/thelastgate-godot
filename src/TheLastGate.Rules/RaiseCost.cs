using System;
using TheLastGate.State;

namespace TheLastGate.Rules;

public static class RaiseCost
{
    private const int BaseValue = 0;
    private const int Limit = 2;
    private const int Difficulty = 3;

    public static int? At(int slot, int[] values, int planned)
    {
        var value = values[BaseValue] + planned;
        if (values[BaseValue] <= 0 || value >= values[Limit])
        {
            return null;
        }

        var difficulty = values[Difficulty];
        return slot switch
        {
            < PlayerState.AttributeCount => value * value * value * difficulty / 20,
            StatSlots.HitPoints or StatSlots.Mana => value * difficulty,
            _ => Math.Max(value, value * value * value * difficulty / 40),
        };
    }
}
