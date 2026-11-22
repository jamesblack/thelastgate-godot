using GdUnit4;
using TheLastGate.Hud;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
public class LookPanelTests
{
    [TestCase]
    public void Shown_IsEmptyBeforeAnyLook()
    {
        AssertThat(new LookPanel().Shown).IsNull();
    }

    [TestCase]
    public void Hide_StopsShowingTheLookAtOnce()
    {
        var panel = new LookPanel();
        panel.Show(new CharacterLook());

        panel.Hide();

        AssertThat(panel.Shown).IsNull();
    }

    [TestCase]
    public void Show_KeepsTheLookForTenSeconds()
    {
        var panel = new LookPanel();
        var look = new CharacterLook();
        panel.Show(look);

        panel.Advance(TimeSpan.FromSeconds(9.9));
        AssertThat(panel.Shown).IsSame(look);

        panel.Advance(TimeSpan.FromSeconds(0.1));
        AssertThat(panel.Shown).IsNull();
    }

    [TestCase]
    public void Show_StartsTheTenSecondsAgainForTheNextLook()
    {
        var panel = new LookPanel();
        panel.Show(new CharacterLook());
        panel.Advance(TimeSpan.FromSeconds(8));
        var next = new CharacterLook();

        panel.Show(next);
        panel.Advance(TimeSpan.FromSeconds(8));

        AssertThat(panel.Shown).IsSame(next);
    }

    [TestCase(LookFlags.None, SpriteTint.None)]
    [TestCase(LookFlags.Shop, SpriteTint.None)]
    [TestCase(LookFlags.Shadow, SpriteTint.Grey)]
    [TestCase(LookFlags.Bloody, SpriteTint.Bloody)]
    [TestCase(LookFlags.Shadow | LookFlags.Bloody, SpriteTint.Grey | SpriteTint.Bloody)]
    public void PortraitTint_GreysShadowsAndReddensTheBloody(LookFlags flags, SpriteTint tint)
    {
        AssertThat(LookPanel.PortraitTint(flags)).IsEqual(tint);
    }
}
