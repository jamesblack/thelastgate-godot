using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.Buffs;
using TheLastGate.Rules;
using TheLastGate.State;

namespace TheLastGate.Planner;

public enum FocusTone
{
    Heading,
    Normal,
    SkillRaise,
    AttributeRaise,
    OtherRoute,
}

public sealed record FocusLine(string Text, string? Value, bool Indented, FocusTone Tone);

// The planner's Focus panel: the cheapest way to give the focused skill or combat stat +1, what that route buys, and
// what the routes that lost would cost.
public sealed class FocusPanel
{
    public const string Prompt = "Click a skill or combat stat to focus it";

    private const int ShownSkill = 1;

    public IReadOnlyList<FocusLine> Lines { get; private init; } = [];

    // Empty when there is nothing to plan.
    public IReadOnlyList<FocusRaise> Plan { get; private init; } = [];

    public int PlanCost { get; private init; }

    public bool Affordable { get; private init; }

    // The focus is on something that can't be focused now, so it should be dropped.
    public bool LostFocus { get; private init; }

    public static FocusPanel For(PlannerSheet sheet, PlannerFocus? focus, FocusCache cache)
    {
        if (focus?.Stat is { } stat)
        {
            return FocusInputs.Combat(sheet, stat) is { } input
                ? CombatPanel(sheet.Game, CombatModel.StatNames[(int)stat], cache.Combat(input))
                : Lost();
        }

        if (focus?.Skill is not { } skill)
        {
            return new FocusPanel { Lines = [PromptLine()] };
        }

        if (!Focusable(sheet.Game, skill)
            || sheet.Skills.FirstOrDefault(row => row.Slot == StatSlots.Skill(skill)) is not { } row)
        {
            return Lost();
        }

        if (sheet.PlannedInput is { } model
            && (FocusInputs.SpellShown(model, BuffKind.Bless) || ModelEffectsPresent(model.Effects, skill)))
        {
            return BuffedPanel(sheet, skill, cache);
        }

        return PlainPanel(sheet, skill, row);
    }

    private static FocusLine PromptLine() => new(Prompt, null, false, FocusTone.Normal);

    private static FocusPanel Lost() => new() { Lines = [PromptLine()], LostFocus = true };

    // Only learned skills on the list can be focused.
    private static bool Focusable(PlannerGame game, int skill) =>
        skill >= 0 && skill < game.SkillTexts.Length && game.SkillTexts[skill].Show == ShownSkill
        && game.Skills[skill][0] > 0;

    // The plain divisor model can't follow percent effects, the highest-attribute boosts or Armor Value in a skill.
    private static bool ModelEffectsPresent(PlannerEffects effects, int skill) =>
        Enum.GetValues<AttributeSlot>()
            .Any(attribute => effects.AttributePercent[attribute] != 0 || effects.SkillExtraPercent[attribute] != 0)
        || effects.ImmunityPercent != 0 || effects.ResistPercent != 0 || effects.ResistEnchantPercent != 0
        || effects.FistsOfHeavens != 0 || effects.GrandeGrimoire != 0
        || FocusInputs.ArmorFeeds(effects, skill);

    private static FocusLine Heading(string name, int value) =>
        new($"{name}  {value} -> {value + 1}", null, false, FocusTone.Heading);

    private static FocusLine CantBeRaised(string name) =>
        new($"{name} can't be raised further", null, false, FocusTone.Normal);

    private static FocusLine Cheapest(int cost) => new("Cheapest +1", $"{cost} exp", false, FocusTone.Normal);

    private static FocusLine OtherRoute(string label, int? cost) =>
        cost is { } total
            ? new FocusLine(label, $"{total} exp", false, FocusTone.OtherRoute)
            : new FocusLine($"{label} not available", null, false, FocusTone.OtherRoute);

    private static string RaiseName(PlannerGame game, int raiseIndex) => raiseIndex switch
    {
        < AttributeAmounts.Count => StatSlots.AttributeNames[raiseIndex],
        StatSlots.HitPoints => "Hitpoints",
        StatSlots.Mana => "Mana",
        _ => game.SkillTexts[raiseIndex - StatSlots.FirstSkill].Name,
    };

    private static IEnumerable<FocusLine> RouteLines(PlannerGame game, IEnumerable<FocusRaise> raises) =>
        raises.Select(raise => new FocusLine(
            $"+{raise.Points} {RaiseName(game, raise.RaiseIndex)}",
            raise.Cost.ToString(),
            true,
            raise.RaiseIndex >= StatSlots.FirstSkill ? FocusTone.SkillRaise : FocusTone.AttributeRaise));

    private static FocusPanel WithRoute(PlannerGame game, IEnumerable<FocusLine> lines, FocusRoute route) => new()
    {
        Lines = lines.ToArray(),
        Plan = route.Raises,
        PlanCost = route.TotalCost,
        Affordable = route.TotalCost <= game.Points - game.Plan.PointsUsed,
    };

    private static FocusPanel PlainPanel(PlannerSheet sheet, int skill, PlannerRow row)
    {
        var game = sheet.Game;
        var name = game.SkillTexts[skill].Name;
        var suggestion = SkillFocus.Suggest(FocusInputs.Plain(sheet, skill));
        if (!suggestion.AnyAvailable)
        {
            return new FocusPanel { Lines = [Heading(name, row.Preview), CantBeRaised(name)] };
        }

        var route = suggestion.UseDirect
            ? new FocusRoute(suggestion.Direct!.TotalCost, [new FocusRaise(row.Slot, 1, suggestion.Direct.TotalCost)])
            : new FocusRoute(
                suggestion.Attributes!.TotalCost,
                suggestion.Attributes.Raises
                    .Select(raise => new FocusRaise((int)raise.Attribute, raise.Points, raise.Cost))
                    .ToArray());
        var other = suggestion.UseDirect
            ? OtherRoute("Through attributes", suggestion.Attributes?.TotalCost)
            : OtherRoute($"Direct (+1 {name})", suggestion.Direct?.TotalCost);
        return WithRoute(
            game,
            [Heading(name, row.Preview), Cheapest(route.TotalCost), .. RouteLines(game, route.Raises), other],
            route);
    }

    private static FocusPanel BuffedPanel(PlannerSheet sheet, int skill, FocusCache cache)
    {
        var game = sheet.Game;
        var input = FocusInputs.Buffed(sheet, skill)!;
        var result = cache.Buff(input);
        var name = game.SkillTexts[skill].Name;
        if (FocusInputs.SplitPool(game, result.Best) is not { } best)
        {
            return new FocusPanel { Lines = [Heading(name, result.CurrentValue), CantBeRaised(name)] };
        }

        var blessSlot = input.SpellRaiseIndex[(int)BuffKind.Bless];
        var focusedIsBless = blessSlot == input.FocusedRaiseIndex;
        var kind = KindOf(best, input.FocusedRaiseIndex, focusedIsBless ? BuffFocus.NoSlot : blessSlot);
        var lines = new List<FocusLine>
        {
            Heading(name, result.CurrentValue),
            Cheapest(best.TotalCost),
        };
        lines.AddRange(RouteLines(game, best.Raises));

        // One line for each kind of route that didn't win.
        if (kind != RouteKind.Direct)
        {
            lines.Add(OtherRoute($"Direct (+1 {name})", result.Direct?.TotalCost));
        }

        if (kind != RouteKind.Attributes)
        {
            lines.Add(OtherRoute("Through attributes", result.ThroughAttributes?.TotalCost));
        }

        if (kind != RouteKind.Bless && !focusedIsBless && FocusInputs.SpellShown(input.Model, BuffKind.Bless))
        {
            lines.Add(OtherRoute("Through Bless", result.ThroughBless?.TotalCost));
        }

        return WithRoute(game, lines, best);
    }

    private static RouteKind KindOf(FocusRoute route, int focusedSlot, int blessSlot)
    {
        if (blessSlot >= 0 && route.Raises.Any(raise => raise.RaiseIndex == blessSlot))
        {
            return RouteKind.Bless;
        }

        return route.Raises.Any(raise => raise.RaiseIndex == focusedSlot) ? RouteKind.Direct : RouteKind.Attributes;
    }

    private static FocusPanel CombatPanel(PlannerGame game, string name, CombatFocusResult result)
    {
        if (FocusInputs.SplitPool(game, result.Best) is not { } best)
        {
            return new FocusPanel { Lines = [Heading(name, result.CurrentValue), CantBeRaised(name)] };
        }

        return WithRoute(
            game,
            [Heading(name, result.CurrentValue), Cheapest(best.TotalCost), .. RouteLines(game, best.Raises)],
            best);
    }

    private enum RouteKind
    {
        Direct,
        Attributes,
        Bless,
    }
}
