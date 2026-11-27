using GdUnit4;
using Godot;
using TheLastGate.Config;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralPanelTests
{
    private ProceduralPanel panel = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void MakeAPanel() => panel = new ProceduralPanel();

    [AfterTest]
    public void FreeThePanel() => panel.Free();

    [TestCase]
    public void Profile_BecomesTheShadersRingsFromTheOutsideIn()
    {
        panel.Profile = "GBRW";

        var material = (ShaderMaterial)panel.Material;
        AssertThat(material.GetShaderParameter("profile").AsInt32Array()).ContainsExactly(0, 1, 2, 3, 0, 0, 0, 0);
        AssertThat(material.GetShaderParameter("profile_width").AsInt32()).IsEqual(4);
    }

    [TestCase]
    public void Fill_IsPassedToTheShader()
    {
        panel.Fill = ProceduralFill.Blue;

        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("fill").AsInt32()).IsEqual(1);
    }

    [TestCase]
    public void SelectedFill_IsPassedToTheShader()
    {
        panel.Fill = ProceduralFill.Selected;

        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("fill").AsInt32()).IsEqual(3);
    }

    [TestCase]
    public void SolidRings_IsPassedToTheShader()
    {
        panel.SolidRings = true;

        var plain = new ProceduralPanel();
        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("solid_rings").AsBool()).IsTrue();
        AssertThat(ReferenceEquals(panel.Material, plain.Material)).IsFalse();
        plain.Free();
    }

    [TestCase]
    public void GreenFill_IsPassedToTheShader()
    {
        panel.Fill = ProceduralFill.Green;

        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("fill").AsInt32()).IsEqual(4);
        AssertThat(GD.Load<Shader>("res://Procedural/procedural.gdshader").Code).Contains("FILL_GREEN = 4");
    }

    [TestCase]
    public async Task Children_SitInsideTheRingsAndPadding()
    {
        panel.Profile = "GBG";
        panel.Padding = 2;
        var child = new Control { CustomMinimumSize = new Vector2(10, 4) };
        panel.AddChild(child);
        Tree.Root.AddChild(panel);
        panel.Size = new Vector2(40, 30);

        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(child.Position).IsEqual(new Vector2(5, 5));
        AssertThat(child.Size).IsEqual(new Vector2(30, 20));
    }

    [TestCase]
    public void MinimumSize_IsTheChildrensPlusTheRingsAndPadding()
    {
        panel.Profile = "GBG";
        panel.Padding = 2;
        panel.AddChild(new Control { CustomMinimumSize = new Vector2(10, 4) });

        AssertThat(panel.GetCombinedMinimumSize()).IsEqual(new Vector2(20, 14));
    }

    [TestCase]
    public async Task ASideLeftOut_InsetsOnlyThePaddingThere()
    {
        panel.Profile = "GBG";
        panel.Padding = 2;
        panel.Edges = ProceduralEdges.Top;
        var child = new Control { CustomMinimumSize = new Vector2(10, 4) };
        panel.AddChild(child);
        Tree.Root.AddChild(panel);
        panel.Size = new Vector2(40, 30);

        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        AssertThat(panel.GetCombinedMinimumSize()).IsEqual(new Vector2(14, 11));
        AssertThat(child.Position).IsEqual(new Vector2(2, 5));
        AssertThat(child.Size).IsEqual(new Vector2(36, 23));
    }

    [TestCase]
    public void Edges_ArePassedToTheShader()
    {
        panel.Edges = ProceduralEdges.Top | ProceduralEdges.Left;

        AssertThat(((ShaderMaterial)panel.Material).GetShaderParameter("edges").AsInt32()).IsEqual(9);
    }

    [TestCase]
    public void FadesAs_SetsOnlyItsOwnAlpha()
    {
        panel.FadesAs = WindowBackground.SkillTree;
        var settings = ClientSettings.Load();
        settings.BackgroundOpacity[WindowBackground.SkillTree] = 25;

        panel.ShowContext(new HudContext(null, null, settings));

        AssertThat(panel.SelfModulate.A).IsEqual(0.25f);
        AssertThat(panel.Modulate.A).IsEqual(1f);
    }

    [TestCase]
    public void PanelsThatLookAlike_ShareOneMaterial()
    {
        var twin = new ProceduralPanel { Profile = "GBG" };
        panel.Profile = "GBG";

        AssertThat(panel.Material).IsSame(twin.Material);
        twin.Free();
    }

    [TestCase]
    public async Task SettingTheSameFill_DoesNotSortAgain()
    {
        AssertThat(await SortsAfter(() => panel.Fill = panel.Fill)).IsEqual(0);
    }

    [TestCase]
    public async Task ChangingTheFill_DoesNotSortAgain()
    {
        AssertThat(await SortsAfter(() => panel.Fill = ProceduralFill.Selected)).IsEqual(0);
    }

    [TestCase]
    public async Task ChangingThePadding_SortsAgain()
    {
        AssertThat(await SortsAfter(() => panel.Padding = 3)).IsEqual(1);
    }

    [TestCase]
    public void TheGeneratedLook_IsNotSavedIntoTheScene()
    {
        var material = panel.GetPropertyList().Single(property => property["name"].AsString() == "material");

        AssertThat(material["usage"].As<PropertyUsageFlags>().HasFlag(PropertyUsageFlags.Storage)).IsFalse();
    }

    [TestCase]
    public void ALine_FadesWithItsWindowsSetting()
    {
        var line = new ProceduralLine { FadesAs = WindowBackground.SkillTree };
        var settings = ClientSettings.Load();
        settings.BackgroundOpacity[WindowBackground.SkillTree] = 40;

        line.ShowContext(new HudContext(null, null, settings));

        AssertThat(line.SelfModulate.A).IsEqual(0.4f);
        line.Free();
    }

    private async Task<int> SortsAfter(Action change)
    {
        Tree.Root.AddChild(panel);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        var sorts = 0;
        panel.SortChildren += () => sorts++;

        change();
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        Tree.Root.RemoveChild(panel);
        return sorts;
    }
}
