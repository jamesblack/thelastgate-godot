using GdUnit4;
using Godot;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

// The rows are the HUD art's 44 x 21 tile: a three-pixel run per row, each row two pixels further out.
[TestSuite]
[RequireGodotRuntime]
public class ProceduralDiamondTests
{
    private static readonly Rect2 Tile = new(0, 0, 44, 21);

    [TestCase(20, 0, true)]
    [TestCase(23, 0, true)]
    [TestCase(19, 0, false)]
    [TestCase(24, 0, false)]
    [TestCase(18, 1, true)]
    [TestCase(17, 1, false)]
    [TestCase(0, 10, true)]
    [TestCase(43, 10, true)]
    [TestCase(0, 0, false)]
    [TestCase(43, 20, false)]
    [TestCase(21, 20, true)]
    public void InDiamond_FollowsTheArtsStaircase(int x, int y, bool inside)
    {
        AssertThat(ProceduralLook.InDiamond(new Vector2(x + 0.5f, y + 0.5f), Tile)).IsEqual(inside);
    }

    [TestCase]
    public void InDiamond_WorksForADiamondOffsetFromThePanel()
    {
        var window = new Rect2(-271, -160, 1280, 687);

        AssertThat(ProceduralLook.InDiamond(new Vector2(34.5f, 12.5f), window)).IsTrue();
        AssertThat(ProceduralLook.InDiamond(new Vector2(14.5f, 8.5f), window)).IsFalse();
    }

    [TestCase]
    public void TheDiamondShape_IsPassedToTheShader()
    {
        var panel = new ProceduralPanel { Shape = ProceduralShape.Diamond };

        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("shape").AsInt32()).IsEqual(4);
        AssertThat(GD.Load<Shader>("res://Procedural/procedural.gdshader").Code).Contains("SHAPE_DIAMOND = 4");
        panel.Free();
    }

    [TestCase]
    public void AHole_IsPassedToTheShaderAndGetsItsOwnMaterial()
    {
        var panel = new ProceduralPanel { Hole = new Rect2(18, 10, 44, 21) };
        var plain = new ProceduralPanel();

        var hole = ((ShaderMaterial)panel.Material).GetShaderParameter("hole").AsVector4();
        AssertThat(hole).IsEqual(new Vector4(18, 10, 44, 21));
        AssertThat(ReferenceEquals(panel.Material, plain.Material)).IsFalse();
        panel.Free();
        plain.Free();
    }
}
