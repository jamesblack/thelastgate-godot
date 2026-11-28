using System;
using System.Collections.Generic;
using System.IO;
using TheLastGate.Characters;

namespace TheLastGate.Hud.Depot;

public static class DepotCharacters
{
    public static IReadOnlyList<DepotCharacter> FromHistory(
        LoginKey loggedIn,
        IEnumerable<string> filePaths,
        Func<string, LoginKey?> readKey)
    {
        var season = CharacterRace.FromNumber(loggedIn.Race).Season;
        var characters = new List<DepotCharacter>();
        var listed = new HashSet<int>();
        if (loggedIn.UserNumber != 0)
        {
            characters.Add(ToCharacter(loggedIn));
            listed.Add(characters[0].Number);
        }

        foreach (var filePath in filePaths)
        {
            if (readKey(filePath) is not { } key
                || key.UserNumber == 0
                || CharacterRace.FromNumber(key.Race).Season != season
                || !listed.Add((int)key.UserNumber))
            {
                continue;
            }

            characters.Add(ToCharacter(key));
        }

        return characters;
    }

    public static LoginKey? ReadKey(string filePath)
    {
        try
        {
            return CharacterRecords.Read(filePath).Key;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return null;
        }
    }

    private static DepotCharacter ToCharacter(LoginKey key) => new((int)key.UserNumber, key.Name);
}
