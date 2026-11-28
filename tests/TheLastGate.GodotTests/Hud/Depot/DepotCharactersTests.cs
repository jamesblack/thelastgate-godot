using GdUnit4;
using TheLastGate.Characters;
using TheLastGate.Hud.Depot;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
public class DepotCharactersTests
{
    private const int LegacyRace = 4;
    private const int CurrentRace = 100004;
    private const int ShortFileSize = 10;

    private static readonly LoginKey LoggedIn = Key(1, "Main", CurrentRace);

    [TestCase]
    public void ListsOnlyTheLoggedInSeason()
    {
        var keys = new Dictionary<string, LoginKey?>
        {
            ["a"] = Key(2, "Alt", CurrentRace),
            ["b"] = Key(3, "Old", LegacyRace),
        };

        var characters = DepotCharacters.FromHistory(LoggedIn, keys.Keys, path => keys[path]);

        AssertThat(characters.Select(character => character.Name).ToArray()).IsEqual(new[] { "Main", "Alt" });
        AssertThat(characters[0].Number).IsEqual(1);
    }

    [TestCase]
    public void SkipsNewAccountsAndDuplicates()
    {
        var keys = new Dictionary<string, LoginKey?>
        {
            ["a"] = Key(0, "Fresh", CurrentRace),
            ["b"] = Key(1, "Main", CurrentRace),
            ["c"] = Key(2, "Alt", CurrentRace),
            ["d"] = Key(2, "Alt", CurrentRace),
        };

        var characters = DepotCharacters.FromHistory(LoggedIn, keys.Keys, path => keys[path]);

        AssertThat(characters.Select(character => character.Number).ToArray()).IsEqual(new[] { 1, 2 });
    }

    [TestCase]
    public void LoggedInNewAccount_IsNotListed()
    {
        var keys = new Dictionary<string, LoginKey?> { ["a"] = Key(2, "Alt", CurrentRace) };

        var characters = DepotCharacters.FromHistory(Key(0, "Fresh", CurrentRace), keys.Keys, path => keys[path]);

        AssertThat(characters.Select(character => character.Number).ToArray()).IsEqual(new[] { 2 });
    }

    [TestCase]
    public void UnreadableFile_IsSkipped()
    {
        var keys = new Dictionary<string, LoginKey?>
        {
            ["a"] = null,
            ["b"] = Key(2, "Alt", CurrentRace),
        };

        var characters = DepotCharacters.FromHistory(LoggedIn, keys.Keys, path => keys[path]);

        AssertThat(characters.Select(character => character.Number).ToArray()).IsEqual(new[] { 1, 2 });
    }

    [TestCase]
    public void ReadKey_TooShortFile_IsNull()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.moa");
        File.WriteAllBytes(filePath, new byte[ShortFileSize]);
        try
        {
            AssertThat(DepotCharacters.ReadKey(filePath) is null).IsTrue();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static LoginKey Key(uint number, string name, int race) => new(number, 0, 0, name, race);
}
