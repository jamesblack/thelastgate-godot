using System;
using System.Collections.Generic;
using System.Linq;
using TheLastGate.World;

namespace TheLastGate.Hotkeys;

public readonly record struct BindingChange(string Id, KeyChord? Chord);

public sealed record Reassignment(IReadOnlyList<BindingChange> Changes, string? DisplacedId);

public static class BindingRules
{
    private const string KeypadPrefix = "Keypad ";
    private const string NotSet = "Not Set";

    private const string TypingRowWarning =
        "With enter to talk off, this hotkey types into the chat line instead of running.";

    private const string TypingWarningHeader =
        "With enter to talk off, these hotkeys type into the chat line instead of running:";

    public static Reassignment Assign(IReadOnlyDictionary<string, KeyChord?> current, string id, KeyChord chord)
    {
        var displaced = Bindings.All
            .FirstOrDefault(binding => binding.Id != id && current.GetValueOrDefault(binding.Id) == chord)
            ?.Id;
        var assigned = new BindingChange(id, chord);
        return displaced == null
            ? new Reassignment([assigned], null)
            : new Reassignment([new BindingChange(displaced, null), assigned], displaced);
    }

    public static IReadOnlyDictionary<string, KeyChord?> Load(
        IReadOnlyDictionary<string, KeyChord?> defaults,
        IReadOnlyDictionary<string, string> saved)
    {
        var current = defaults.ToDictionary(binding => binding.Key, binding => binding.Value);
        foreach (var (id, value) in saved.Where(entry => current.ContainsKey(entry.Key)))
        {
            if (value.Length == 0)
            {
                current[id] = null;
            }
            else if (KeyChord.Parse(value) is { } chord)
            {
                current[id] = chord;
            }
        }

        return current;
    }

    public static IReadOnlyDictionary<string, string> Saved(
        IReadOnlyDictionary<string, KeyChord?> defaults,
        IReadOnlyDictionary<string, KeyChord?> current) =>
        current
            .Where(binding => defaults.GetValueOrDefault(binding.Key) != binding.Value)
            .ToDictionary(binding => binding.Key, binding => binding.Value?.LongName ?? "");

    public static IReadOnlyList<Binding> TypingHotkeys(IReadOnlyDictionary<string, KeyChord?> current) =>
        Bindings.All
            .Where(binding => !binding.Hold && current.GetValueOrDefault(binding.Id) is { } chord
                && IsPlainCharacter(chord))
            .ToList();

    public static string? TypingHotkeysWarning(IReadOnlyDictionary<string, KeyChord?> current, bool enterToTalk)
    {
        var typing = TypingHotkeys(current);
        if (enterToTalk || typing.Count == 0)
        {
            return null;
        }

        var lines = typing.Select(binding => $"{binding.Name}: {current[binding.Id]!.Value.LongName}");
        return string.Join("\n", [TypingWarningHeader, .. lines]);
    }

    public static string? TypingHotkeyWarning(
        IReadOnlyDictionary<string, KeyChord?> current,
        string id,
        bool enterToTalk) =>
        !enterToTalk && TypingHotkeys(current).Any(binding => binding.Id == id) ? TypingRowWarning : null;

    public static string LongName(KeyChord? chord) => chord?.LongName ?? NotSet;

    public static string ShortName(KeyChord? chord) => chord?.ShortName ?? "";

    public static string HotbarKeyName(int slot) => ShortName(BindingActions.For(Bindings.SpellId(slot)));

    private static bool IsPlainCharacter(KeyChord chord) =>
        (chord.Modifiers & (MapKeys.Ctrl | MapKeys.Alt)) == MapKeys.None
        && (chord.Key.Length == 1 || chord.Key == "Space"
            || chord.Key.StartsWith(KeypadPrefix, StringComparison.Ordinal));
}
