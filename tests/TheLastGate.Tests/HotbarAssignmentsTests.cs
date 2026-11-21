using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Hotbar;
using TheLastGate.State;

namespace TheLastGate.Tests;

public class HotbarAssignmentsTests
{
    private const int Protect = 17;
    private const int Blind = 37;
    private const int Light = 50;
    private const int Identify = 52;

    [Fact]
    public void Assign_PutsTheSkillOnTheButton()
    {
        var settings = new CharacterSettings();

        var line = HotbarAssignments.Assign(settings, 3, Protect, "Protect", "C-4");

        Assert.Equal(Protect, settings.HotbarActions[3]);
        Assert.Equal("Protect", settings.HotbarNames[3]);
        Assert.Equal(new LogLine("C-4 is now Protect.", (int)TextColour.Yellow), line);
    }

    [Fact]
    public void Assign_ShortensTheButtonNameToSevenLetters()
    {
        var settings = new CharacterSettings();

        var line = HotbarAssignments.Assign(settings, 3, 11, "Magic Shield", "C-4");

        Assert.Equal("Magic S", settings.HotbarNames[3]);
        Assert.Equal("C-4 is now Magic Shield.", line.Text);
    }

    [Fact]
    public void Assign_ReplacesAnotherSkillOnTheButton()
    {
        var settings = new CharacterSettings();
        HotbarAssignments.Assign(settings, 3, Protect, "Protect", "C-4");

        HotbarAssignments.Assign(settings, 3, Blind, "Blind", "C-4");

        Assert.Equal(Blind, settings.HotbarActions[3]);
        Assert.Equal("Blind", settings.HotbarNames[3]);
    }

    [Fact]
    public void Assign_OfTheSkillAlreadyOnTheButtonUnassignsIt()
    {
        var settings = new CharacterSettings();
        HotbarAssignments.Assign(settings, 3, Protect, "Protect", "C-4");

        var line = HotbarAssignments.Assign(settings, 3, Protect, "Protect", "C-4");

        Assert.Equal(CharacterSettings.Unassigned, settings.HotbarActions[3]);
        Assert.Equal("-", settings.HotbarNames[3]);
        Assert.Equal(new LogLine("C-4 is now unassigned.", (int)TextColour.Yellow), line);
    }

    [Fact]
    public void Assign_PutsAnItemOnTheButton()
    {
        var settings = new CharacterSettings();

        var line = HotbarAssignments.Assign(settings, 0, 105, HotbarButtons.ItemName(105), "A-Keypad 1");

        Assert.Equal(105, settings.HotbarActions[0]);
        Assert.Equal("Item 5", settings.HotbarNames[0]);
        Assert.Equal(new LogLine("A-Keypad 1 is now Item 5.", (int)TextColour.Yellow), line);
    }

    [Fact]
    public void Clear_UnassignsTheButton()
    {
        var settings = new CharacterSettings();
        HotbarAssignments.Assign(settings, 3, Protect, "Protect", "C-4");

        HotbarAssignments.Clear(settings, 3);

        Assert.Equal(CharacterSettings.Unassigned, settings.HotbarActions[3]);
        Assert.Equal("-", settings.HotbarNames[3]);
    }

    [Fact]
    public void MenuSkills_ListsTheLearntUsableSkillsInMenuOrder()
    {
        var skills = new PlayerState().Skills;
        skills[Protect][0] = 10;
        skills[Light][0] = 1;
        skills[Blind][0] = 5;
        skills[3][0] = 20;

        Assert.Equal([Blind, Light, Protect], HotbarAssignments.MenuSkills(skills, 0));
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(Kindred.KnowsIdentify, true)]
    public void MenuSkills_ListsIdentifyOnlyForCharactersWhoKnowIt(uint kindred, bool listed)
    {
        var skills = new PlayerState().Skills;
        skills[Identify][0] = 1;

        Assert.Equal(listed, HotbarAssignments.MenuSkills(skills, kindred).Contains(Identify));
    }
}
