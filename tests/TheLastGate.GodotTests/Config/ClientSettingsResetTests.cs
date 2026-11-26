using GdUnit4;
using TheLastGate.Config;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Config;

[TestSuite]
[RequireGodotRuntime]
public class ClientSettingsResetTests
{
    [TestCase]
    public void TheCursorSize_ResetsWithTheDisplayTabItSitsOn()
    {
        var settings = ClientSettings.Load();
        settings.CursorScale = 2;

        settings.Reset(OptionsTab.General);
        AssertThat(settings.CursorScale).IsEqual(2f);

        settings.Reset(OptionsTab.Display);
        AssertThat(settings.CursorScale).IsEqual(1f);
    }
}
