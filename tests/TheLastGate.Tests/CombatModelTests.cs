using TheLastGate.Rules;

namespace TheLastGate.Tests;

public class CombatModelTests
{
    private const int WeaponSkillReach = 16;

    private readonly CombatModelInput input;

    public CombatModelTests()
    {
        input = new CombatModelInput
        {
            Weapon = CombatWeapon.Sword,
            ServerHit = 100,
            ServerParry = 90,
            BaseWeapon = 30,
            BaseArmor = 20,
        };
        Array.Fill(input.Known, true);
    }

    [Fact]
    public void DefaultWeapon_IsTheHighestKnownWeaponSkill()
    {
        Set(CombatSkill.Dagger, 50);
        Set(CombatSkill.Sword, 80);
        Set(CombatSkill.Axe, 80);
        Set(CombatSkill.Staff, 90);
        Forget(CombatSkill.Staff);

        Assert.Equal(CombatWeapon.Sword, CombatModel.DefaultWeapon(input.Known, input.Current));
    }

    [Fact]
    public void DefaultWeapon_IsUnarmedWithNoWeaponSkill()
    {
        Forget(
            CombatSkill.Hand,
            CombatSkill.Dagger,
            CombatSkill.Sword,
            CombatSkill.Axe,
            CombatSkill.Staff,
            CombatSkill.TwoHanded);
        Set(CombatSkill.Sword, 80);

        Assert.Equal(CombatWeapon.Unarmed, CombatModel.DefaultWeapon(input.Known, input.Current));
    }

    [Fact]
    public void DefaultOffhand_IsTheHigherKnownOfShieldAndDualWield()
    {
        Set(CombatSkill.Shield, 40);
        Set(CombatSkill.DualWield, 60);
        Assert.Equal(CombatOffhand.DualWield, CombatModel.DefaultOffhand(input.Known, input.Current));

        Forget(CombatSkill.DualWield);
        Assert.Equal(CombatOffhand.Shield, CombatModel.DefaultOffhand(input.Known, input.Current));

        Forget(CombatSkill.Shield);
        Assert.Equal(CombatOffhand.None, CombatModel.DefaultOffhand(input.Known, input.Current));
    }

    [Fact]
    public void DefaultOffhand_PrefersShieldOnATie()
    {
        Set(CombatSkill.Shield, 50);
        Set(CombatSkill.DualWield, 50);

        Assert.Equal(CombatOffhand.Shield, CombatModel.DefaultOffhand(input.Known, input.Current));
    }

    [Fact]
    public void TheWeaponSkill_MovesHitAndParry()
    {
        Plan(CombatSkill.Sword, 80, 90);

        Assert.Equal(new CombatValues(110, 100, 30, 20), CombatModel.Compute(input));
    }

    [Fact]
    public void TheWeaponSkill_CountsUpTo300()
    {
        Plan(CombatSkill.Sword, 295, 310);

        Assert.Equal(105, CombatModel.Compute(input).Hit);
    }

    [Fact]
    public void ASpear_UsesTheBetterOfDaggerAndStaff()
    {
        input.Weapon = CombatWeapon.Spear;
        Plan(CombatSkill.Dagger, 50, 50);
        Plan(CombatSkill.Staff, 60, 70);
        Assert.Equal(110, CombatModel.Compute(input).Hit);

        Plan(CombatSkill.Dagger, 50, 80);
        Assert.Equal(120, CombatModel.Compute(input).Hit);
    }

    [Fact]
    public void DualWield_AddsAQuarterToHit()
    {
        input.Offhand = CombatOffhand.DualWield;
        Plan(CombatSkill.DualWield, 40, 48);

        var values = CombatModel.Compute(input);

        Assert.Equal(102, values.Hit);
        Assert.Equal(90, values.Parry);
    }

    [Fact]
    public void AShield_AddsAQuarterToParryAndRampartAnEighthMore()
    {
        input.Offhand = CombatOffhand.Shield;
        Plan(CombatSkill.Shield, 40, 48);
        Assert.Equal(92, CombatModel.Compute(input).Parry);
        Assert.Equal(100, CombatModel.Compute(input).Hit);

        input.Rampart = true;
        Assert.Equal(93, CombatModel.Compute(input).Parry);
    }

    [Fact]
    public void OffhandSkills_CountUpTo300()
    {
        input.Offhand = CombatOffhand.Shield;
        Plan(CombatSkill.Shield, 300, 312);
        Assert.Equal(90, CombatModel.Compute(input).Parry);

        input.Offhand = CombatOffhand.DualWield;
        Plan(CombatSkill.DualWield, 292, 312);
        Assert.Equal(102, CombatModel.Compute(input).Hit);
    }

    [Fact]
    public void Tactics_AddsASixthToHitAndParry()
    {
        Plan(CombatSkill.Tactics, 60, 66);

        var values = CombatModel.Compute(input);

        Assert.Equal(101, values.Hit);
        Assert.Equal(91, values.Parry);
    }

    [Fact]
    public void ASkillNotKnown_AddsNothing()
    {
        Forget(CombatSkill.Tactics);
        Plan(CombatSkill.Tactics, 60, 66);

        var values = CombatModel.Compute(input);

        Assert.Equal(100, values.Hit);
        Assert.Equal(90, values.Parry);
    }

    [Theory]
    [InlineData(CombatGearClass.Other, 35, 25)]
    [InlineData(CombatGearClass.TemplarOrBraver, 40, 30)]
    [InlineData(CombatGearClass.ArchTemplar, 42, 37)]
    [InlineData(CombatGearClass.Skald, 47, 32)]
    public void GearMastery_DividesByTheClassesDivisors(CombatGearClass gearClass, int weapon, int armor)
    {
        input.GearClass = gearClass;
        Plan(CombatSkill.GearMastery, 100, 150);

        var values = CombatModel.Compute(input);

        Assert.Equal(weapon, values.Weapon);
        Assert.Equal(armor, values.Armor);
    }

    [Fact]
    public void Hand_AddsAQuarterToWeaponValueOnlyUnarmed()
    {
        input.Weapon = CombatWeapon.Unarmed;
        Plan(CombatSkill.Hand, 80, 100);
        Assert.Equal(120, CombatModel.Compute(input).Hit);
        Assert.Equal(35, CombatModel.Compute(input).Weapon);

        input.Weapon = CombatWeapon.Claw;
        Assert.Equal(120, CombatModel.Compute(input).Hit);
        Assert.Equal(30, CombatModel.Compute(input).Weapon);
    }

    [Fact]
    public void Ferocity_AddsToWeaponAndArmorValueForALycanth()
    {
        input.Lycanth = true;
        input.EmptySlots = 4;
        Plan(CombatSkill.Ferocity, 40, 60);
        Assert.Equal(34, CombatModel.Compute(input).Weapon);
        Assert.Equal(24, CombatModel.Compute(input).Armor);

        input.Lycanth = false;
        Assert.Equal(30, CombatModel.Compute(input).Weapon);
        Assert.Equal(20, CombatModel.Compute(input).Armor);
    }

    [Fact]
    public void SkillsAffecting_HitAndParry()
    {
        input.Weapon = CombatWeapon.Spear;
        input.Offhand = CombatOffhand.DualWield;
        var hit = CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach);
        Assert.Equal(
            new HashSet<CombatSkill>
                { CombatSkill.Dagger, CombatSkill.Staff, CombatSkill.Tactics, CombatSkill.DualWield },
            hit);

        Assert.DoesNotContain(
            CombatSkill.DualWield,
            CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach));

        input.Offhand = CombatOffhand.Shield;
        Assert.Contains(CombatSkill.Shield, CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach));

        Forget(CombatSkill.Tactics);
        Assert.DoesNotContain(
            CombatSkill.Tactics,
            CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach));
    }

    [Fact]
    public void SkillsAffecting_WeaponAndArmorValue()
    {
        input.Weapon = CombatWeapon.Unarmed;
        Assert.Equal(
            new HashSet<CombatSkill> { CombatSkill.GearMastery, CombatSkill.Hand },
            CombatModel.SkillsAffecting(input, CombatStat.Weapon, WeaponSkillReach));

        input.Lycanth = true;
        Assert.Equal(
            new HashSet<CombatSkill> { CombatSkill.GearMastery, CombatSkill.Ferocity },
            CombatModel.SkillsAffecting(input, CombatStat.Armor, WeaponSkillReach));
    }

    [Fact]
    public void TheBestWeaponSkill_IsUsedWhateverTheWeapon()
    {
        input.BestWeaponSkill = true;
        Plan(CombatSkill.Hand, 50, 50);
        Plan(CombatSkill.Sword, 80, 80);
        Plan(CombatSkill.Axe, 70, 90);

        var values = CombatModel.Compute(input);

        Assert.Equal(110, values.Hit);
        Assert.Equal(100, values.Parry);
    }

    [Fact]
    public void TheBestWeaponSkill_GivesNoUnarmedWeaponValue()
    {
        input.BestWeaponSkill = true;
        input.Weapon = CombatWeapon.Unarmed;
        Plan(CombatSkill.Hand, 40, 80);

        var values = CombatModel.Compute(input);

        Assert.Equal(30, values.Weapon);
        Assert.Equal(140, values.Hit);
    }

    [Fact]
    public void TheBestWeaponSkill_AffectsOnlyWeaponSkillsWithinReachOfTheBest()
    {
        input.BestWeaponSkill = true;
        Forget(CombatSkill.TwoHanded);
        Plan(CombatSkill.Hand, 50, 50);
        Plan(CombatSkill.Sword, 80, 80);
        Plan(CombatSkill.Axe, 70, 65);
        Plan(CombatSkill.TwoHanded, 79, 79);
        Assert.Equal(
            new HashSet<CombatSkill> { CombatSkill.Sword, CombatSkill.Axe, CombatSkill.Tactics },
            CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach));

        Plan(CombatSkill.Axe, 70, 64);
        Assert.DoesNotContain(CombatSkill.Axe, CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach));

        var parry = CombatModel.SkillsAffecting(input, CombatStat.Parry, 31);
        Assert.Contains(CombatSkill.Hand, parry);
        Assert.Contains(CombatSkill.Axe, parry);
        Assert.DoesNotContain(CombatSkill.Dagger, parry);
    }

    [Fact]
    public void ParryFeedingHit_MakesTheShieldAffectHit()
    {
        input.Offhand = CombatOffhand.Shield;
        Assert.DoesNotContain(CombatSkill.Shield, CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach));

        input.ParryFeedsHit = true;
        var hit = CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach);
        Assert.Contains(CombatSkill.Shield, hit);
        Assert.Contains(CombatSkill.Sword, hit);
        Assert.DoesNotContain(CombatSkill.DualWield, hit);

        input.Offhand = CombatOffhand.DualWield;
        hit = CombatModel.SkillsAffecting(input, CombatStat.Hit, WeaponSkillReach);
        Assert.DoesNotContain(CombatSkill.Shield, hit);
        Assert.Contains(CombatSkill.DualWield, hit);
    }

    [Fact]
    public void HitFeedingParry_MakesDualWieldAffectParry()
    {
        input.Offhand = CombatOffhand.DualWield;
        Assert.DoesNotContain(
            CombatSkill.DualWield,
            CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach));

        input.HitFeedsParry = true;
        var parry = CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach);
        Assert.Contains(CombatSkill.DualWield, parry);
        Assert.Contains(CombatSkill.Sword, parry);
        Assert.DoesNotContain(CombatSkill.Shield, parry);

        Forget(CombatSkill.DualWield);
        Assert.DoesNotContain(
            CombatSkill.DualWield,
            CombatModel.SkillsAffecting(input, CombatStat.Parry, WeaponSkillReach));
    }

    [Fact]
    public void Values_CanBeReadByStat()
    {
        var values = new CombatValues(1, 2, 3, 4);

        Assert.Equal([1, 2, 3, 4], Enum.GetValues<CombatStat>().Select(stat => values[stat]));
    }

    private void Set(CombatSkill skill, int current) => input.Current[(int)skill] = current;

    private void Plan(CombatSkill skill, int current, int shown)
    {
        input.Current[(int)skill] = current;
        input.Shown[(int)skill] = shown;
    }

    private void Forget(params CombatSkill[] skills)
    {
        foreach (var skill in skills)
        {
            input.Known[(int)skill] = false;
        }
    }
}
