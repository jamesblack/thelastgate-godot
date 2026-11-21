using System;
using System.Diagnostics;
using TheLastGate.State;

namespace TheLastGate.Stats;

public sealed class StatPanel
{
    public const int TopRows = 7;
    private const int FirstDefenseMeta = 48;

    public static int LastMetaRow => (Terminology.MetaStatCount - TopRows) / 2 - SkillScroll.VisibleRows;

    public StatListMode Mode { get; private set; }

    public SkillScroll Skills { get; } = new();

    public int MetaRow { get; private set; }

    public bool ShowsMetaStats => Mode is StatListMode.Offenses or StatListMode.Defenses;

    public bool ShowsMetaOnTop => Mode != StatListMode.Skills;

    public int Row => ShowsMetaStats ? MetaRow : Skills.Row;

    public int LastRow => ShowsMetaStats ? LastMetaRow : SkillScroll.LastRow;

    public static string Help(StatListMode button) => button switch
    {
        StatListMode.Skills or StatListMode.SkillsAndMeta =>
            "Makes the left panel display skill scores and experience points (default).",
        StatListMode.Offenses =>
            "Makes the left panel display Offense-related statistics, such as attack damage and critical hit rate.",
        StatListMode.Defenses =>
            "Makes the left panel display Defense-related statistics, such as eHP and regeneration rates.",
        _ => throw new UnreachableException(),
    };

    public void Choose(StatListMode button) =>
        Mode = button == StatListMode.Skills && Mode == StatListMode.Skills ? StatListMode.SkillsAndMeta : button;

    public bool IsMarked(StatListMode button) =>
        button == (Mode == StatListMode.SkillsAndMeta ? StatListMode.Skills : Mode);

    public void ScrollBy(int rows) => ScrollTo(Row + rows);

    public void ScrollTo(int row)
    {
        if (ShowsMetaStats)
        {
            MetaRow = Math.Clamp(row, 0, LastMetaRow);
        }
        else
        {
            Skills.ScrollTo(row);
        }
    }

    public int? TopMeta(int row) => ShowsMetaOnTop ? row : null;

    public int? BottomMeta(int row) => Mode switch
    {
        StatListMode.Skills or StatListMode.SkillsAndMeta => null,
        StatListMode.Offenses => TopRows + row + MetaRow,
        StatListMode.Defenses => FirstDefenseMeta + row + MetaRow,
        _ => throw new UnreachableException(),
    };
}
