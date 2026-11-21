using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class ItemEffectsTests
{
    private readonly ItemEffectsInput items = new();
    private readonly TreeEffectsInput tree = new();

    public static TheoryData<Gear, Func<PlannerEffects, int>, Action<PlannerEffects>> GearFlags => new()
    {
        { Gear.TravelersGuide, effects => effects.TravelersGuide, effects => effects.TravelersGuide = 0 },
        { Gear.FistsOfTheHeavens, effects => effects.FistsOfHeavens, effects => effects.FistsOfHeavens = 0 },
        { Gear.GrandeGrimoire, effects => effects.GrandeGrimoire, effects => effects.GrandeGrimoire = 0 },
        {
            Gear.ImprovedBlackTachi, effects => effects.SpellBonusToWeapon,
            effects => effects.SpellBonusToWeapon = 0
        },
        {
            Gear.ImprovedWhiteOdachi, effects => effects.SpellBonusToArmor,
            effects => effects.SpellBonusToArmor = 0
        },
        { Gear.Evergreen, effects => effects.Evergreen, effects => effects.Evergreen = 0 },
        {
            Gear.GestureOfGorn, effects => effects.HitpointsToSpellBonus,
            effects => effects.HitpointsToSpellBonus = 0
        },
        { Gear.ImprovedNeisClaw, effects => effects.ManaToSpellBonus, effects => effects.ManaToSpellBonus = 0 },
        { Gear.Superbia, effects => effects.Superbia, effects => effects.Superbia = 0 },
        { Gear.ImprovedAjaxBlue, effects => effects.BraveIntoStrength, effects => effects.BraveIntoStrength = 0 },
    };

    [Fact]
    public void NoItems_AddNothing()
    {
        Assert.Equal(new PlannerEffects(), Compute());
    }

    [Fact]
    public void AttributeEnchants_Stack()
    {
        TickEnchant(Enchant.MoreStrength, 2);
        TickEnchant(Enchant.MoreAttributes, 1);

        var effects = Compute();

        Assert.Equal(8, effects.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(2, effects.AttributePercent[AttributeSlot.Braveness]);
    }

    [Fact]
    public void AnUntickedEnchantCount_IsIgnored()
    {
        items.EnchantCounts[(int)Enchant.MoreStrength] = 4;

        Assert.Equal(0, Compute().AttributePercent[AttributeSlot.Strength]);
    }

    [Fact]
    public void TickboxEnchants_CountOneCopyWhateverTheCount()
    {
        TickEnchant(Enchant.MoreArmor, 5);
        TickEnchant(Enchant.MoreResistance, 5);

        var effects = Compute();

        Assert.Equal(1, effects.ArmorEnchant);
        Assert.Equal(10, effects.ResistEnchantPercent);
        Assert.Equal(1, ItemEffects.EnchantCopies(items, Enchant.MoreArmor));
        Assert.Equal(0, ItemEffects.EnchantCopies(items, Enchant.MoreStrength));
    }

    [Fact]
    public void SpellBonusAndKwaiEnchants_Stack()
    {
        TickEnchant(Enchant.MoreSpellBonus, 3);
        TickEnchant(Enchant.HitToParry, 2);
        TickEnchant(Enchant.ParryToHit, 1);

        var effects = Compute();

        Assert.Equal(30, effects.SpellBonusEnchantPercent);
        Assert.Equal(6, effects.HitToParryPercent);
        Assert.Equal(3, effects.ParryToHitPercent);
    }

    [Fact]
    public void ArmorShareEnchants_AreFlags()
    {
        TickEnchant(Enchant.ArmorToResistance, 1);
        TickEnchant(Enchant.ArmorToImmunity, 1);

        var effects = Compute();

        Assert.Equal(1, effects.ArmorToResist);
        Assert.Equal(1, effects.ArmorToImmunity);
    }

    [Fact]
    public void Gear_AddsSkillAndAttributePercents()
    {
        foreach (var gear in (Gear[])
            [
                Gear.ImprovedLionsPaws, Gear.ImprovedCobaltLance, Gear.Haladie, Gear.ImprovedShivasScepter,
                Gear.Muramasa, Gear.ImprovedBarbarianSword, Gear.ImprovedVioletGaze,
            ])
        {
            TickGear(gear);
        }

        var effects = Compute();

        Assert.Equal(40, effects.SkillExtraPercent[AttributeSlot.Braveness]);
        Assert.Equal(10, effects.SkillExtraPercent[AttributeSlot.Willpower]);
        Assert.Equal(10, effects.SkillExtraPercent[AttributeSlot.Intuition]);
        Assert.Equal(20, effects.SkillExtraPercent[AttributeSlot.Agility]);
        Assert.Equal(10, effects.SkillExtraPercent[AttributeSlot.Strength]);
        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Willpower]);
        Assert.Equal(4, effects.AttributePercent[AttributeSlot.Intuition]);
    }

    [Theory]
    [MemberData(nameof(GearFlags))]
    public void FlagGear_SetsOnlyItsOwnFlag(Gear gear, Func<PlannerEffects, int> flag, Action<PlannerEffects> clear)
    {
        TickGear(gear);

        var effects = Compute();

        Assert.Equal(1, flag(effects));
        clear(effects);
        Assert.Equal(new PlannerEffects(), effects);
    }

    [Fact]
    public void AjaxBlue_AddsTheBravenessPercentToStrength()
    {
        TickEnchant(Enchant.MoreBraveness, 1);
        TickGear(Gear.ImprovedAjaxBlue);

        var effects = Compute();

        Assert.Equal(3, effects.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(1, effects.BraveIntoStrength);
    }

    [Fact]
    public void SkillPercents_StopAt200()
    {
        TickCorruptor(Corruptor.FalseBarbarism, 20);
        TickGear(Gear.ImprovedBarbarianSword);

        Assert.Equal(200, Compute().SkillExtraPercent[AttributeSlot.Strength]);
    }

    [Fact]
    public void AttributePercents_StopAt200()
    {
        TickCorruptor(Corruptor.FalseBraveness, 20);
        TickCorruptor(Corruptor.FalseStrength, 20);
        TickEnchant(Enchant.MoreBraveness, 20);
        TickEnchant(Enchant.MoreStrength, 20);
        TickEnchant(Enchant.MoreAttributes, 20);
        TickGear(Gear.ImprovedAjaxBlue);

        var effects = Compute();

        Assert.Equal(160, effects.AttributePercent[AttributeSlot.Braveness]);
        Assert.Equal(200, effects.AttributePercent[AttributeSlot.Strength]);
    }

    [Fact]
    public void GearNotModelled_ChangesNothing()
    {
        TickGear(Gear.CudgelOfKwai);
        TickGear(Gear.Quicksilver);
        TickGear(Gear.Excalibur);

        Assert.Equal(new PlannerEffects(), Compute());
        Assert.False(ItemEffects.Gears[(int)Gear.CudgelOfKwai].Modelled);
        Assert.False(ItemEffects.Gears[(int)Gear.Excalibur].Modelled);
        Assert.False(ItemEffects.Gears[(int)Gear.Quicksilver].Modelled);
        Assert.True(ItemEffects.Gears[(int)Gear.Superbia].Modelled);
    }

    [Fact]
    public void TreeEffects_StillApply()
    {
        tree.TreeClass = TreeClass.Seyan;
        tree.TreePoints = 1 << (12 - 8 + 4);
        TickEnchant(Enchant.MoreStrength, 1);

        var effects = Compute();

        Assert.Equal(8, effects.AttributePercent[AttributeSlot.Strength]);
        Assert.Equal(5, effects.AttributePercent[AttributeSlot.Willpower]);
    }

    [Fact]
    public void Enchants_AreKnownByTheServersNumbers()
    {
        int[] serverNumbers = [6, 17, 27, 36, 44, 84, 87, 96, 97, 2, 21, 16, 35];
        Enchant[] tickboxOnly =
            [Enchant.MoreArmor, Enchant.MoreResistance, Enchant.ArmorToResistance, Enchant.ArmorToImmunity];

        Assert.Equal(serverNumbers, ItemEffects.Enchants.Select(enchant => enchant.ServerNumber));
        foreach (var enchant in Enum.GetValues<Enchant>())
        {
            var info = ItemEffects.Enchants[(int)enchant];
            Assert.Equal(!tickboxOnly.Contains(enchant), info.Counted);
            Assert.True(info.OneCopy.Length <= 16);
        }
    }

    private PlannerEffects Compute() => ItemEffects.Compute(tree, items);

    private void TickEnchant(Enchant enchant, int count)
    {
        items.EnchantTicks |= 1u << (int)enchant;
        items.EnchantCounts[(int)enchant] = count;
    }

    private void TickGear(Gear gear) => items.GearTicks |= 1u << (int)gear;

    private void TickCorruptor(Corruptor corruptor, int count)
    {
        tree.CorruptorTicks |= 1u << (int)corruptor;
        tree.CorruptorCounts[(int)corruptor] = count;
    }
}
