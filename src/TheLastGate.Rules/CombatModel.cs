using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TheLastGate.Rules;

public enum CombatSkill
{
    Hand,
    Dagger,
    Sword,
    Axe,
    Staff,
    TwoHanded,
    DualWield,
    Shield,
    Tactics,
    GearMastery,
    Ferocity,
}

public enum CombatWeapon
{
    Unarmed,
    Claw,
    Dagger,
    Sword,
    Axe,
    Staff,
    TwoHanded,
    Spear,
    Greataxe,
}

public enum CombatOffhand
{
    None,
    Shield,
    DualWield,
}

public enum CombatGearClass
{
    Other,
    TemplarOrBraver,
    ArchTemplar,
    Skald,
}

public enum CombatStat
{
    Hit,
    Parry,
    Weapon,
    Armor,
}

public readonly record struct CombatValues(int Hit, int Parry, int Weapon, int Armor)
{
    public int this[CombatStat stat] => stat switch
    {
        CombatStat.Hit => Hit,
        CombatStat.Parry => Parry,
        CombatStat.Weapon => Weapon,
        CombatStat.Armor => Armor,
        _ => throw new UnreachableException(),
    };
}

// Equal to another input with the same numbers.
public sealed class CombatModelInput : IEquatable<CombatModelInput>
{
    public bool[] Known { get; } = new bool[CombatModel.SkillCount];

    // The server's totals.
    public int[] Current { get; } = new int[CombatModel.SkillCount];

    // Planned, with the buffs being shown.
    public int[] Shown { get; } = new int[CombatModel.SkillCount];

    public CombatWeapon Weapon { get; set; }

    public CombatOffhand Offhand { get; set; }

    public CombatGearClass GearClass { get; set; }

    public bool Rampart { get; set; }

    public bool Lycanth { get; set; }

    public int EmptySlots { get; set; }

    public int ServerHit { get; set; }

    public int ServerParry { get; set; }

    // Weapon and Armor Value before skill changes, with the buffs being shown.
    public int BaseWeapon { get; set; }

    public int BaseArmor { get; set; }

    // Hit and Parry use the highest weapon skill, whatever the weapon.
    public bool BestWeaponSkill { get; set; }

    // Parry gains part of Hit after the tarot steps, and the other way round; only SkillsAffecting reads these.
    public bool HitFeedsParry { get; set; }

    public bool ParryFeedsHit { get; set; }

    public bool Equals(CombatModelInput? other) =>
        other != null
        && (Weapon, Offhand, GearClass, Rampart, Lycanth, EmptySlots, ServerHit)
        == (other.Weapon, other.Offhand, other.GearClass, other.Rampart, other.Lycanth, other.EmptySlots,
            other.ServerHit)
        && (ServerParry, BaseWeapon, BaseArmor, BestWeaponSkill, HitFeedsParry, ParryFeedsHit)
        == (other.ServerParry, other.BaseWeapon, other.BaseArmor, other.BestWeaponSkill, other.HitFeedsParry,
            other.ParryFeedsHit)
        && Known.SequenceEqual(other.Known)
        && Current.SequenceEqual(other.Current)
        && Shown.SequenceEqual(other.Shown);

    public CombatModelInput Copy()
    {
        var copy = new CombatModelInput
        {
            Weapon = Weapon,
            Offhand = Offhand,
            GearClass = GearClass,
            Rampart = Rampart,
            Lycanth = Lycanth,
            EmptySlots = EmptySlots,
            ServerHit = ServerHit,
            ServerParry = ServerParry,
            BaseWeapon = BaseWeapon,
            BaseArmor = BaseArmor,
            BestWeaponSkill = BestWeaponSkill,
            HitFeedsParry = HitFeedsParry,
            ParryFeedsHit = ParryFeedsHit,
        };
        Known.CopyTo(copy.Known, 0);
        Current.CopyTo(copy.Current, 0);
        Shown.CopyTo(copy.Shown, 0);
        return copy;
    }

    public override bool Equals(object? other) => Equals(other as CombatModelInput);

    public override int GetHashCode() => HashCode.Combine(Weapon, Offhand, ServerHit, ServerParry, BaseWeapon);
}

// What the planner shows for Hit, Parry, Weapon Value and Armor Value. The server only sends the totals, so each is
// the server value plus what every combat skill adds at its shown (planned and buffed) value, minus what it adds now.
// The client can't see what is equipped, so the weapon and off-hand are chosen in the planner.
public static class CombatModel
{
    public const int SkillCount = 11;

    private const int WeaponSkillCap = 300;
    private const int StatMost = 999;
    private const int TacticsDivisor = 6;
    private const int OffhandDivisor = 4;
    private const int RampartDivisor = 8;
    private const int HandDivisor = 4;
    private const int FerocityEmptySlotDivisor = 10;

    private static readonly string[] WeaponNameTable =
        ["Unarmed", "Claw", "Dagger", "Sword", "Axe", "Staff", "Two-Handed", "Spear", "Greataxe"];

    private static readonly string[] OffhandNameTable = ["None", "Shield", "Dual Wield"];

    private static readonly string[] StatNameTable = ["Hit Score", "Parry Score", "Weapon Value", "Armor Value"];

    // The weapon skills a default can come from, with the weapon each stands for; a tie goes to the earlier.
    private static readonly (CombatSkill Skill, CombatWeapon Weapon)[] WeaponSkills =
    [
        (CombatSkill.Hand, CombatWeapon.Unarmed), (CombatSkill.Dagger, CombatWeapon.Dagger),
        (CombatSkill.Sword, CombatWeapon.Sword), (CombatSkill.Axe, CombatWeapon.Axe),
        (CombatSkill.Staff, CombatWeapon.Staff), (CombatSkill.TwoHanded, CombatWeapon.TwoHanded),
    ];

    public static IReadOnlyList<string> WeaponNames => WeaponNameTable;

    public static IReadOnlyList<string> OffhandNames => OffhandNameTable;

    public static IReadOnlyList<string> StatNames => StatNameTable;

    public static CombatWeapon DefaultWeapon(bool[] known, int[] current)
    {
        var bestWeapon = CombatWeapon.Unarmed;
        var bestValue = -1;
        foreach (var (skill, weapon) in WeaponSkills)
        {
            if (known[(int)skill] && current[(int)skill] > bestValue)
            {
                bestValue = current[(int)skill];
                bestWeapon = weapon;
            }
        }

        return bestWeapon;
    }

    // A Shield wins a tie with Dual Wield.
    public static CombatOffhand DefaultOffhand(bool[] known, int[] current)
    {
        var shield = (int)CombatSkill.Shield;
        var dual = (int)CombatSkill.DualWield;
        if (known[shield] && (!known[dual] || current[shield] >= current[dual]))
        {
            return CombatOffhand.Shield;
        }

        return known[dual] ? CombatOffhand.DualWield : CombatOffhand.None;
    }

    public static CombatValues Compute(CombatModelInput input) =>
        new(
            Shown(input.ServerHit, HitTerm(input, input.Shown) - HitTerm(input, input.Current)),
            Shown(input.ServerParry, ParryTerm(input, input.Shown) - ParryTerm(input, input.Current)),
            Shown(input.BaseWeapon, WeaponValueTerm(input, input.Shown) - WeaponValueTerm(input, input.Current)),
            Shown(input.BaseArmor, ArmorValueTerm(input, input.Shown) - ArmorValueTerm(input, input.Current)));

    // The known skills whose value can move the stat with the chosen weapon, off-hand and race. With the best weapon
    // skill counting, only weapon skills less than the reach below the best are counted.
    public static IReadOnlySet<CombatSkill> SkillsAffecting(
        CombatModelInput input,
        CombatStat stat,
        int weaponSkillReach)
    {
        var affecting = new HashSet<CombatSkill>();
        switch (stat)
        {
            case CombatStat.Hit or CombatStat.Parry:
                if (input.BestWeaponSkill)
                {
                    var highest = HighestWeaponSkill(input, input.Shown);
                    affecting.UnionWith(
                        WeaponSkills.Select(entry => entry.Skill)
                            .Where(skill => highest - input.Shown[(int)skill] < weaponSkillReach));
                }
                else
                {
                    affecting.UnionWith(SkillsOf(input.Weapon));
                }

                affecting.Add(CombatSkill.Tactics);
                if (input.Offhand == CombatOffhand.DualWield && (stat == CombatStat.Hit || input.HitFeedsParry))
                {
                    affecting.Add(CombatSkill.DualWield);
                }

                if (input.Offhand == CombatOffhand.Shield && (stat == CombatStat.Parry || input.ParryFeedsHit))
                {
                    affecting.Add(CombatSkill.Shield);
                }

                break;
            case CombatStat.Weapon or CombatStat.Armor:
                affecting.Add(CombatSkill.GearMastery);
                if (input.Lycanth)
                {
                    affecting.Add(CombatSkill.Ferocity);
                }

                if (stat == CombatStat.Weapon && input.Weapon == CombatWeapon.Unarmed && !input.BestWeaponSkill)
                {
                    affecting.Add(CombatSkill.Hand);
                }

                break;
            default:
                throw new UnreachableException();
        }

        affecting.RemoveWhere(skill => !input.Known[(int)skill]);
        return affecting;
    }

    private static int Shown(int server, int change) => Math.Clamp(server + change, 0, StatMost);

    // A Spear or Greataxe uses the better of its two skills.
    private static CombatSkill[] SkillsOf(CombatWeapon weapon) => weapon switch
    {
        CombatWeapon.Unarmed or CombatWeapon.Claw => [CombatSkill.Hand],
        CombatWeapon.Dagger => [CombatSkill.Dagger],
        CombatWeapon.Sword => [CombatSkill.Sword],
        CombatWeapon.Axe => [CombatSkill.Axe],
        CombatWeapon.Staff => [CombatSkill.Staff],
        CombatWeapon.TwoHanded => [CombatSkill.TwoHanded],
        CombatWeapon.Spear => [CombatSkill.Dagger, CombatSkill.Staff],
        CombatWeapon.Greataxe => [CombatSkill.Axe, CombatSkill.TwoHanded],
        _ => throw new UnreachableException(),
    };

    private static int SkillValue(CombatModelInput input, int[] skills, CombatSkill skill) =>
        input.Known[(int)skill] ? skills[(int)skill] : 0;

    private static int HighestWeaponSkill(CombatModelInput input, int[] skills) =>
        Math.Max(WeaponSkills.Max(entry => SkillValue(input, skills, entry.Skill)), 0);

    private static int WeaponTerm(CombatModelInput input, int[] skills)
    {
        var value = input.BestWeaponSkill
            ? HighestWeaponSkill(input, skills)
            : SkillsOf(input.Weapon).Max(skill => SkillValue(input, skills, skill));
        return Math.Min(value, WeaponSkillCap);
    }

    private static int TacticsTerm(CombatModelInput input, int[] skills) =>
        SkillValue(input, skills, CombatSkill.Tactics) / TacticsDivisor;

    private static int HitTerm(CombatModelInput input, int[] skills)
    {
        var term = WeaponTerm(input, skills) + TacticsTerm(input, skills);
        if (input.Offhand == CombatOffhand.DualWield)
        {
            term += Math.Min(SkillValue(input, skills, CombatSkill.DualWield), WeaponSkillCap) / OffhandDivisor;
        }

        return term;
    }

    // Rampart makes the Shield add a quarter and an eighth.
    private static int ParryTerm(CombatModelInput input, int[] skills)
    {
        var term = WeaponTerm(input, skills) + TacticsTerm(input, skills);
        if (input.Offhand == CombatOffhand.Shield)
        {
            var shield = Math.Min(SkillValue(input, skills, CombatSkill.Shield), WeaponSkillCap);
            term += shield / OffhandDivisor + (input.Rampart ? shield / RampartDivisor : 0);
        }

        return term;
    }

    private static int GearMasteryDivisor(CombatGearClass gearClass, bool forArmor) => gearClass switch
    {
        CombatGearClass.Other => 10,
        CombatGearClass.TemplarOrBraver => 5,
        CombatGearClass.ArchTemplar => forArmor ? 3 : 4,
        CombatGearClass.Skald => forArmor ? 4 : 3,
        _ => throw new UnreachableException(),
    };

    // Each empty gear slot makes a Lycanth's Ferocity stronger.
    private static int FerocityTerm(CombatModelInput input, int[] skills)
    {
        if (!input.Lycanth)
        {
            return 0;
        }

        var ferocity = SkillValue(input, skills, CombatSkill.Ferocity);
        return (ferocity / 2 + ferocity * input.EmptySlots / FerocityEmptySlotDivisor) / 4;
    }

    private static int WeaponValueTerm(CombatModelInput input, int[] skills)
    {
        var term = SkillValue(input, skills, CombatSkill.GearMastery) / GearMasteryDivisor(input.GearClass, false)
            + FerocityTerm(input, skills);
        if (input.Weapon == CombatWeapon.Unarmed && !input.BestWeaponSkill)
        {
            term += SkillValue(input, skills, CombatSkill.Hand) / HandDivisor;
        }

        return term;
    }

    private static int ArmorValueTerm(CombatModelInput input, int[] skills) =>
        SkillValue(input, skills, CombatSkill.GearMastery) / GearMasteryDivisor(input.GearClass, true)
        + FerocityTerm(input, skills);
}
