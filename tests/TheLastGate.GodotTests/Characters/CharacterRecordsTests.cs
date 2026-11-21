using GdUnit4;
using TheLastGate.Characters;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Characters;

[TestSuite]
public class CharacterRecordsTests
{
    [TestCase]
    public void CreateNew_HasDefaultSettings()
    {
        var settings = CharacterRecords.CreateNew().Settings();

        AssertThat(settings.HideWalls).IsTrue();
        AssertThat(settings.HotbarActions[0]).IsEqual(CharacterSettings.Unassigned);
    }

    [TestCase]
    public void Read_GivesAKeyOnlyFileDefaultSettings()
    {
        using var folder = new TempFolder();
        var keyBytes = new byte[LoginKey.Size];
        new LoginKey(77, 1, 2, "Old", 4).Write(keyBytes);
        var path = folder.Write("old.moa", keyBytes);

        var record = CharacterRecords.Read(path);

        AssertThat(record.Key.UserNumber).IsEqual(77u);
        AssertThat(record.Settings().ShowNames).IsTrue();
    }

    [TestCase]
    public void StoredSettings_ReadBackFromTheFile()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.FolderPath, "ishtar.moa");
        var record = CharacterRecords.CreateNew();
        var settings = record.Settings();
        settings.HotbarActions[0] = 50;
        settings.ShowHealthBars = true;
        record.Store(settings);

        record.Write(path);
        var read = CharacterRecords.Read(path).Settings();

        AssertThat(read.HotbarActions[0]).IsEqual(50);
        AssertThat(read.ShowHealthBars).IsTrue();
    }
}
