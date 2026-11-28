using GdUnit4;
using Godot;
using TheLastGate.Config;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Config;

[TestSuite]
[RequireGodotRuntime]
public class ClientSettingsSaveTests
{
    private const string SettingsPath = "user://settings.cfg";

    private byte[]? saved;

    private static string GlobalPath => ProjectSettings.GlobalizePath(SettingsPath);

    [BeforeTest]
    public void KeepTheSavedSettings() => saved = File.Exists(GlobalPath) ? File.ReadAllBytes(GlobalPath) : null;

    [AfterTest]
    public void PutTheSavedSettingsBack()
    {
        if (saved is null)
        {
            File.Delete(GlobalPath);
        }
        else
        {
            File.WriteAllBytes(GlobalPath, saved);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void TheDepotCharacterList_IsSavedAsDepotCharacterListInGeneral(bool isOn)
    {
        var settings = ClientSettings.Load();
        settings[GeneralOption.DepotCharacterList] = isOn;

        settings.Save();
        var file = new ConfigFile();
        file.Load(SettingsPath);

        AssertThat(file.GetValue("general", "depot_character_list", !isOn).AsBool()).IsEqual(isOn);
        AssertThat(ClientSettings.Load().DepotCharacterList).IsEqual(isOn);
    }
}
