namespace TheLastGate.Planner;

// A search that finds nothing can take most of a second, so each one runs again only when what it reads has changed.
public sealed class FocusCache
{
    private (BuffFocusInput Input, BuffFocusResult Result)? buff;
    private (CombatFocusInput Input, CombatFocusResult Result)? combat;

    public BuffFocusResult Buff(BuffFocusInput input)
    {
        if (buff is not { } cached || !cached.Input.Equals(input))
        {
            buff = cached = (input, BuffFocus.Search(input));
        }

        return cached.Result;
    }

    public CombatFocusResult Combat(CombatFocusInput input)
    {
        if (combat is not { } cached || !cached.Input.Equals(input))
        {
            combat = cached = (input, CombatFocus.Search(input));
        }

        return cached.Result;
    }
}
