namespace TheLastGate.Characters;

public static class CharacterRecords
{
    public static CharacterRecord CreateNew()
    {
        var record = CharacterRecord.CreateNew();
        record.Store(new CharacterSettings());
        return record;
    }

    // A file with no settings block gets the default settings, as the current client does.
    public static CharacterRecord Read(string filePath)
    {
        var record = CharacterRecord.Read(filePath);
        if (!record.SettingsRead)
        {
            record.Store(new CharacterSettings());
        }

        return record;
    }

    public static CharacterSettings Settings(this CharacterRecord record) =>
        CharacterSettings.Parse(record.SettingsBlock);

    public static void Store(this CharacterRecord record, CharacterSettings settings) =>
        settings.Write(record.SettingsBlock);
}
