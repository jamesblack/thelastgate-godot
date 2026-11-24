using GdUnit4;
using TheLastGate.Hud.SkillTree;
using TheLastGate.Net;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.SkillTree;

[TestSuite]
public class SkillTreeWindowTests
{
    private const int FirstNodeLearned = 1 << 15;
    private const int OneUnspent = 1;
    private const int TwoPointsOneSpent = FirstNodeLearned | OneUnspent;
    private const int AllSpent = FirstNodeLearned;
    private const int NoPoints = 0;

    private SkillTreeWindow window = new();

    [BeforeTest]
    public void CloseTheTree() => window = new SkillTreeWindow();

    [TestCase]
    public void Toggle_OpensTheClassTreeAndClosesItAgain()
    {
        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);
        AssertThat(window.Open).IsEqual(State.SkillTree.Class);

        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);
        AssertThat(window.Open).IsNull();
    }

    [TestCase(MapKeys.Shift)]
    [TestCase(MapKeys.Ctrl)]
    [TestCase(MapKeys.Alt)]
    public void Toggle_OpensTheContractTreeWithAModifierHeld(MapKeys keys)
    {
        window.Toggle(keys, TwoPointsOneSpent, AllSpent);

        AssertThat(window.Open).IsEqual(State.SkillTree.Contract);
    }

    [TestCase]
    public void Toggle_SwitchesBetweenTheTrees()
    {
        window.Toggle(MapKeys.Shift, TwoPointsOneSpent, AllSpent);

        window.Toggle(MapKeys.None, TwoPointsOneSpent, AllSpent);

        AssertThat(window.Open).IsEqual(State.SkillTree.Class);
    }

    [TestCase]
    public void Toggle_ClosesInsteadOfOpeningATreeWithoutPoints()
    {
        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);

        window.Toggle(MapKeys.Shift, TwoPointsOneSpent, NoPoints);

        AssertThat(window.Open).IsNull();
    }

    [TestCase]
    public void Close_SaysWhetherTheTreeWasOpen()
    {
        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);

        AssertThat(window.Close()).IsTrue();
        AssertThat(window.Open).IsNull();
        AssertThat(window.Close()).IsFalse();
    }

    [TestCase(MapButton.Left, MapKeys.None, 5)]
    [TestCase(MapButton.Left, MapKeys.Shift, 29)]
    [TestCase(MapButton.Left, MapKeys.Ctrl, 29)]
    [TestCase(MapButton.Right, MapKeys.None, 17)]
    [TestCase(MapButton.Right, MapKeys.Shift, 17)]
    public void Click_LearnsUnlearnsOrDescribesAClassNode(MapButton button, MapKeys keys, int value)
    {
        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);

        AssertThat(window.Click(5, button, keys)).IsEqual(ClientPackets.OneValue(ClientCommand.Tree, value));
    }

    [TestCase(MapButton.Left, MapKeys.None, 41)]
    [TestCase(MapButton.Left, MapKeys.Alt, 65)]
    [TestCase(MapButton.Right, MapKeys.None, 53)]
    public void Click_LearnsUnlearnsOrDescribesAContractNode(MapButton button, MapKeys keys, int value)
    {
        window.Toggle(MapKeys.Shift, TwoPointsOneSpent, AllSpent);

        AssertThat(window.Click(5, button, keys)).IsEqual(ClientPackets.OneValue(ClientCommand.Tree, value));
    }

    [TestCase]
    public void Click_SendsNothingForOtherButtonsOrAClosedTree()
    {
        AssertThat(window.Click(5, MapButton.Left, MapKeys.None)).IsNull();

        window.Toggle(MapKeys.None, TwoPointsOneSpent, NoPoints);

        AssertThat(window.Click(5, MapButton.None, MapKeys.None)).IsNull();
    }

    [TestCase(NoPoints, false)]
    [TestCase(AllSpent, true)]
    [TestCase(OneUnspent, true)]
    public void ButtonShows_OnlyWithClassTreePoints(int classPoints, bool shows)
    {
        AssertThat(SkillTreeWindow.ButtonShows(classPoints)).IsEqual(shows);
    }

    [TestCase]
    public void ButtonGlows_WhileATreeThatIsNotOpenHasPointsToSpend()
    {
        AssertThat(window.ButtonGlows(TwoPointsOneSpent, AllSpent)).IsTrue();
        AssertThat(window.ButtonGlows(AllSpent, OneUnspent)).IsTrue();
        AssertThat(window.ButtonGlows(AllSpent, AllSpent)).IsFalse();

        window.Toggle(MapKeys.None, TwoPointsOneSpent, AllSpent);
        AssertThat(window.ButtonGlows(TwoPointsOneSpent, AllSpent)).IsFalse();
        AssertThat(window.ButtonGlows(TwoPointsOneSpent, OneUnspent)).IsTrue();
    }

    [TestCase(0, 15)]
    [TestCase(4, 8)]
    [TestCase(8, 0)]
    [TestCase(13, 10)]
    [TestCase(16, 15)]
    public void GlowLight_SweepsEverySixteenMilliseconds(long milliseconds, int light)
    {
        AssertThat(SkillTreeWindow.GlowLight(milliseconds)).IsEqual(light);
    }

    [TestCase]
    public void Counter_ShowsUnspentOverTotal()
    {
        AssertThat(SkillTreeWindow.Counter(TwoPointsOneSpent)).IsEqual("1/2");
    }

    [TestCase]
    public void ShownText_PrintsTheServersDoubledPercentOnce()
    {
        AssertThat(SkillTreeWindow.ShownText("2%% more damage dealt")).IsEqual("2% more damage dealt");
    }
}
