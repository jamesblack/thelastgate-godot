using System;
using TheLastGate.State;

namespace TheLastGate.Buffs;

// The three buffs the server's buff list is read for.
public enum BuffKind
{
    Bless,
    Enhance,
    Protect,
}

// One buff as the server's buff list describes it: its power, and with spell knowledge the exact bonuses it gives.
public sealed class BuffReading : IEquatable<BuffReading>
{
    public bool Known { get; set; }

    public int Power { get; set; }

    public bool HasExact { get; set; }

    public AttributeAmounts AttributeBonus { get; } = new();

    public int WeaponBonus { get; set; }

    public int ArmorBonus { get; set; }

    public bool Equals(BuffReading? other) =>
        other != null
        && (Known, Power, HasExact, WeaponBonus, ArmorBonus)
        == (other.Known, other.Power, other.HasExact, other.WeaponBonus, other.ArmorBonus)
        && AttributeBonus.Equals(other.AttributeBonus);

    public override bool Equals(object? other) => Equals(other as BuffReading);

    public override int GetHashCode() => HashCode.Combine(Known, Power, WeaponBonus, ArmorBonus);
}
