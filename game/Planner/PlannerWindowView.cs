using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Planner;

// The skill planner window, at its fixed place on the screen: the points left and planned, Commit, Reset and Close,
// the stat rows and the skills list. The HUD tells it what to show and does what its buttons ask.
[Tool]
public partial class PlannerWindowView : Control
{
    private const string StatRowScenePath = "res://Planner/planner_stat_row.tscn";
    private const string FocusLineScenePath = "res://Planner/planner_focus_line.tscn";
    private const int PreviewSkillRows = 12;
    private const int MathLineHeight = 14;

    private const string FistsTooltip = "Fists of the Heavens uses your best weapon skill";

    private static readonly string[] StatRowNames =
        ["Braveness", "Willpower", "Intuition", "Agility", "Strength", "Hitpoints", "Mana"];

    private static readonly string[] CombatRowNames = ["HitScore", "ParryScore", "WeaponValue", "ArmorValue"];

    private static readonly FocusLine[] PreviewFocusLines =
    [
        new("Sword  80 -> 81", null, false, FocusTone.Heading),
        new("Cheapest +1", "4800 exp", false, FocusTone.Normal),
        new("+1 Sword", "4800", true, FocusTone.SkillRaise),
        new("Through attributes", "49476 exp", false, FocusTone.OtherRoute),
    ];

    private PackedScene? focusLineScene;

    private PackedScene? statRowScene;

    private Container SkillRows => GetNode<Container>("%SkillRows");

    private ProceduralPicker WeaponChoice => GetNode<ProceduralPicker>("%Weapon");

    private ProceduralPicker OffhandChoice => GetNode<ProceduralPicker>("%Offhand");

    private Container MathLines => GetNode<Container>("%MathLines");

    private Container FocusLines => GetNode<Container>("%FocusLines");

    private ProceduralButton PlanIt => GetNode<ProceduralButton>("%PlanIt");

    public event Action? CommitPressed;

    public event Action? ResetPressed;

    public event Action? ClosePressed;

    // A row's stat plan slot, and true to raise it or false to lower it.
    public event Action<int, bool>? RowClicked;

    public event Action<CombatWeapon>? WeaponChosen;

    public event Action<CombatOffhand>? OffhandChosen;

    public event Action<PlannerFocus>? FocusClicked;

    public event Action? PlanItPressed;

    public event Action? AdvancedPressed;

    public override void _Ready()
    {
        foreach (var scroll in (string[])["%MathScroll", "%SkillScroll"])
        {
            ProceduralScrollBar.StyleScrollContainer(GetNode<ScrollContainer>(scroll));
        }

        statRowScene = GD.Load<PackedScene>(StatRowScenePath);
        focusLineScene = GD.Load<PackedScene>(FocusLineScenePath);
        if (Engine.IsEditorHint())
        {
            EnsureSkillRows(PreviewSkillRows);
            ShowFocusLines(PreviewFocusLines);
            return;
        }

        WindowOverlays.Lift(this);

        Visible = false;
        foreach (var stat in Enum.GetValues<CombatStat>())
        {
            GetNode<PlannerValueRow>("%" + CombatRowNames[(int)stat]).NameClicked +=
                () => FocusClicked?.Invoke(PlannerFocus.OnStat(stat));
        }

        foreach (var name in StatRowNames)
        {
            GetNode<PlannerStatRow>("%" + name).GlyphClicked += (slot, raise) => RowClicked?.Invoke(slot, raise);
        }

        WeaponChoice.SetChoices(CombatModel.WeaponNames);
        OffhandChoice.SetChoices(CombatModel.OffhandNames);
        WeaponChoice.ItemChosen += index => WeaponChosen?.Invoke((CombatWeapon)index);
        OffhandChoice.ItemChosen += index => OffhandChosen?.Invoke((CombatOffhand)index);
        GetNode<ProceduralButton>("%Commit").Pressed += () => CommitPressed?.Invoke();
        GetNode<ProceduralButton>("%Reset").Pressed += () => ResetPressed?.Invoke();
        GetNode<ClickArea>("%Close").Released += _ => ClosePressed?.Invoke();
        PlanIt.Pressed += () => PlanItPressed?.Invoke();
        GetNode<ProceduralButton>("%Advanced").Pressed += () => AdvancedPressed?.Invoke();
    }

    // The gold outline marks the focused skill or combat stat.
    public void ShowFocus(PlannerFocus? focus, FocusPanel panel)
    {
        var focusedSlot = focus?.Skill is { } skill ? StatSlots.Skill(skill) : (int?)null;
        foreach (var row in SkillRows.GetChildren().Cast<PlannerStatRow>())
        {
            row.ShowOutline(row.Visible && row.Slot == focusedSlot);
        }

        foreach (var stat in Enum.GetValues<CombatStat>())
        {
            GetNode<PlannerValueRow>("%" + CombatRowNames[(int)stat]).ShowOutline(focus?.Stat == stat);
        }

        ShowFocusLines(panel.Lines);
        PlanIt.Visible = panel.Plan.Count > 0 || panel.Working;
        PlanIt.Disabled = panel.Working || !panel.Affordable;
    }

    private void ShowFocusLines(IReadOnlyList<FocusLine> lines)
    {
        var focusLines = FocusLines;
        while (focusLines.GetChildCount() < lines.Count && focusLineScene != null)
        {
            focusLines.AddChild(focusLineScene.Instantiate());
        }

        var shownLines = focusLines.GetChildren().OfType<PlannerFocusLine>().ToArray();
        for (var index = 0; index < shownLines.Length; index++)
        {
            shownLines[index].Visible = index < lines.Count;
            if (index < lines.Count)
            {
                shownLines[index].Show(lines[index]);
            }
        }
    }

    public void ShowWindow(bool shown, int pointsLeft, int planned, PlannerSheet? sheet)
    {
        Visible = shown;
        GetNode<Label>("%PointsLeft").Text = $"Exp left {pointsLeft}";
        GetNode<Label>("%Planned").Text = $"Planned {planned}";
        if (sheet == null)
        {
            return;
        }

        var statRows = sheet.Attributes.Concat(sheet.Vitals).ToArray();
        for (var index = 0; index < StatRowNames.Length; index++)
        {
            GetNode<PlannerStatRow>("%" + StatRowNames[index]).Show(statRows[index]);
        }

        ShowSkills(sheet.Skills);
        ShowCombat(sheet);
        ShowMathLines(sheet.MathLines);
    }

    // Until Hit, Parry and the spell stats have arrived the block shows the server's numbers alone.
    private void ShowCombat(PlannerSheet sheet)
    {
        var combatRows = CombatRowNames.Select(name => GetNode<PlannerValueRow>("%" + name)).ToArray();
        var multiplier = GetNode<PlannerValueRow>("%SpellMultiplier");
        var aptitude = GetNode<PlannerValueRow>("%SpellAptitude");
        var heading = GetNode<RuledHeading>("%CombatHeading");
        multiplier.Show(PlannerSheet.SpellMultiplier, sheet.MetaText(PlannerSheet.SpellMultiplier));
        if (sheet is not { Combat: { } combat, CombatShown: { } shown, PlannedInput: { } input, Planned: { } planned })
        {
            heading.Text = "Combat (current)";
            GetNode<CanvasItem>("%Equipment").Visible = false;
            combatRows[0].Show(PlannerSheet.HitScore, sheet.MetaText(PlannerSheet.HitScore));
            combatRows[1].Show(PlannerSheet.ParryScore, sheet.MetaText(PlannerSheet.ParryScore));
            combatRows[2].Show(CombatModel.StatNames[(int)CombatStat.Weapon], sheet.ServerWeapon.ToString());
            combatRows[3].Show(CombatModel.StatNames[(int)CombatStat.Armor], sheet.ServerArmor.ToString());
            foreach (var row in combatRows)
            {
                row.AllowFocus(false);
            }

            aptitude.Show(PlannerSheet.SpellAptitude, sheet.MetaText(PlannerSheet.SpellAptitude));
            return;
        }

        heading.Text = sheet.CombatChanged ? "Combat" : "Combat (current)";
        GetNode<CanvasItem>("%Equipment").Visible = true;
        WeaponChoice.Select((int)combat.Weapon);
        OffhandChoice.Select((int)combat.Offhand);
        WeaponChoice.Disabled = combat.BestWeaponSkill;
        WeaponChoice.Tooltip = combat.BestWeaponSkill ? FistsTooltip : "";
        var server = new CombatValues(combat.ServerHit, combat.ServerParry, sheet.ServerWeapon, sheet.ServerArmor);
        foreach (var stat in Enum.GetValues<CombatStat>())
        {
            combatRows[(int)stat].ShowPlanned(CombatModel.StatNames[(int)stat], server[stat], shown[stat]);
            combatRows[(int)stat].AllowFocus(true);
        }

        aptitude.ShowPlanned(PlannerSheet.SpellAptitude, input.SpellAptitude, planned.SpellAptitude);
    }

    private void ShowMathLines(IReadOnlyList<string> lines)
    {
        var mathLines = MathLines;
        while (mathLines.GetChildCount() < lines.Count)
        {
            mathLines.AddChild(
                new HudLabel
                {
                    CustomMinimumSize = new Vector2(0, MathLineHeight),
                    VerticalAlignment = VerticalAlignment.Center,
                    MouseFilter = MouseFilterEnum.Ignore,
                });
        }

        var labels = mathLines.GetChildren().Cast<Label>().ToArray();
        for (var index = 0; index < labels.Length; index++)
        {
            labels[index].Visible = index < lines.Count;
            labels[index].Text = index < lines.Count ? lines[index] : "";
        }
    }

    private void ShowSkills(IReadOnlyList<PlannerRow> skills)
    {
        EnsureSkillRows(skills.Count);
        var rows = SkillRows.GetChildren().Cast<PlannerStatRow>().ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            rows[index].Visible = index < skills.Count;
            if (index < skills.Count)
            {
                rows[index].Show(skills[index]);
            }
        }
    }

    private void EnsureSkillRows(int count)
    {
        var skillRows = SkillRows;
        while (skillRows.GetChildCount() < count && statRowScene != null)
        {
            var row = statRowScene.Instantiate();
            if (row is PlannerStatRow statRow)
            {
                statRow.GlyphClicked += (slot, raise) => RowClicked?.Invoke(slot, raise);
                statRow.NameClicked += slot =>
                    FocusClicked?.Invoke(PlannerFocus.OnSkill(slot - StatSlots.FirstSkill));
            }

            skillRows.AddChild(row);
        }
    }
}
