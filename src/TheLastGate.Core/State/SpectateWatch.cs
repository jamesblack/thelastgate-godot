using System;

namespace TheLastGate.State;

// Spectating shows someone else's view, which must not be recorded as explored. The server only says so in chat, and
// players hidden from the who list leave without a goodbye, so the view centre moving back to our own character
// ends it too; it first has to have moved away, as the start message can arrive a tick before the new view.
public sealed class SpectateWatch
{
    private const string StartPrefix = "Now spectating ";
    private const string StopPrefix = "No longer spectating";
    private const string LeftGameSuffix = " left the game";
    private int ownCharacter;

    private Phase phase;
    private string targetName = "";

    public bool Active => phase != Phase.None;

    public void Reset()
    {
        phase = Phase.None;
        ownCharacter = 0;
        targetName = "";
    }

    public void LogLine(string line, int centreCharacter)
    {
        if (line.StartsWith(StartPrefix, StringComparison.Ordinal))
        {
            // Switching straight to another target keeps the character recorded when spectating began.
            if (phase == Phase.None)
            {
                ownCharacter = centreCharacter;
            }

            phase = Phase.Starting;
            targetName = NameAt(line[StartPrefix.Length..]);
        }
        else if (line.StartsWith(StopPrefix, StringComparison.Ordinal) || (Active && TargetLeft(line)))
        {
            Reset();
        }
    }

    public void ViewCentre(int centreCharacter)
    {
        if (phase == Phase.None || centreCharacter == 0 || ownCharacter == 0)
        {
            return;
        }

        if (centreCharacter != ownCharacter)
        {
            phase = Phase.Watching;
        }
        else if (phase == Phase.Watching)
        {
            Reset();
        }
    }

    private static string NameAt(string text)
    {
        var end = text.IndexOfAny(['.', ' ']);
        return end < 0 ? text : text[..end];
    }

    private bool TargetLeft(string line) =>
        targetName.Length > 0
        && line.StartsWith(targetName, StringComparison.Ordinal)
        && line.AsSpan(targetName.Length).StartsWith(LeftGameSuffix, StringComparison.Ordinal);

    private enum Phase
    {
        None,
        Starting,
        Watching,
    }
}
