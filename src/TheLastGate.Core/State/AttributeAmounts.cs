using System;
using System.Linq;

namespace TheLastGate.State;

// The server's order for the five attributes.
public enum AttributeSlot
{
    Braveness,
    Willpower,
    Intuition,
    Agility,
    Strength,
}

// An amount for each attribute, equal to another with the same amounts.
public sealed class AttributeAmounts : IEquatable<AttributeAmounts>
{
    public const int Count = 5;

    private readonly int[] amounts = new int[Count];

    public int this[AttributeSlot attribute]
    {
        get => amounts[(int)attribute];
        set => amounts[(int)attribute] = value;
    }

    public bool Equals(AttributeAmounts? other) => other != null && amounts.SequenceEqual(other.amounts);

    // In the server's order: Braveness, Willpower, Intuition, Agility, Strength.
    public static AttributeAmounts Of(int braveness, int willpower, int intuition, int agility, int strength)
    {
        var made = new AttributeAmounts();
        int[] values = [braveness, willpower, intuition, agility, strength];
        values.CopyTo(made.amounts, 0);
        return made;
    }

    public void SetAll(int amount) => Array.Fill(amounts, amount);

    public AttributeAmounts Copy()
    {
        var copy = new AttributeAmounts();
        amounts.CopyTo(copy.amounts, 0);
        return copy;
    }

    public override string ToString() => string.Join(", ", amounts);

    public override bool Equals(object? other) => Equals(other as AttributeAmounts);

    public override int GetHashCode() =>
        HashCode.Combine(amounts[0], amounts[1], amounts[2], amounts[3], amounts[4]);
}
