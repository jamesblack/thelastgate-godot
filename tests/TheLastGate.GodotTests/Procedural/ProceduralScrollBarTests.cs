using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralScrollBarTests
{
    private ProceduralScrollBar bar = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void MakeABar()
    {
        bar = new ProceduralScrollBar();
        Tree.Root.AddChild(bar);
    }

    [AfterTest]
    public void FreeTheBar() => bar.Free();

    [TestCase]
    public void TheGrabber_IsTheShadersGreenWithNoRings()
    {
        var material = (ShaderMaterial)bar.Material;

        AssertThat(material.GetShaderParameter("fill").AsInt32()).IsEqual((int)ProceduralFill.Green);
        AssertThat(material.GetShaderParameter("profile_width").AsInt32()).IsEqual(0);
        AssertThat(material.GetShaderParameter("edges").AsInt32()).IsEqual((int)ProceduralEdges.None);
    }

    [TestCase]
    public void TheGrabber_LightsUpLikeTheButtons()
    {
        AssertThat(Grabber("grabber")).IsEqual(Colors.White);
        AssertThat(Grabber("grabber_highlight")).IsEqual(ClickArea.Hovered);
        AssertThat(Grabber("grabber_pressed")).IsEqual(ClickArea.Held);
        AssertThat(bar.GetThemeStylebox("scroll")).IsInstanceOf<StyleBoxEmpty>();
    }

    [TestCase("material")]
    [TestCase("theme")]
    public void TheGeneratedLook_IsNotSavedIntoTheScene(string name)
    {
        var property = bar.GetPropertyList().Single(entry => entry["name"].AsString() == name);

        AssertThat(property["usage"].As<PropertyUsageFlags>().HasFlag(PropertyUsageFlags.Storage)).IsFalse();
    }

    private Color Grabber(string style) => ((StyleBoxFlat)bar.GetThemeStylebox(style)).BgColor;
}
