using GdUnit4;
using TheLastGate.Buffs;
using TheLastGate.Planner;
using TheLastGate.State;
using TheLastGate.Stats;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class PlannerSheetTests
{
    private const int BlessSkill = 21;
    private const int StaffSkill = 5;
    private const int SwordSkill = 3;
    private const int Strength = (int)AttributeSlot.Strength;
    private const int Agility = (int)AttributeSlot.Agility;
    private const int Braveness = (int)AttributeSlot.Braveness;

    private PlannerGame game = null!;

    [BeforeTest]
    public void StartAGame() => game = Game();

    [TestCase]
    public void WithoutTheMetaStats_TheRowsShowTheServersValues()
    {
        game.MetaStats.Clear();

        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.BuffsAvailable).IsFalse();
        AssertThat(sheet.Attributes.Select(row => (row.Preview, row.BuffShift)).Distinct().ToArray())
            .ContainsExactly((50, 0));
    }

    [TestCase]
    public void AShownBless_LiftsTheAttributeRows()
    {
        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.BuffsAvailable).IsTrue();
        AssertThat(sheet.Planned!.CastPower[(int)BuffKind.Bless]).IsEqual(66);
        AssertThat(sheet.Attributes.Select(row => (row.Preview, row.BuffShift)).Distinct().ToArray())
            .ContainsExactly((61, 11));
    }

    [TestCase]
    public void ABlessNotToggledOn_LeavesThemAlone()
    {
        game.Choices.SpellToggles = 0;

        AssertThat(PlannerSheet.For(game).Attributes.Select(row => row.Preview).Distinct().ToArray())
            .ContainsExactly(50);
    }

    [TestCase]
    public void APlannedAttribute_ShowsWhatWasBoughtAndTheNextCost()
    {
        game.Choices.SpellToggles = 0;
        game.Plan.Raise(Strength, game.ValuesFor(Strength), game.Points, 1);

        var strength = PlannerSheet.For(game).Attributes[Strength];

        AssertThat((strength.Name, strength.Base, strength.Current, strength.DirectRaise, strength.Preview))
            .IsEqual(("Strength", 40, 50, 1, 51));
        AssertThat(strength.Cost).IsEqual(41 * 41 * 41 * 3 / 20);
        AssertThat(strength.CanRaise).IsTrue();
        AssertThat(strength.CanLower).IsTrue();
    }

    [TestCase]
    public void SkillRows_FollowTheListAndOnlyShowShownSkills()
    {
        game.SkillOrder = [BlessSkill, StaffSkill, SwordSkill];
        game.SkillTexts[StaffSkill] = new PlannerSkillText("Staff", [3, 3, 4], 1);
        game.SkillTexts[SwordSkill] = new PlannerSkillText("Sword", [3, 3, 4], 0);

        var skills = PlannerSheet.For(game).Skills;

        AssertThat(skills.Select(row => row.Name)).ContainsExactly("Bless", "Staff");
        AssertThat(skills[0].Slot).IsEqual(StatPlan.SkillSlot(BlessSkill));
    }

    [TestCase]
    public void ASkillRow_GainsFromItsRaisedAttributes()
    {
        game.MetaStats.Clear();
        game.SkillOrder = [StaffSkill];
        game.SkillTexts[StaffSkill] = new PlannerSkillText("Staff", [Agility, Agility, Strength], 1);
        game.Skills[StaffSkill][0] = 20;
        game.Plan.Raise(Agility, game.ValuesFor(Agility), game.Points, 3);

        var staffs = PlannerSheet.For(game).Skills;
        AssertThat(staffs).HasSize(1);
        var staff = staffs[0];

        // (53 + 53 + 50) / 5 = 31 against 150 / 5 = 30.
        AssertThat(staff.AttributeGain).IsEqual(1);
        AssertThat(staff.UsesRaisedAttribute).IsTrue();
    }

    [TestCase]
    public void InHardMode_TheAttributesDivideBySix()
    {
        game.MetaStats.Clear();
        game.HardMode = true;
        game.SkillOrder = [StaffSkill];
        game.SkillTexts[StaffSkill] = new PlannerSkillText("Staff", [Agility, Agility, Strength], 1);
        game.Skills[StaffSkill][0] = 20;
        game.Plan.Raise(Agility, game.ValuesFor(Agility), game.Points, 3);

        AssertThat(PlannerSheet.For(game).Skills.Single().AttributeGain).IsEqual(1);
        game.Plan.Lower(Agility, game.ValuesFor(Agility), 1);
        AssertThat(PlannerSheet.For(game).Skills.Single().AttributeGain).IsEqual(0);
    }

    [TestCase]
    public void ACappedStat_HasNoCostAndCantBeRaised()
    {
        game.Attributes[Strength][2] = 40;

        var strength = PlannerSheet.For(game).Attributes[Strength];

        AssertThat(strength.Cost).IsNull();
        AssertThat(strength.CanRaise).IsFalse();
    }

    [TestCase]
    public void TooFewPoints_CantRaiseAStat()
    {
        game.Points = 100;

        AssertThat(PlannerSheet.For(game).Attributes[Strength].CanRaise).IsFalse();
    }

    [TestCase]
    public void TheVitalRows_AreHitpointsAndMana()
    {
        game.HitPoints[0] = 120;
        game.HitPoints[5] = 300;
        game.Mana[0] = 80;
        game.Mana[5] = 150;

        var vitals = PlannerSheet.For(game).Vitals;

        AssertThat(vitals.Select(row => (row.Name, row.Slot, row.Base, row.Current)))
            .ContainsExactly(("Hitpoints", StatPlan.HitPointsSlot, 120, 300), ("Mana", StatPlan.ManaSlot, 80, 150));
    }

    [TestCase(1u << 4, 75)]
    [TestCase(1u << 0, 100)]
    [TestCase(1u << 6, 105)]
    [TestCase(1u << 1, 95)]
    [TestCase(1u << 5, 80)]
    [TestCase(1u << 12, 80)]
    [TestCase(1u << 10, 105)]
    [TestCase(1u << 11, 105)]
    [TestCase(1u << 13, 110)]
    [TestCase(1u << 9, 110)]
    [TestCase(1u << 14, 115)]
    [TestCase(1u << 16, 100)]
    [TestCase(0u, 100)]
    public void SpellRaceModifier_IsTheClasssPercent(uint kindred, int percent)
    {
        AssertThat(PlannerSheet.SpellRaceModifier(kindred)).IsEqual(percent);
    }

    [TestCase]
    public void EmptyGearSlots_LeaveOutTheCharmsAndTheOffhand()
    {
        var worn = new int[PlayerState.WornSlots];
        AssertThat(PlannerSheet.EmptyGearSlots(worn)).IsEqual(10);

        worn[0] = 100;
        worn[5] = 100;
        worn[7] = 100;
        AssertThat(PlannerSheet.EmptyGearSlots(worn)).IsEqual(9);
    }

    [TestCase]
    public void AFirstLook_PicksTheWeaponAndOffhandFromTheBestSkills()
    {
        game.Skills[SwordSkill][0] = 40;
        game.Skills[SwordSkill][5] = 80;

        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.ChoicesChanged).IsTrue();
        AssertThat(game.Choices.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(game.Choices.Offhand).IsEqual(CombatOffhand.None);
        AssertThat(sheet.Combat?.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(PlannerSheet.For(game).ChoicesChanged).IsFalse();
    }

    [TestCase]
    public void TheCombatModel_WaitsForHitAndParry()
    {
        game.MetaStats.Remove("Parry Score");

        AssertThat(PlannerSheet.For(game).Combat).IsNull();
    }

    [TestCase]
    public void TheCombatModel_ReadsTheClassAndGear()
    {
        game.Kindred = (1u << 5) | (1u << 16);
        game.TreePoints = 1 << (12 - 10 + 4);
        game.WornSprites[0] = 100;

        var combat = PlannerSheet.For(game).Combat!;

        AssertThat(combat.GearClass).IsEqual(CombatGearClass.ArchTemplar);
        AssertThat(combat.Rampart).IsTrue();
        AssertThat(combat.Lycanth).IsTrue();
        AssertThat(combat.EmptySlots).IsEqual(9);
        AssertThat((combat.ServerHit, combat.ServerParry)).IsEqual((100, 90));
    }

    [TestCase]
    public void WithNothingPlannedOrShown_TheCombatBlockShowsTheServersValues()
    {
        game.Choices.SpellToggles = 0;

        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.CombatShown).IsEqual(new CombatValues(100, 90, 30, 30));
        AssertThat(sheet.CombatChanged).IsFalse();
    }

    [TestCase]
    public void RaisingTheWeaponSkill_RaisesHit()
    {
        game.Choices.SpellToggles = 0;
        game.SkillTexts[SwordSkill] = new PlannerSkillText("Sword", [Braveness, Agility, Strength], 1);
        int[] sword = [40, 0, 200, 3, 0, 80];
        sword.CopyTo(game.Skills[SwordSkill], 0);
        var slot = StatPlan.SkillSlot(SwordSkill);
        game.Plan.Raise(slot, game.ValuesFor(slot), game.Points, 10);

        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.Combat?.Weapon).IsEqual(CombatWeapon.Sword);
        AssertThat(sheet.CombatShown?.Hit > 100).IsTrue();
        AssertThat(sheet.CombatChanged).IsTrue();
    }

    [TestCase]
    public void WithoutHitAndParry_ThereAreNoCombatValues()
    {
        game.MetaStats.Remove("Hit Score");

        AssertThat(PlannerSheet.For(game).CombatShown).IsNull();
    }

    [TestCase(125, true, "%", "1.25%")]
    [TestCase(7, true, "", "0.07")]
    [TestCase(90, false, "", "90")]
    [TestCase(250, false, " pts", "250 pts")]
    public void FormatMeta_WritesHundredthsForADecimalStat(int value, bool isDecimal, string affix, string text)
    {
        AssertThat(PlannerSheet.FormatMeta(value, new MetaFormat(isDecimal, affix))).IsEqual(text);
    }

    [TestCase]
    public void MetaText_IsADashUntilTheStatArrives()
    {
        game.MetaFormats[PlannerSheet.SpellMultiplier] = new MetaFormat(true, "x");

        var sheet = PlannerSheet.For(game);

        AssertThat(sheet.MetaText("Nothing Like It")).IsEqual("-");
        AssertThat(sheet.MetaText(PlannerSheet.SpellMultiplier)).IsEqual("1.00x");
        AssertThat(sheet.MetaText(PlannerSheet.ParryScore)).IsEqual("90");
    }

    [TestCase]
    public void MathLines_ListWhatFeedsTheNumbers()
    {
        const uint seyan = 1u << 1;
        game.HardMode = true;
        game.Kindred = seyan;
        game.TreePoints = TreeBit(5) | TreeBit(6) | TreeBit(7) | TreeBit(8);
        game.TreeNodeNames[4] = "Node Five";
        game.TreeNodes[5] = 20;
        game.TreeNodes[6] = 66;
        game.TreeNodes[7] = 250;
        game.Choices.TarotCards = 1ul << (int)TarotCard.Magician;
        game.Choices.CorruptorTicks = 1u << (int)Corruptor.FalseAgility;
        game.Choices.CorruptorCounts[(int)Corruptor.FalseAgility] = 2;
        game.Choices.CorruptorTicks |= 1u << (int)Corruptor.FalseStrength;
        game.Choices.EnchantTicks = 1u << (int)Enchant.MoreBraveness;
        game.Choices.EnchantCounts[(int)Enchant.MoreBraveness] = 3;
        var uncounted = FirstIndex(ItemEffects.Enchants, enchant => !enchant.Counted);
        game.Choices.EnchantTicks |= 1u << uncounted;
        game.Choices.EnchantCounts[uncounted] = 5;
        var unmodelled = FirstIndex(ItemEffects.Gears, gear => !gear.Modelled);
        game.Choices.GearTicks = 1u << unmodelled;

        AssertThat(PlannerSheet.For(game).MathLines)
            .ContainsExactly(
                "Hard Mode",
                "Bless",
                "The Magician",
                "Node Five (skill tree)",
                "False Strength (skill tree)",
                "False Tactician (not modelled) (skill tree)",
                "False Agility x2",
                "More Braveness x3",
                ItemEffects.Enchants[uncounted].Name,
                $"{ItemEffects.Gears[unmodelled].Name} (not modelled)");
    }

    [TestCase]
    public void MathLines_LeaveOutUnlearnedNodesAndSpellsYouDontKnow()
    {
        game.Kindred = 1u << 1;
        game.TreePoints = TreeBit(1);
        game.Skills[BlessSkill][0] = 0;

        AssertThat(PlannerSheet.For(game).MathLines).IsEmpty();
    }

    [TestCase]
    public void CardNames_AreTheUprightCardsThenTheReversed()
    {
        AssertThat(TarotMath.CardNames.Count).IsEqual(TarotMath.CardCount);
        AssertThat(TarotMath.CardNames[0]).IsEqual("The Fool");
        AssertThat(TarotMath.CardNames[21]).IsEqual("The World");
        AssertThat(TarotMath.CardNames[22]).IsEqual("The Fool, Reversed");
        AssertThat(TarotMath.CardNames[43]).IsEqual("The World, Reversed");
    }

    [TestCase]
    public void Score_JoinsTheHighAndLowParts()
    {
        AssertThat(PlannerGame.Score([0, 0, 0, 0, 1, 44])).IsEqual(300);
    }

    [TestCase]
    public void From_CopiesThePlayersStats()
    {
        var player = new PlayerState();
        player.Attributes[Strength][0] = 40;
        player.Skills[SwordSkill][0] = 12;

        var copied = PlannerGame.From(
            player,
            new Terminology(),
            new StatPlan(),
            new PlannerChoices(),
            new BuffWatch(player),
            false);

        AssertThat(copied.Attributes[Strength][0]).IsEqual(40);
        AssertThat(copied.Skills[SwordSkill][0]).IsEqual(12);
        AssertThat(copied.SkillOrder.Count).IsEqual(Terminology.SkillCount);
        AssertThat(copied.MetaStats).IsEmpty();
        AssertThat(copied.MetaFormats).IsEmpty();
        AssertThat(copied.TreeNodeNames[0]).IsEqual("p01");
    }

    private static int TreeBit(int node) => 1 << (12 - node + 4);

    private static int FirstIndex<TEntry>(IReadOnlyList<TEntry> entries, Func<TEntry, bool> matches) =>
        entries.Select((entry, index) => (entry, index)).First(pair => matches(pair.entry)).index;

    internal static PlannerGame Game()
    {
        var game = new PlannerGame { Points = 1_000_000, Kindred = 1 };
        foreach (var values in game.Attributes)
        {
            int[] attribute = [40, 0, 200, 3, 0, 50];
            attribute.CopyTo(values, 0);
        }

        foreach (var name in (string[])["Hit Score", "Parry Score"])
        {
            game.MetaStats[name] = name == "Hit Score" ? 100 : 90;
        }

        game.MetaStats["Spell Multiplier"] = 100;
        game.MetaStats["Spell Aptitude"] = 200;
        game.MetaStats["Spell Bonus"] = 0;
        game.Weapon = 30;
        game.Armor = 30;
        game.SkillTexts = Enumerable.Range(0, Terminology.SkillCount)
            .Select(number => new PlannerSkillText($"Skill {number}", [-1, -1, -1], 0))
            .ToArray();
        game.SkillTexts[BlessSkill] = new PlannerSkillText("Bless", [0, 1, 1], 1);
        int[] bless = [30, 0, 200, 3, 0, 60];
        bless.CopyTo(game.Skills[BlessSkill], 0);
        game.SkillOrder = [BlessSkill];
        game.Choices.SpellToggles = 1u << (int)BuffKind.Bless;
        return game;
    }
}
