using GdUnit4;
using Godot;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralJoinTests
{
    [TestCase]
    public void JoinedRects_ArePassedToTheShader()
    {
        var panel = new ProceduralPanel { JoinedWith = [new Rect2(-68, 51, 70, 38), new Rect2(36, 51, 70, 38)] };

        var material = (ShaderMaterial)panel.Material;
        var joined = material.GetShaderParameter("joined").AsVector4Array();
        AssertThat(material.GetShaderParameter("joined_count").AsInt32()).IsEqual(2);
        AssertThat(joined[0]).IsEqual(new Vector4(-68, 51, 70, 38));
        AssertThat(joined[1]).IsEqual(new Vector4(36, 51, 70, 38));
        panel.Free();
    }

    [TestCase]
    public void PanelsJoinedTheSameWay_ShareAMaterial()
    {
        var first = new ProceduralPanel { JoinedWith = [new Rect2(0, 30, 34, 16)] };
        var second = new ProceduralPanel { JoinedWith = [new Rect2(0, 30, 34, 16)] };
        var plain = new ProceduralPanel();

        AssertThat(ReferenceEquals(first.Material, second.Material)).IsTrue();
        AssertThat(ReferenceEquals(first.Material, plain.Material)).IsFalse();
        AssertThat(((ShaderMaterial)plain.Material).GetShaderParameter("joined_count").AsInt32()).IsEqual(0);
        first.Free();
        second.Free();
        plain.Free();
    }
}
