using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Launcher;
using TheLastGate.Procedural;
using static GdUnit4.Assertions;
using HudLabel = TheLastGate.Hud.HudLabel;

namespace TheLastGate.GodotTests.Launcher;

[TestSuite]
[RequireGodotRuntime]
public class LauncherSceneTests
{
    private const string ScenePath = "res://Launcher/launcher.tscn";

    private static readonly string[] SavedFiles = ["user://settings.cfg", "user://character.moa"];

    private readonly Dictionary<string, byte[]?> saved = [];
    private TempFolder folder = null!;
    private LauncherScreen? launcher;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    private LauncherScreen Launcher => launcher!;

    [BeforeTest]
    public void KeepTheSavedFiles()
    {
        folder = new TempFolder();
        foreach (var path in SavedFiles)
        {
            var globalPath = ProjectSettings.GlobalizePath(path);
            saved[path] = File.Exists(globalPath) ? File.ReadAllBytes(globalPath) : null;
        }
    }

    [AfterTest]
    public void PutTheSavedFilesBack()
    {
        launcher?.Free();
        launcher = null;
        HudTextTheme.Apply(HudTextTheme.Load(), new ClassicFonts());
        foreach (var (path, bytes) in saved)
        {
            var globalPath = ProjectSettings.GlobalizePath(path);
            if (bytes is null)
            {
                File.Delete(globalPath);
            }
            else
            {
                File.WriteAllBytes(globalPath, bytes);
            }
        }

        folder.Dispose();
    }

    [TestCase]
    public void TheLauncher_HasNoClassicNodes()
    {
        var scene = AutoFree(GD.Load<PackedScene>(ScenePath).Instantiate())!;
        var nodes = scene.FindChildren("*");

        AssertThat(
                nodes.Any(node => node is Button or CheckBox or OptionButton or HSlider or PanelContainer
                    or NinePatchRect))
            .IsFalse();
        AssertThat(nodes.OfType<Label>().All(label => label is HudLabel)).IsTrue();
    }

    [TestCase]
    public async Task TheLauncher_FillsTheScreen()
    {
        await ShowLauncher();

        AssertThat(Launcher.GetNode<Control>("Frame").GetGlobalRect()).IsEqual(Launcher.GetViewportRect());
    }

    [TestCase]
    public async Task ClickingAPreviousCharacter_LoadsItAndLightsIt()
    {
        var first = WriteCharacter("First", 0);
        var second = WriteCharacter("Second", 0);
        RememberCharacters(first, second);
        await ShowLauncher();

        Rows()[1].EmitSignal(Control.SignalName.GuiInput, LeftClick());

        AssertThat(Launcher.GetNode<Label>("%AccountName").Text).IsEqual("Second");
        AssertThat(Table().SelectedRow).IsEqual(0);
        AssertThat(Rows()[0].Fill).IsEqual(ProceduralFill.Selected);
    }

    [TestCase]
    public async Task AnExistingCharacter_ShowsItsChoicesAsText()
    {
        var existing = WriteCharacter("Ishtar", 7);
        RememberCharacters(existing);
        await ShowLauncher();

        Rows()[0].EmitSignal(Control.SignalName.GuiInput, LeftClick());

        AssertThat(Launcher.GetNode<Control>("%ClassPicker").Visible).IsFalse();
        AssertThat(Launcher.GetNode<Label>("%ClassText").Visible).IsTrue();
        AssertThat(Launcher.GetNode<Label>("%ClassText").Text).IsEqual("Seyan");
    }

    [TestCase]
    public async Task APreviousCharacterSavedWithoutFacts_ShowsThemFromItsFile()
    {
        var existing = WriteCharacter("Ishtar", 7);
        RememberCharacters(existing);
        await ShowLauncher();

        var cells = Rows()[0].FindChildren("*", nameof(Label), true, false).Cast<Label>().Select(label => label.Text);

        AssertThat(cells).ContainsExactly("Ishtar", "Seyan", "Hard Mode", "Current");
    }

    [TestCase]
    public async Task TheCharacterLoadedAtStart_IsLitWhereverItIsInTheList()
    {
        var first = WriteCharacter("First", 0);
        var second = WriteCharacter("Second", 0);
        RememberCharacters(first, second);
        CharacterStore.Save(CharacterRecords.Read(second));
        await ShowLauncher();

        AssertThat(Table().SelectedRow).IsEqual(1);
    }

    [TestCase]
    public async Task ACharacterCreatedByStarting_GetsItsFactsInTheList()
    {
        var saved = WriteCharacter("Ishtar", 0);
        RememberCharacters(saved);
        var created = CharacterRecords.Read(saved);
        created.Key = created.Key with { UserNumber = 7 };
        CharacterStore.Save(created);
        await ShowLauncher();

        var cells = Rows()[0].FindChildren("*", nameof(Label), true, false).Cast<Label>().Select(label => label.Text);

        AssertThat(cells).ContainsExactly("Ishtar", "Seyan", "Hard Mode", "Current");
        AssertThat(ClientSettings.Load().CharacterHistory.Entries[0].Class).IsEqual("Seyan");
    }

    [TestCase]
    public void StartLoadSave_SitLeftAndDiscordExit_Right()
    {
        var scene = AutoFree(GD.Load<PackedScene>(ScenePath).Instantiate())!;
        var footer = scene.GetNode("Frame/Layout/Footer/Row").GetChildren().Select(child => child.Name.ToString());

        AssertThat(footer)
            .ContainsExactly(
                "StartButton",
                "LoadButton",
                "SaveButton",
                "StatusCentre",
                "DiscordButton",
                "ExitButton");
    }

    private static InputEventMouseButton LeftClick() => new() { ButtonIndex = MouseButton.Left, Pressed = true };

    private async Task ShowLauncher()
    {
        launcher = GD.Load<PackedScene>(ScenePath).Instantiate<LauncherScreen>();
        Tree.Root.AddChild(launcher);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
    }

    private DataTable Table() => Launcher.GetNode<DataTable>("%PreviousCharacters");

    private List<SettingsRow> Rows() => Table().GetNode("Rows/Body").GetChildren().Cast<SettingsRow>().ToList();

    private string WriteCharacter(string name, uint userNumber)
    {
        var record = CharacterRecords.CreateNew();
        var race = new CharacterRace(CharacterClass.Seyan, Body.Feminine, GameMode.Hard, Season.Current);
        record.Key = record.Key with { UserNumber = userNumber, Name = name, Race = race.ToNumber() };
        var path = Path.Combine(folder.FolderPath, name + ".moa");
        record.Write(path);
        return path;
    }

    private static void RememberCharacters(params string[] newestFirst)
    {
        var settings = ClientSettings.Load();
        foreach (var path in newestFirst.Reverse())
        {
            settings.CharacterHistory.Use(new PreviousCharacter(Path.GetFileNameWithoutExtension(path), path));
        }

        settings.Save();
    }
}
