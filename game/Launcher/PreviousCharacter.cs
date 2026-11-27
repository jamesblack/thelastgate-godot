using System.Collections.Generic;
using TheLastGate.Characters;

namespace TheLastGate.Launcher;

public sealed record PreviousCharacter(
    string Name,
    string FilePath,
    string Class = "",
    string GameMode = "",
    string Season = "")
{
    public static PreviousCharacter Of(CharacterRecord record, string filePath)
    {
        if (record.Key.UserNumber == 0)
        {
            return new PreviousCharacter(record.Key.Name, filePath);
        }

        var race = CharacterRace.FromNumber(record.Key.Race);
        return new PreviousCharacter(
            record.Key.Name,
            filePath,
            CharacterChoiceText.ClassNames[race.Class],
            CharacterChoiceText.GameModeNames.GetValueOrDefault(race.GameMode, race.GameMode.ToString()),
            race.Season.ToString());
    }
}
