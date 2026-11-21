using System.Linq;
using Godot;
using TheLastGate.State;
using TheLastGate.Stats;

namespace TheLastGate.Hud;

public partial class SkillList : VBoxContainer
{
    private ScrollBar? scrollBar;

    [Export] public NodePath ScrollBarPath { get; set; } = new();

    public StatPanel Panel { get; } = new();

    public override void _Ready()
    {
        if (!ScrollBarPath.IsEmpty)
        {
            scrollBar = GetNode<ScrollBar>(ScrollBarPath);
            ShowScroll();
            scrollBar.ValueChanged += value => Panel.ScrollTo((int)value);
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton click)
        {
            return;
        }

        AcceptEvent();
        if (click.Pressed && click.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            Panel.ScrollBy(click.ButtonIndex == MouseButton.WheelUp ? -1 : 1);
            scrollBar?.SetValueNoSignal(Panel.Row);
        }
    }

    public void Choose(StatListMode button)
    {
        Panel.Choose(button);
        ShowScroll();
    }

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
            GetChild<StatRow>(row).Show(line, StatSlots.Skill(number), showBase);
        }
    }

    private void ShowMeta(Terminology terminology)
    {
        for (var row = 0; row < SkillScroll.VisibleRows; row++)
        {
            var meta = Panel.BottomMeta(row)!.Value;
            GetChild<StatRow>(row).ShowMeta(MetaLines.For(terminology.MetaStats[meta]), meta);
        }
    }

    // Zeroed first so the new maximum never clamps the old list's row and scrolls the new list with it.
    private void ShowScroll()
    {
        if (scrollBar == null)
        {
            return;
        }

        scrollBar.SetValueNoSignal(0);
        scrollBar.MaxValue = Panel.LastRow;
        scrollBar.SetValueNoSignal(Panel.Row);
    }
}
