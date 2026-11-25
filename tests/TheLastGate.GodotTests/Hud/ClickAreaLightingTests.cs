using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud;

[TestSuite]
[RequireGodotRuntime]
public class ClickAreaLightingTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    public void LitContent_LightsOnlyThatChild()
    {
        var area = new ClickArea { LightsUp = true };
        var sign = new Control { Name = "Sign" };
        area.AddChild(sign);
        area.LitContent = new NodePath("Sign");

        area.EmitSignal(Control.SignalName.MouseEntered);

        AssertThat(sign.Modulate).IsEqual(ClickArea.Hovered);
        AssertThat(area.Modulate).IsEqual(Colors.White);
        area.Free();
    }

    [TestCase]
    public void TurningLightsUpOff_PutsALitAreaBackToNormal()
    {
        var area = new ClickArea { LightsUp = true };
        area.EmitSignal(Control.SignalName.MouseEntered);
        AssertThat(area.Modulate).IsEqual(ClickArea.Hovered);

        area.LightsUp = false;

        AssertThat(area.Modulate).IsEqual(Colors.White);
        area.Free();
    }

    [TestCase]
    public void AMinimumGrabberLength_KeepsTheGrabberThatTallInEveryState()
    {
        var bar = new ProceduralScrollBar { MinimumGrabberLength = 11 };
        Tree.Root.AddChild(bar);

        foreach (var style in (string[])["grabber", "grabber_highlight", "grabber_pressed"])
        {
            AssertThat(bar.GetThemeStylebox(style).GetMinimumSize().Y).IsEqual(11f);
        }

        bar.MinimumGrabberLength = 0;
        AssertThat(bar.GetThemeStylebox("grabber").GetMinimumSize().Y).IsEqual(0f);
        bar.Free();
    }

    [TestCase]
    public async Task ADiamondTile_AnswersOnlyInsideItsVisibleDiamond()
    {
        var tile = new DiamondTile { Size = new Vector2(44, 21), Hole = new Rect2(-271, -160, 1280, 687) };
        Tree.Root.AddChild(tile);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(tile._HasPoint(new Vector2(14.5f, 8.5f))).IsTrue();
        AssertThat(tile._HasPoint(new Vector2(2.5f, 1.5f))).IsFalse();
        AssertThat(tile._HasPoint(new Vector2(34.5f, 12.5f))).IsFalse();
        tile.Free();
    }

    [TestCase]
    public async Task ADiamondTile_LightsUpButLetsTheClickReachTheWorld()
    {
        var tile = new DiamondTile { Size = new Vector2(44, 21), MouseFilter = Control.MouseFilterEnum.Pass };
        Tree.Root.AddChild(tile);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        tile.EmitSignal(Control.SignalName.MouseEntered);
        AssertThat(tile.Modulate).IsEqual(ClickArea.Hovered);
        AssertThat(ClickThrough.ReachesTheWorldAt(tile.GlobalPosition + new Vector2(22, 10))).IsTrue();
        tile.Free();
    }
}
