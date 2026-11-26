using GdUnit4;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralTooltipTests
{
    [TestCase]
    public void Make_ShowsTheTextInARedPanel()
    {
        var tip = ProceduralTooltip.Make("Fists use Hand to Hand,\nso pick Unarmed.");

        AssertThat(tip.Fill).IsEqual(ProceduralFill.Red);
        AssertThat(tip.Profile).IsEqual("GBG");
        var text = tip.GetNode<HudLabel>("Text");
        AssertThat(text.Text).IsEqual("Fists use Hand to Hand,\nso pick Unarmed.");
        AssertThat(text.Colour).IsEqual(TextColour.Yellow);
        tip.Free();
    }

    [TestCase]
    public void AClickArea_WithoutStyledTooltip_KeepsGodotsOwn()
    {
        var click = new ClickArea();

        AssertThat(click._MakeCustomTooltip("plain")).IsNull();
        click.Free();
    }
}
