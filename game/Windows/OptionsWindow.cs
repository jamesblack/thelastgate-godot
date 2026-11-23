using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheLastGate.Assets;
using TheLastGate.Hotkeys;
using TheLastGate.State;
using TheLastGate.World;

namespace TheLastGate.Windows;

public sealed record KeybindChange(IReadOnlyList<BindingChange> Changes, LogLine? Line);

public enum OptionsTab
{
    General,
    Audio,
    Keybinds,
    Display,
    Experimental,
}

// Nothing but its own button, its hotkey and Escape opens or closes it; the tab is remembered while it's shut.
// Leaving the tab or the window stops a key capture, so the keyboard is never left waiting on a hidden row.
public sealed class OptionsWindow
{
    private const string ChatCommandKey = "/";

    public bool IsOpen { get; private set; }

    public OptionsTab Tab { get; private set; }

    public string? Capturing { get; private set; }

    public bool ConfirmingReset { get; private set; }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        StopKeybindChoices();
    }

    public bool Close()
    {
        var wasOpen = IsOpen;
        IsOpen = false;
        StopKeybindChoices();
        return wasOpen;
    }

    public void Show(OptionsTab tab)
    {
        Tab = tab;
        StopKeybindChoices();
    }

    public void StartCapture(string id) => Capturing = id;

    // The slash stays free for chat commands, and a modifier alone only makes sense for a binding that is held.
    public KeybindChange? Capture(KeyChord chord, IReadOnlyDictionary<string, KeyChord?> current)
    {
        if (Capturing is not { } id || chord.Key == ChatCommandKey)
        {
            return null;
        }

        if (IsModifier(chord.Key))
        {
            if (!Bindings.All.First(binding => binding.Id == id).Hold)
            {
                return null;
            }

            chord = chord with { Modifiers = MapKeys.None };
        }

        Capturing = null;
        var reassignment = BindingRules.Assign(current, id, chord);
        if (reassignment.DisplacedId is not { } displaced)
        {
            return new KeybindChange(reassignment.Changes, null);
        }

        var name = Bindings.All.First(binding => binding.Id == displaced).Name;
        var line = new LogLine($"Unbound {name} ({chord.LongName}) - key reassigned.", (int)TextColour.Red);
        return new KeybindChange(reassignment.Changes, line);
    }

    public void CancelCapture() => Capturing = null;

    public BindingChange? UnbindCaptured()
    {
        var change = Capturing is { } id ? new BindingChange(id, null) : (BindingChange?)null;
        Capturing = null;
        return change;
    }

    public LogLine? ChooseReset()
    {
        if (!ConfirmingReset)
        {
            ConfirmingReset = true;
            return null;
        }

        ConfirmingReset = false;
        return new LogLine($"{ResetName(Tab)} been set to their defaults.", (int)TextColour.Green);
    }

    private static string ResetName(OptionsTab tab) => tab switch
    {
        OptionsTab.General => "General options have",
        OptionsTab.Audio => "Audio settings have",
        OptionsTab.Keybinds => "Keybinds have",
        OptionsTab.Display => "Display settings have",
        OptionsTab.Experimental => "Experimental options have",
        _ => throw new UnreachableException(),
    };

    private static bool IsModifier(string key) => key is KeyNames.Shift or KeyNames.Ctrl or KeyNames.Alt;

    private void StopKeybindChoices()
    {
        Capturing = null;
        ConfirmingReset = false;
    }
}
