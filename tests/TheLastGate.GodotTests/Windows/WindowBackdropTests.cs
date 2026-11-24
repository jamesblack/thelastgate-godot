using GdUnit4;
using Godot;
using TheLastGate.Config;
using TheLastGate.Hud;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
[RequireGodotRuntime]
public class WindowBackdropTests
{
    private WindowBackdrop backdrop = null!;
    private ClientSettings settings = null!;

    [BeforeTest]
    public void MakeABackdrop()
    {
        settings = ClientSettings.Load();
        backdrop = new WindowBackdrop { FadesAs = WindowBackground.Depot };
    }

    [AfterTest]
    public void FreeTheBackdrop() => backdrop.Free();

    [TestCase]
    public void ShowContext_FadesItsOwnStyleboxToTheSetting()
    {
        ShowAt(30);

        AssertThat(backdrop.SelfModulate.A).IsEqual(0.3f);
        AssertThat(backdrop.Modulate.A).IsEqual(1f);
    }

    [TestCase]
    public void ShowContext_LeavesChildrenSolid()
    {
        var label = new Label();
        backdrop.AddChild(label);

        ShowAt(0);

        AssertThat(FadeLayers.EffectiveAlpha(label)).IsEqual(1f);
    }

    [TestCase]
    public void ShowContext_FollowsANewSetting()
    {
        ShowAt(30);

        ShowAt(80);

        AssertThat(backdrop.SelfModulate.A).IsEqual(0.8f);
    }

    [TestCase]
    public void ABackdropThatFadesAsNone_NeverFades()
    {
        backdrop.FadesAs = WindowBackground.None;

        ShowAt(0);

        AssertThat(backdrop.SelfModulate.A).IsEqual(1f);
    }

    [TestCase]
    public void FadeLayers_CatchesAFilledBackdropInsideAnother()
    {
        var filled = new StyleBoxFlat { BgColor = Colors.Black };
        backdrop.AddThemeStyleboxOverride("panel", filled);
        var inner = new WindowBackdrop { Name = "Inner" };
        inner.AddThemeStyleboxOverride("panel", filled);
        backdrop.AddChild(inner);

        AssertThrown(() => FadeLayers.AssertNoStackedFills(backdrop)).IsInstanceOf<Exception>();
    }

    [TestCase]
    public void FadeLayers_AllowsAnOutlineInsideAFilledBackdrop()
    {
        backdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.Black });
        var inner = new WindowBackdrop();
        inner.AddThemeStyleboxOverride("panel", new StyleBoxFlat { DrawCenter = false });
        backdrop.AddChild(inner);

        FadeLayers.AssertNoStackedFills(backdrop);
    }

    private void ShowAt(int opacity)
    {
        settings.BackgroundOpacity[WindowBackground.Depot] = opacity;
        backdrop.ShowContext(new HudContext(null, null, settings, true));
    }
}
