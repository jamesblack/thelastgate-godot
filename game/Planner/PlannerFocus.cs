namespace TheLastGate.Planner;

// What the planner's Focus panel works on: a skill, by number, or a combat stat.
public sealed record PlannerFocus(int? Skill, CombatStat? Stat)
{
    public static PlannerFocus OnSkill(int number) => new(number, null);

    public static PlannerFocus OnStat(CombatStat stat) => new(null, stat);
}
