using System;
using System.Collections.Generic;
using System.IO;

namespace TheLastGate.Launcher;

public sealed class CharacterHistory
{
    private readonly List<PreviousCharacter> entries = [];

    public CharacterHistory()
    {
    }

    public CharacterHistory(IEnumerable<PreviousCharacter> mostRecentFirst)
    {
        foreach (var entry in mostRecentFirst)
        {
            if (IndexOf(entry.FilePath) < 0)
            {
                entries.Add(entry);
            }
        }
    }

    public IReadOnlyList<PreviousCharacter> Entries => entries;

    public string? LastFolder => entries.Count > 0 ? Path.GetDirectoryName(entries[0].FilePath) : null;

    public void Use(PreviousCharacter character)
    {
        var knownIndex = IndexOf(character.FilePath);
        if (knownIndex >= 0)
        {
            entries.RemoveAt(knownIndex);
        }

        entries.Insert(0, character);
    }

    public void Describe(PreviousCharacter character)
    {
        var knownIndex = IndexOf(character.FilePath);
        if (knownIndex >= 0)
        {
            entries[knownIndex] = character;
        }
    }

    public void RemoveMissingFiles() => entries.RemoveAll(entry => !File.Exists(entry.FilePath));

    private int IndexOf(string filePath) =>
        entries.FindIndex(entry => string.Equals(entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
}
