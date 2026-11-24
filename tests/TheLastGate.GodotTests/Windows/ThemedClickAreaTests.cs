using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Windows;

[TestSuite]
[RequireGodotRuntime]
public class ThemedClickAreaTests
{
    private const string CloseVariation = "DarkCloseButton";
    private ThemedClickArea area = null!;

    private Theme theme = null!;

    [BeforeTest]
    public void MakeAnArea()
    {
        theme = GD.Load<Theme>("res://themes/OptionsTheme.tres");
        area = new ThemedClickArea { Theme = theme, ThemeTypeVariation = CloseVariation };
    }

    [AfterTest]
    public void FreeTheArea() => area.Free();

    [TestCase]
    public void ShownStyle_IsTheVariationsNormalStyle()
    {
        AssertThat(area.ShownStyle).IsSame(theme.GetStylebox("normal", CloseVariation));
    }

    [TestCase]
    public void ShownStyle_IsTheHoverStyleWhileTheMouseIsOver()
    {
        area.Notification((int)Control.NotificationMouseEnter);

        AssertThat(area.ShownStyle).IsSame(theme.GetStylebox("hover", CloseVariation));
    }

    [TestCase]
    public void ShownStyle_GoesBackToNormalWhenTheMouseLeaves()
    {
        area.Notification((int)Control.NotificationMouseEnter);

        area.Notification((int)Control.NotificationMouseExit);

        AssertThat(area.ShownStyle).IsSame(theme.GetStylebox("normal", CloseVariation));
    }

    [TestCase]
    public void ARightClick_IsStillReleased()
    {
        var released = new List<MapButton>();
        area.Size = new Vector2(14, 14);
        area.Released += button => released.Add(button);

        area._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Position = new Vector2(5, 5) });

        AssertThat(released).ContainsExactly(MapButton.Right);
    }
}
