using System;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Hud;

public sealed class LookPanel
{
    public static readonly TimeSpan ShowFor = TimeSpan.FromSeconds(10);

    private TimeSpan shownFor;

    public CharacterLook? Shown { get; private set; }

    public static SpriteTint PortraitTint(LookFlags flags) =>
        (flags.HasFlag(LookFlags.Shadow) ? SpriteTint.Grey : SpriteTint.None)
        | (flags.HasFlag(LookFlags.Bloody) ? SpriteTint.Bloody : SpriteTint.None);

    public void Show(CharacterLook look)
    {
        Shown = look;
        shownFor = TimeSpan.Zero;
    }

    public void Hide() => Shown = null;

    public void Advance(TimeSpan elapsed)
    {
        shownFor += elapsed;
        if (shownFor >= ShowFor)
        {
            Shown = null;
        }
    }
}
