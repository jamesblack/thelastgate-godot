using TheLastGate.Assets;
using TheLastGate.State;

namespace TheLastGate.Stats;

public static class StatLines
{
    private const int BaseValue = 0;
    private const int ScoreHigh = 4;
    private const int ScoreLow = 5;
    private const int Maximum = 5;
    private const int NotLearntValue = 300;

    public static StatLine Attribute(int attribute, int[] values, StatPlan plan, int points) =>
        Raisable(StatSlots.AttributeNames[attribute], attribute, values, Score(values), plan, points);

    public static StatLine HitPoints(int[] values, StatPlan plan, int points) =>
        Raisable("Hitpoints", StatSlots.HitPoints, values, values[Maximum], plan, points);

    public static StatLine Mana(int[] values, StatPlan plan, int points) =>
        Raisable("Mana", StatSlots.Mana, values, values[Maximum], plan, points);

    public static StatLine Skill(int number, string name, int show, int[] values, StatPlan plan, int points) =>
        show switch
        {
            < SkillShows.Known => StatLine.Unknown,
            SkillShows.Red => new StatLine(name, TextColour.Red, null, Score(values), false, null),
            SkillShows.NotLearnt => new StatLine(name, TextColour.Orange, null, NotLearntValue, false, null),
            _ => Raisable(name, StatSlots.Skill(number), values, Score(values), plan, points),
        };

    private static StatLine Raisable(string name, int slot, int[] values, int current, StatPlan plan, int points)
    {
        var planned = plan.Raised(slot);
        var value = values[BaseValue] + planned;
        var cost = plan.NextCost(slot, values);
        return new StatLine(
            name,
            TextColour.Yellow,
            value > 0 ? value : null,
            current + planned,
            cost <= points - plan.PointsUsed,
            cost,
            planned > 0);
    }

    private static int Score(int[] values) => (values[ScoreHigh] << 8) | values[ScoreLow];
}
