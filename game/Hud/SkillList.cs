using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Hud;

public partial class SkillList : Control
{
    public StatPanel Panel { get; } = new();

    public IEnumerable<StatRow> Rows => GetNode("%Rows").GetChildren().Cast<StatRow>();

    public ClickArea UpdateButton => GetNode<ClickArea>("%UpdateButton");

    public ClickArea PlannerButton => GetNode<ClickArea>("%PlannerButton");

    private ScrollBar ScrollBar => GetNode<ScrollBar>("%ScrollBar");

    public override void _Ready()
    {
        ShowScroll();
        ScrollBar.ValueChanged += value => Panel.ScrollTo((int)value);
        GetNode<Control>("%Frame").GuiInput += ScrollByWheel;
    }

    public void Choose(StatListMode button)
    {
        Panel.Choose(button);
        ShowScroll();
    }

    public void ShowUpdate(bool shown, int points)
    {
        GetNode<CanvasItem>("%UpdateFooter").Visible = shown;
        GetNode<Label>("%UpdatePoints").Text = points.ToString(CultureInfo.InvariantCulture);
    }

    public void ShowPlannerButton(bool shown) => GetNode<CanvasItem>("%PlannerFooter").Visible = shown;

    public void Show(PlayerState player, Terminology terminology, StatPlan plan, bool showBase)
    {
        if (Panel.ShowsMetaStats)
        {
            ShowMeta(terminology);
            return;
        }

        var order = SkillOrder.Sorted(
            terminology.Skills.Select((text, number) =>
                new SkillEntry(number, text.Name, text.SortKey, player.Skills[number][0] != 0)),
            player.Kindred);
        for (var row = 0; row < SkillScroll.VisibleRows; row++)
        {
            var number = order[Panel.Skills.PositionShownIn(row)];
            var text = terminology.Skills[number];
            var line = StatLines.Skill(number, text.Name, text.Show, player.Skills[number], plan, player.Points);
            Row(row).Show(line, StatSlots.Skill(number), showBase);
        }
    }

    private StatRow Row(int row) => GetNode("%Rows").GetChild<StatRow>(row);

    private void ScrollByWheel(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton click)
        {
            return;
        }

        AcceptEvent();
        if (click.Pressed && click.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            Panel.ScrollBy(click.ButtonIndex == MouseButton.WheelUp ? -1 : 1);
            ScrollBar.SetValueNoSignal(Panel.Row);
        }
    }

    private void ShowMeta(Terminology terminology)
    {
        for (var row = 0; row < SkillScroll.VisibleRows; row++)
        {
            var meta = Panel.BottomMeta(row)!.Value;
            Row(row).ShowMeta(MetaLines.For(terminology.MetaStats[meta]), meta);
        }
    }

    // Zeroed first so the new maximum never clamps the old list's row and scrolls the new list with it.
    private void ShowScroll()
    {
        ScrollBar.SetValueNoSignal(0);
        ScrollBar.MaxValue = Panel.LastRow;
        ScrollBar.SetValueNoSignal(Panel.Row);
    }
}
