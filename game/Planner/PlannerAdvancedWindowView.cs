using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheLastGate.Buffs;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Rules;

namespace TheLastGate.Planner;

// The skill planner's Advanced window: which spells, tarot cards, corruptors, enchantments and gear the planner's
// numbers count, and the skill tree's rows it counts already. The HUD tells it what to show and saves what is changed.
[Tool]
public partial class PlannerAdvancedWindowView : Control
{
    private const string RowScenePath = "res://Planner/planner_advanced_row.tscn";
    private const int CardsPerColumn = 5;

    private PackedScene? rowScene;

    private Container Spells => GetNode<Container>("%Spells");

    private Container TreeRows => GetNode<Container>("%TreeRows");

    public event Action? ClosePressed;

    // A change to the planner's choices, for the HUD to make and save.
    public event Action<Action<PlannerChoices>>? ChoiceChanged;

    public override void _Ready()
    {
        ProceduralScrollBar.StyleScrollContainer(GetNode<ScrollContainer>("%Scroll"));
        rowScene = GD.Load<PackedScene>(RowScenePath);
        AddSpellRows();
        AddCardRows();
        AddCorruptorRows();
        AddEnchantRows();
        AddGearRows();
        if (Engine.IsEditorHint())
        {
            ShowSpells([new AdvancedSpell(BuffKind.Bless, "Bless", true, null)], new PlannerChoices());
            ShowTreeRows(["False Strength"]);
            return;
        }

        Visible = false;
        GetNode<ClickArea>("%Close").Released += _ => ClosePressed?.Invoke();
    }

    public void ShowWindow(bool shown, PlannerChoices choices, AdvancedSheet sheet)
    {
        Visible = shown;
        if (!shown)
        {
            return;
        }

        ShowSpells(sheet.Spells, choices);
        var cards = Rows("%CardsLeft").Concat(Rows("%CardsRight")).ToArray();
        for (var index = 0; index < cards.Length; index++)
        {
            var card = TarotMath.ModelledCards[index];
            cards[index].Show(AdvancedSheet.CardLabel(card), TarotMath.Has(choices.TarotCards, card), true);
        }

        ShowTreeRows(sheet.TreeRows);
        var corruptors = Rows("%Corruptors");
        for (var corruptor = 0; corruptor < corruptors.Length; corruptor++)
        {
            var row = corruptors[corruptor];
            var ticked = (choices.CorruptorTicks & (1u << corruptor)) != 0;
            row.Show(TreeEffects.Corruptors[corruptor].Name, ticked, true);
            row.ShowCount(choices.CorruptorCounts[corruptor], ticked, AdvancedSheet.CorruptorCountHelp);
            row.ShowTree(AdvancedSheet.TreeCopiesText(sheet.TreeCopies[corruptor]));
        }

        var enchants = Rows("%Enchants");
        for (var enchant = 0; enchant < enchants.Length; enchant++)
        {
            var ticked = (choices.EnchantTicks & (1u << enchant)) != 0;
            enchants[enchant].Show(ItemEffects.Enchants[enchant].Name, ticked, true);
            if (ItemEffects.Enchants[enchant].Counted)
            {
                enchants[enchant].ShowCount(choices.EnchantCounts[enchant], ticked, AdvancedSheet.EnchantCountHelp);
            }
        }

        var gears = Rows("%Gear");
        for (var gear = 0; gear < gears.Length; gear++)
        {
            gears[gear].Show(ItemEffects.Gears[gear].Name, (choices.GearTicks & (1u << gear)) != 0, true);
        }
    }

    private PlannerAdvancedRow[] Rows(string path) =>
        GetNode<Container>(path).GetChildren().OfType<PlannerAdvancedRow>().ToArray();

    private PlannerAdvancedRow AddRow(Container parent, Action<PlannerChoices>? toggle)
    {
        var row = (PlannerAdvancedRow)rowScene!.Instantiate();
        parent.AddChild(row);
        if (toggle != null)
        {
            row.Toggled += () => ChoiceChanged?.Invoke(toggle);
        }

        return row;
    }

    // In C's order: the first five cards down the left column, the rest down the right.
    private void AddCardRows()
    {
        for (var index = 0; index < TarotMath.ModelledCards.Count; index++)
        {
            var card = TarotMath.ModelledCards[index];
            var column = GetNode<Container>(
                index < CardsPerColumn ? "%CardsLeft" : "%CardsRight");
            AddRow(column, choices => choices.ToggleCard(card)).Show(AdvancedSheet.CardLabel(card), false, true);
        }
    }

    private void AddCorruptorRows()
    {
        var parent = GetNode<Container>("%Corruptors");
        for (var corruptor = 0; corruptor < TreeEffects.CorruptorCount; corruptor++)
        {
            var index = corruptor;
            var row = AddRow(parent, choices => choices.ToggleCorruptor(index));
            row.CountChosen += copies => ChoiceChanged?.Invoke(choices => choices.SetCorruptorCount(index, copies));
            row.Show(TreeEffects.Corruptors[corruptor].Name, false, true);
            row.ShowEffect(AdvancedSheet.CorruptorEffect(corruptor));
            row.ShowCount(0, false, AdvancedSheet.CorruptorCountHelp);
        }
    }

    private void AddEnchantRows()
    {
        var parent = GetNode<Container>("%Enchants");
        for (var enchant = 0; enchant < ItemEffects.EnchantCount; enchant++)
        {
            var index = enchant;
            var info = ItemEffects.Enchants[enchant];
            var row = AddRow(parent, choices => choices.ToggleEnchant(index));
            row.Show(info.Name, false, true);
            row.ShowEffect(info.OneCopy);
            if (info.Counted)
            {
                row.CountChosen += copies => ChoiceChanged?.Invoke(choices => choices.SetEnchantCount(index, copies));
                row.ShowCount(0, false, AdvancedSheet.EnchantCountHelp);
            }
        }
    }

    private void AddGearRows()
    {
        var parent = GetNode<Container>("%Gear");
        for (var gear = 0; gear < ItemEffects.GearCount; gear++)
        {
            var index = gear;
            var row = AddRow(parent, choices => choices.ToggleGear(index));
            row.Show(ItemEffects.Gears[gear].Name, false, true);
            row.ShowEffect(AdvancedSheet.GearEffect(gear));
        }
    }

    // Each spell has its row and a line under it for a warning.
    private void AddSpellRows()
    {
        foreach (var kind in Enum.GetValues<BuffKind>())
        {
            AddRow(Spells, choices => choices.ToggleSpell(kind));
            AddRow(Spells, null);
        }
    }

    // Only the spells you know are shown. Hiding a row, even for a moment, drops a click on its tick, so each row's
    // visibility is set once.
    private void ShowSpells(IReadOnlyList<AdvancedSpell> spells, PlannerChoices choices)
    {
        var rows = Rows("%Spells");
        foreach (var kind in Enum.GetValues<BuffKind>())
        {
            var spell = spells.FirstOrDefault(known => known.Kind == kind);
            var spellRow = rows[2 * (int)kind];
            var noteRow = rows[2 * (int)kind + 1];
            spellRow.Visible = spell != null;
            noteRow.Visible = spell?.Warning != null;
            if (spell != null)
            {
                spellRow.Show(spell.Label, ((choices.SpellToggles >> (int)kind) & 1) != 0, spell.Ready);
                noteRow.ShowNote(spell.Warning ?? "");
            }
        }
    }

    // The tree's rows are always counted, so their ticks are set and can't be changed.
    private void ShowTreeRows(IReadOnlyList<string> names)
    {
        foreach (var name in (string[])["TreeGap", "TreeHeading", "TreeRows"])
        {
            GetNode<CanvasItem>("%" + name).Visible = names.Count > 0;
        }

        var treeRows = TreeRows;
        while (treeRows.GetChildCount() < names.Count)
        {
            AddRow(treeRows, null);
        }

        var rows = Rows("%TreeRows");
        for (var index = 0; index < rows.Length; index++)
        {
            rows[index].Visible = index < names.Count;
            if (index < names.Count)
            {
                rows[index].ShowAlwaysOn(names[index]);
                rows[index].ShowEffect(AdvancedSheet.TreeRowEffect);
            }
        }
    }
}
