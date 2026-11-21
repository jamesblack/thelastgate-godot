using GdUnit4;
using TheLastGate.Buffs;
using TheLastGate.Planner;
using TheLastGate.Rules;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Planner;

[TestSuite]
public class AdvancedSheetTests
{
    private const uint SeyanDu = 1u << 1;
    private PlannerChoices choices = new();
    private PlannerGame game = null!;

    [BeforeTest]
    public void StartAGame() => (choices, game) = (new PlannerChoices(), PlannerSheetTests.Game());

    [TestCase]
    public void Spells_ListOnlyTheSpellsYouKnow()
    {
        var spells = Sheet().Spells;
        AssertThat(spells).HasSize(1);
        var spell = spells[0];

        AssertThat(spell).IsEqual(new AdvancedSpell(BuffKind.Bless, "Bless", true, null));
    }

    [TestCase]
    public void ASpellOnYouWaitsForItsReading()
    {
        game.BuffActive[(int)BuffKind.Bless] = true;

        AssertThat(Sheet().Spells.Single())
            .IsEqual(new AdvancedSpell(BuffKind.Bless, "Bless (reading buffs...)", false, null));
    }

    [TestCase]
    public void WithoutTheSpellStats_NoSpellCanBeToggled()
    {
        game.MetaStats.Clear();

        AssertThat(Sheet().Spells.Single().Ready).IsFalse();
    }

    [TestCase]
    public void AWeakerCastOfYourOwn_WarnsThatItWontReplaceTheActiveBuff()
    {
        game.BuffActive[(int)BuffKind.Bless] = true;
        game.BuffReadings[(int)BuffKind.Bless] = new BuffReading { Known = true, Power = 90 };

        AssertThat(Sheet().Spells.Single().Warning)
            .IsEqual("Your Bless (57) won't replace the active one (90)");
    }

    [TestCase]
    public void AStrongerCastOrASpellToggledOff_HasNoWarning()
    {
        game.BuffActive[(int)BuffKind.Bless] = true;
        game.BuffReadings[(int)BuffKind.Bless] = new BuffReading { Known = true, Power = 40 };
        AssertThat(Sheet().Spells.Single().Warning).IsNull();

        game.BuffReadings[(int)BuffKind.Bless] = new BuffReading { Known = true, Power = 90 };
        game.Choices.SpellToggles = 0;
        AssertThat(Sheet().Spells.Single().Warning).IsNull();
    }

    [TestCase]
    public void TreeRows_AreTheLearnedNodesInTheMathAndTheCorruptors()
    {
        game.Kindred = SeyanDu;
        game.TreePoints = TreeBit(5) | TreeBit(6) | TreeBit(7) | TreeBit(8) | TreeBit(9);
        game.TreeNodeNames[4] = "Node Five";
        game.TreeNodes[5] = 20;
        game.TreeNodes[6] = 66;
        game.TreeNodes[7] = 250;
        game.TreeNodes[8] = 20;

        var sheet = Sheet();

        AssertThat(sheet.TreeRows)
            .ContainsExactly("Node Five", "False Strength", "False Tactician (not modelled)", "False Strength");
        AssertThat(sheet.TreeCopies[(int)Corruptor.FalseStrength]).IsEqual(2);
        AssertThat(sheet.TreeCopies[(int)Corruptor.FalseTactician]).IsEqual(1);
        AssertThat(sheet.TreeCopies[(int)Corruptor.FalseAgility]).IsEqual(0);
    }

    [TestCase(TarotCard.Magician, "I The Magician")]
    [TestCase(TarotCard.FoolReversed, "0 The Fool, Reversed")]
    [TestCase(TarotCard.MoonReversed, "XVIII The Moon, Reversed")]
    public void CardLabels_StartWithTheCardsNumeral(TarotCard card, string label)
    {
        AssertThat(AdvancedSheet.CardLabel(card)).IsEqual(label);
    }

    [TestCase]
    public void EffectTexts_SayWhenTheModelLeavesSomethingOut()
    {
        var unmodelledCorruptor = TreeEffects.Corruptors.ToList().FindIndex(info => !info.Modelled);
        var unmodelledGear = ItemEffects.Gears.ToList().FindIndex(info => !info.Modelled);

        AssertThat(AdvancedSheet.CorruptorEffect((int)Corruptor.FalseStrength)).IsEqual("+3% Strength");
        AssertThat(AdvancedSheet.CorruptorEffect(unmodelledCorruptor)).IsEqual("(not modelled)");
        AssertThat(AdvancedSheet.GearEffect(0)).IsEqual("+4% Wil & Int");
        AssertThat(AdvancedSheet.GearEffect(unmodelledGear))
            .IsEqual($"{ItemEffects.Gears[unmodelledGear].Effect} (not modelled)");
        AssertThat(AdvancedSheet.TreeCopiesText(2)).IsEqual("+2 tree");
        AssertThat(AdvancedSheet.TreeCopiesText(0)).IsEqual("");
    }

    [TestCase]
    public void ToggleSpellAndCard_FlipTheirBits()
    {
        choices.ToggleSpell(BuffKind.Protect);
        choices.ToggleCard(TarotCard.Hermit);
        AssertThat(choices.SpellToggles).IsEqual(1u << (int)BuffKind.Protect);
        AssertThat(choices.TarotCards).IsEqual(1ul << (int)TarotCard.Hermit);

        choices.ToggleSpell(BuffKind.Protect);
        choices.ToggleCard(TarotCard.Hermit);
        AssertThat((choices.SpellToggles, choices.TarotCards)).IsEqual((0u, 0ul));
    }

    [TestCase]
    public void TickingACorruptor_GivesItACopyAndUntickingKeepsTheCount()
    {
        choices.ToggleCorruptor(3);
        AssertThat((choices.CorruptorTicks, choices.CorruptorCounts[3])).IsEqual((1u << 3, 1));

        choices.SetCorruptorCount(3, 4);
        choices.ToggleCorruptor(3);
        AssertThat((choices.CorruptorTicks, choices.CorruptorCounts[3])).IsEqual((0u, 4));

        choices.ToggleCorruptor(3);
        AssertThat(choices.CorruptorCounts[3]).IsEqual(4);
    }

    [TestCase]
    public void TickingAnEnchant_GivesACountedOneACopy()
    {
        var uncounted = ItemEffects.Enchants.ToList().FindIndex(info => !info.Counted);

        choices.ToggleEnchant((int)Enchant.MoreBraveness);
        choices.ToggleEnchant(uncounted);

        AssertThat(choices.EnchantCounts[(int)Enchant.MoreBraveness]).IsEqual(1);
        AssertThat(choices.EnchantCounts[uncounted]).IsEqual(0);
        AssertThat(choices.EnchantTicks).IsEqual((1u << (int)Enchant.MoreBraveness) | (1u << uncounted));
    }

    [TestCase(0, 1)]
    [TestCase(7, 7)]
    [TestCase(25, 20)]
    public void Counts_StayFromOneToTwenty(int typed, int kept)
    {
        choices.SetCorruptorCount(0, typed);
        choices.SetEnchantCount(0, typed);

        AssertThat((choices.CorruptorCounts[0], choices.EnchantCounts[0])).IsEqual((kept, kept));
    }

    [TestCase]
    public void ToggleGear_FlipsItsBit()
    {
        choices.ToggleGear(20);
        AssertThat(choices.GearTicks).IsEqual(1u << 20);

        choices.ToggleGear(20);
        AssertThat(choices.GearTicks).IsEqual(0u);
    }

    private static int TreeBit(int node) => 1 << (12 - node + 4);

    private AdvancedSheet Sheet() => AdvancedSheet.For(PlannerSheet.For(game));
}
