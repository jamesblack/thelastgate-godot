using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class RuledHeadingTests
{
    private RuledHeading heading = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowAHeading()
    {
        heading = GD.Load<PackedScene>("res://Procedural/ruled_heading.tscn").Instantiate<RuledHeading>();
        heading.Text = "Attributes";
        Tree.Root.AddChild(heading);
    }

    [AfterTest]
    public void FreeTheHeading() => heading.Free();

    [TestCase]
    public void Text_SetsTheWhiteTitle()
    {
        var title = heading.GetNode<HudLabel>("Title");

        AssertThat(title.Text).IsEqual("Attributes");
        AssertThat(title.Colour).IsEqual(TextColour.White);
    }

    [TestCase]
    public async Task Rule_SpansTheWidth()
    {
        heading.Size = new Vector2(200, 20);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        var line = heading.GetNode<ProceduralLine>("Rule/Line");

        AssertThat(line.Points[^1].X).IsEqual(200f);
    }
}
