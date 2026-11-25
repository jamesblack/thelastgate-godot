using System;
using System.Collections.Generic;
using TheLastGate.Net;

namespace TheLastGate.Chat;

public sealed class TabCompletion
{
    private const int MaxWords = 2048;
    private const int MaxWordLength = 39;

    private readonly List<string> words = [];
    private Cycle? cycle;

    public void Learn(string line)
    {
        var start = 0;
        while (start < line.Length)
        {
            if (!IsWordCharacter(line[start]))
            {
                start++;
                continue;
            }

            var end = start;
            while (end < line.Length && IsWordCharacter(line[end]))
            {
                end++;
            }

            Remember(line[start..Math.Min(end, start + MaxWordLength)]);
            start = end;
        }
    }

    public (string Text, int Caret)? Complete(string text, int caret)
    {
        var continuing = cycle != null && cycle.Shown == (text, caret);
        (string Text, int Caret) typed = continuing ? cycle!.Typed : (text, caret);
        var firstWord = continuing ? cycle!.NextWord : 0;
        cycle = null;

        var prefix = WordBefore(typed.Text, typed.Caret);
        if (prefix.Length == 0)
        {
            return null;
        }

        for (var index = firstWord; index < words.Count; index++)
        {
            var word = words[index];
            if (word.Length <= prefix.Length || !word.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var added = word[prefix.Length..] + " ";
            added = added[..Math.Min(added.Length, ChatPackets.MaxLength - typed.Text.Length)];
            var shown = (typed.Text.Insert(typed.Caret, added), typed.Caret + added.Length);
            cycle = new Cycle(typed, shown, index + 1);
            return shown;
        }

        return continuing ? typed : null;
    }

    private static bool IsWordCharacter(char character) => character == '#' || char.IsAsciiLetter(character);

    private static string WordBefore(string text, int caret)
    {
        var start = caret;
        while (start > 0 && caret - start < MaxWordLength && IsWordCharacter(text[start - 1]))
        {
            start--;
        }

        return text[start..caret];
    }

    private void Remember(string word)
    {
        words.Remove(word);
        words.Insert(0, word);
        if (words.Count > MaxWords)
        {
            words.RemoveAt(words.Count - 1);
        }
    }

    private sealed record Cycle((string Text, int Caret) Typed, (string Text, int Caret) Shown, int NextWord);
}
