using GdUnit4;
using Godot;
using TheLastGate.Hud;
using TheLastGate.Procedural;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Procedural;

[TestSuite]
[RequireGodotRuntime]
public class ProceduralTabsTests
{
    private readonly List<int> chosen = [];
    private ProceduralTabs tabs = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public async Task ShowTabs()
    {
        tabs = new ProceduralTabs { Titles = ["General", "Audio", "Keybinds"] };
        var page = new ProceduralPanel { Name = "Page" };
        tabs.AddChild(page);
        foreach (var name in (string[])["GeneralPage", "AudioPage", "KeybindsPage"])
        {
            page.AddChild(new Control { Name = name });
        }

        Tree.Root.AddChild(tabs);
        chosen.Clear();
        tabs.TabChosen += chosen.Add;
        tabs.Select(0);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    [AfterTest]
    public void FreeTheTabs() => tabs.Free();

    [TestCase]
    public void Select_ShowsOnlyThatPage()
    {
        tabs.Select(1);

        AssertThat(Pages().Select(page => page.Visible)).ContainsExactly(false, true, false);
        AssertThat(chosen).IsEmpty();
    }

    [TestCase]
    public void ClickingAClosedTab_FiresTabChosen()
    {
        Click(2);

        AssertThat(chosen).ContainsExactly(2);
    }

    [TestCase]
    public void ClickingTheOpenTab_DoesNothing()
    {
        Click(0);

        AssertThat(chosen).IsEmpty();
    }

    [TestCase]
    public void OpenTab_KeepsTheTabHeight()
    {
        var heights = Tabs().Select(tab => tab.GetCombinedMinimumSize().Y).Distinct();

        AssertThat(heights.Count()).IsEqual(1);
    }

    [TestCase]
    public void OpenTab_JoinsThePage()
    {
        var open = Tabs()[0];

        AssertThat(open.Fill).IsEqual(ProceduralFill.Red);
        AssertThat(open.GetIndex()).IsGreater(PageIndex());
        AssertThat(Tabs()[1].Fill).IsEqual(ProceduralFill.Blue);
    }

    [TestCase]
    public void OpenTab_TurnsBothCornersIntoThePagesRing()
    {
        tabs.Select(1);

        AssertThat(Join(1, "Left").Visible).IsTrue();
        AssertThat(Join(1, "Right").Visible).IsTrue();
        AssertThat(Join(0, "Left").Visible).IsFalse();
        AssertThat(Join(0, "Right").Visible).IsFalse();
    }

    [TestCase]
    public void TheFirstTab_KeepsItsLeftSideOnThePagesEdge()
    {
        AssertThat(Join(0, "Left").Visible).IsFalse();
        AssertThat(Join(0, "Right").Visible).IsTrue();
    }

    [TestCase]
    public void Tabs_SitAboveThePageOverlappingItsTopRing()
    {
        var page = tabs.GetNode<Control>("Page");

        AssertThat(Tabs()[0].Position.Y).IsEqual(0f);
        AssertThat(page.Position.Y).IsEqual(Tabs()[0].Size.Y - 3);
        AssertThat(Tabs()[1].Position.X).IsEqual(Tabs()[0].Size.X + 1);
    }

    [TestCase]
    public void ClosedTabs_SitBehindThePagesTopRing()
    {
        foreach (var tab in Tabs())
        {
            AssertThat(tab.Edges.HasFlag(ProceduralEdges.Bottom)).IsFalse();
        }

        AssertThat(Tabs()[1].GetIndex()).IsLess(PageIndex());
        AssertThat(Tabs()[2].GetIndex()).IsLess(PageIndex());
        AssertThat(Tabs().Concat([tabs.GetNode<CanvasItem>("Page")]).All(item => item.ZIndex == 0)).IsTrue();
    }

    [TestCase]
    public void SettingTheSameState_KeepsTheHover()
    {
        Tabs()[1].GetNode<ClickArea>("Click").EmitSignal(Control.SignalName.MouseEntered);

        tabs.Select(0);

        AssertThat(Tabs()[1].Modulate).IsEqual(ClickArea.Hovered);
    }

    private List<ProceduralPanel> Tabs() =>
        Enumerable.Range(0, 3).Select(index => tabs.GetNode<ProceduralPanel>($"Tab{index}")).ToList();

    private int PageIndex() => tabs.GetNode("Page").GetIndex();

    private CanvasItem Join(int tab, string side) => Tabs()[tab].GetNode<CanvasItem>($"Click/Join{side}");

    private List<Control> Pages() => tabs.GetNode("Page").GetChildren().Cast<Control>().ToList();

    private void Click(int index) =>
        Tabs()[index]
            .GetNode<ClickArea>("Click")
            .EmitSignal(ClickArea.SignalName.Released, Variant.From(MapButton.Left));
}
