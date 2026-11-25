using System;

namespace TheLastGate.Planner;

// A search can take most of a second, so it runs away from the caller, and again only when what it reads has
// changed. Null means its answer isn't ready yet.
public sealed class FocusCache(Action<Action> startSearch)
{
    private readonly FocusJob<BuffFocusInput, BuffFocusResult> buff = new(BuffFocus.Search, startSearch);
    private readonly FocusJob<CombatFocusInput, CombatFocusResult> combat = new(CombatFocus.Search, startSearch);

    public BuffFocusResult? Buff(BuffFocusInput input) => buff.For(input);

    public CombatFocusResult? Combat(CombatFocusInput input) => combat.For(input);

    public bool TakeFinished() => buff.TakeFinished() | combat.TakeFinished();
}
