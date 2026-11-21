using System;
using System.IO;
using Godot;
using TheLastGate.Characters;

namespace TheLastGate.Config;

public static class CharacterStore
{
    private const string FilePath = "user://character.moa";

    private static string GlobalPath => ProjectSettings.GlobalizePath(FilePath);

    public static CharacterRecord? Load()
    {
        if (!File.Exists(GlobalPath))
        {
            return null;
        }

        try
        {
            return CharacterRecords.Read(GlobalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException)
        {
            GD.PushError($"Couldn't read the current character: {exception.Message}");
            return null;
        }
    }

    public static void Save(CharacterRecord record)
    {
        try
        {
            record.Write(GlobalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't save the current character: {exception.Message}");
        }
    }
}
