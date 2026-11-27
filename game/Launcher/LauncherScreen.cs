using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Game;
using TheLastGate.Hud;
using TheLastGate.Map;
using TheLastGate.Net;
using TheLastGate.Procedural;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public partial class LauncherScreen : Control
{
    private const string StartAsArgument = "--start-as=";
    private const string DiscordInvite = "https://discord.gg/jJbPv2R";
    private const int MaxDescriptionLength = 159;
    private const int WindowedChoice = 1;
    private const string ClearMapText = "Clear saved map data";
    private const string ConfirmClearMapText = "Click again to clear the map";

    private static readonly CharacterClass[] NewClasses =
        [CharacterClass.Templar, CharacterClass.Mercenary, CharacterClass.Harakim];

    private static readonly Body[] Bodies = [Body.Masculine, Body.Feminine];
    private static readonly GameMode[] NewGameModes = [GameMode.Normal, GameMode.Hard];
    private static readonly Season[] Seasons = [Season.Legacy, Season.Current];

    private static readonly TableColumn[] PreviousCharacterColumns =
        [new("Name", 1.4f), new("Class"), new("Game mode"), new("Season")];

    private Label accountName = null!;
    private ProceduralPicker bodyPicker = null!;
    private Label bodyText = null!;
    private ProceduralPicker classPicker = null!;
    private Label classText = null!;
    private ProceduralButton clearMapButton = null!;
    private bool confirmingClearMap;
    private TextEdit descriptionField = null!;
    private ProceduralPicker gameModePicker = null!;
    private Label gameModeText = null!;
    private FileDialog loadDialog = null!;
    private LineEdit nameField = null!;
    private LineEdit passwordField = null!;
    private DataTable previousCharacters = null!;
    private LauncherPrompt prompt = null!;
    private CharacterRecord record = null!;
    private RollbackWindowView rollbackWindow = null!;
    private FileDialog saveDialog = null!;
    private ProceduralPicker seasonPicker = null!;
    private Label seasonText = null!;
    private ClientSettings settings = null!;
    private ProceduralCheckBox shadowsCheck = null!;
    private bool showingCharacter;
    private IReadOnlyList<PreviousCharacter> shownCharacters = [];
    private ProceduralCheckBox soundCheck = null!;
    private LoginHandoff? startingSession;
    private UpdateWindowView updateWindow = null!;
    private ProceduralSlider volumeSlider = null!;
    private Label volumeValue = null!;
    private ProceduralPicker windowPicker = null!;

    private string ServerLabel => $"{settings.Server.Host}:{settings.Server.Port}";

    public ILauncherFontSet Fonts { get; set; } = new PixelOperatorFonts();

    public override void _Ready()
    {
        SetProcess(false);
        if (VelopackStartup.HandledHook(GetTree()))
        {
            return;
        }

        ApplyFonts();
        FindNodes();
        WindowOverlays.Lift(this);
        GetNode<Label>("%Version").Text = ClientVersion.Text(
            typeof(LauncherScreen).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "");

        settings = ClientSettings.Load();
        ApplyWindowMode();
        ShowOptions();
        ShowChoices();

        record = CharacterStore.Load() ?? CharacterRecords.CreateNew();
        CharacterStore.Save(record);
        ShowCharacter();
        previousCharacters.SetColumns(PreviousCharacterColumns);
        var loadedFile = LoadedOnStart();
        DescribeCreatedCharacter(loadedFile);
        ShowPreviousCharacters(loadedFile);

        ConnectSignals();
        StartUpdates();
    }

    // Buttons are written in the bold font, everything else in the regular one.
    private void ApplyFonts()
    {
        var themed = (Theme)Theme.Duplicate();
        themed.DefaultFont = Fonts.Regular;
        themed.DefaultFontSize = Fonts.Size;
        themed.SetFont("font", "Button", Fonts.Bold);
        themed.SetFont("font", "OptionButton", Fonts.Regular);
        Theme = themed;
        HudTextTheme.Apply(HudTextTheme.Load(), new LauncherPartFonts(Fonts));
    }

    private void FindNodes()
    {
        accountName = GetNode<Label>("%AccountName");
        nameField = GetNode<LineEdit>("%NameField");
        passwordField = GetNode<LineEdit>("%PasswordField");
        descriptionField = GetNode<TextEdit>("%DescriptionField");
        classPicker = GetNode<ProceduralPicker>("%ClassPicker");
        bodyPicker = GetNode<ProceduralPicker>("%BodyPicker");
        gameModePicker = GetNode<ProceduralPicker>("%GameModePicker");
        seasonPicker = GetNode<ProceduralPicker>("%SeasonPicker");
        classText = GetNode<Label>("%ClassText");
        bodyText = GetNode<Label>("%BodyText");
        gameModeText = GetNode<Label>("%GameModeText");
        seasonText = GetNode<Label>("%SeasonText");
        previousCharacters = GetNode<DataTable>("%PreviousCharacters");
        windowPicker = GetNode<ProceduralPicker>("%WindowPicker");
        shadowsCheck = GetNode<ProceduralCheckBox>("%ShadowsCheck");
        soundCheck = GetNode<ProceduralCheckBox>("%SoundCheck");
        volumeSlider = GetNode<ProceduralSlider>("%VolumeSlider");
        volumeValue = GetNode<Label>("%VolumeValue");
        clearMapButton = GetNode<ProceduralButton>("%ClearMapButton");
        prompt = GetNode<LauncherPrompt>("%Prompt");
        loadDialog = GetNode<FileDialog>("%LoadDialog");
        saveDialog = GetNode<FileDialog>("%SaveDialog");
        updateWindow = GetNode<UpdateWindowView>("%UpdateWindow");
        rollbackWindow = GetNode<RollbackWindowView>("%RollbackWindow");
    }

    private ProceduralButton Button(string path) => GetNode<ProceduralButton>(path);

    private void ConnectSignals()
    {
        Button("%CreateAccountButton").Pressed += OnCreateAccountPressed;
        Button("%LoadButton").Pressed += () => OpenInLastFolder(loadDialog);
        Button("%SaveButton").Pressed += () => OpenInLastFolder(saveDialog);
        Button("%DiscordButton").Pressed += () => OS.ShellOpen(DiscordInvite);
        Button("%ExitButton").Pressed += () => GetTree().Quit();
        Button("%StartButton").Pressed += OnStartPressed;
        Button("%OpenDataFolderButton").Pressed += () => OpenFolder(DataPaths.PlayerData());
        Button("%OpenOverridesFolderButton").Pressed += () => OpenFolder(DataPaths.PlayerOverrides());
        clearMapButton.Pressed += OnClearMapPressed;
        clearMapButton.GetNode<Control>("Click").MouseExited += () => ShowClearMapConfirming(false);

        nameField.TextChanged += OnNameChanged;
        descriptionField.TextChanged += OnDescriptionChanged;
        classPicker.ItemChosen += _ => ShowChoiceDescriptions();
        gameModePicker.ItemChosen += _ => ShowChoiceDescriptions();
        seasonPicker.ItemChosen += _ => ShowChoiceDescriptions();
        previousCharacters.RowClicked += index => Import(shownCharacters[index].FilePath);

        windowPicker.ItemChosen += choice => ChangeSetting(
            () => settings.Windowed = choice == WindowedChoice,
            ApplyWindowMode);
        shadowsCheck.Toggled += isChecked => ChangeSetting(() => settings.Shadows = isChecked);
        soundCheck.Toggled += isChecked => ChangeSetting(() => settings.SoundEnabled = isChecked);
        volumeSlider.ValueChanged += value => ChangeSetting(
            () => settings.SoundVolume = (int)value,
            () => volumeValue.Text = settings.SoundVolume.ToString());

        loadDialog.FileSelected += OnLoadFileSelected;
        saveDialog.FileSelected += OnSaveFileSelected;
        StartIfAsked();
    }

    private void ShowOptions()
    {
        windowPicker.SetChoices(["Fullscreen", "Windowed"]);
        windowPicker.Select(settings.Windowed ? WindowedChoice : 0);
        shadowsCheck.Checked = settings.Shadows;
        soundCheck.Checked = settings.SoundEnabled;
        volumeSlider.SetValueNoSignal(settings.SoundVolume);
        volumeValue.Text = settings.SoundVolume.ToString();
    }

    private void ChangeSetting(Action change, Action? afterChange = null)
    {
        change();
        afterChange?.Invoke();
        settings.Save();
    }

    // Only switch between windowed and fullscreen, so a maximised window keeps its size.
    private void ApplyWindowMode()
    {
        var fullscreen = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen
            or DisplayServer.WindowMode.ExclusiveFullscreen;
        if (fullscreen == settings.Windowed)
        {
            DisplayServer.WindowSetMode(
                settings.Windowed ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        }
    }

    private void ShowChoices()
    {
        classPicker.SetChoices(NewClasses.Select(choice => CharacterChoiceText.ClassNames[choice]).ToList());
        bodyPicker.SetChoices(Bodies.Select(choice => choice.ToString()).ToList());
        gameModePicker.SetChoices(NewGameModes.Select(choice => CharacterChoiceText.GameModeNames[choice]).ToList());
        seasonPicker.SetChoices(Seasons.Select(choice => choice.ToString()).ToList());
        ResetNewCharacterChoices();
    }

    private void ShowChoiceDescriptions()
    {
        classPicker.Tooltip =
            CharacterChoiceText.ClassDescriptions.GetValueOrDefault(NewClasses[classPicker.Selected], "");
        gameModePicker.Tooltip =
            CharacterChoiceText.GameModeDescriptions.GetValueOrDefault(NewGameModes[gameModePicker.Selected], "");
        seasonPicker.Tooltip =
            CharacterChoiceText.SeasonDescriptions.GetValueOrDefault(Seasons[seasonPicker.Selected], "");
    }

    private void ShowCharacter()
    {
        showingCharacter = true;
        accountName.Text = record.Key.Name;
        nameField.Text = record.CharacterName;
        descriptionField.Text = record.Description;

        var existingCharacter = record.Key.UserNumber != 0;
        if (existingCharacter)
        {
            var race = CharacterRace.FromNumber(record.Key.Race);
            classText.Text = CharacterChoiceText.ClassNames[race.Class];
            bodyText.Text = race.Body.ToString();
            gameModeText.Text =
                CharacterChoiceText.GameModeNames.GetValueOrDefault(race.GameMode, race.GameMode.ToString());
            seasonText.Text = race.Season.ToString();
        }

        foreach (var (picker, text) in (ReadOnlySpan<(Control, Control)>)
            [
                (classPicker, classText), (bodyPicker, bodyText), (gameModePicker, gameModeText),
                (seasonPicker, seasonText),
            ])
        {
            picker.Visible = !existingCharacter;
            text.Visible = existingCharacter;
        }

        ShowChoiceDescriptions();
        showingCharacter = false;
    }

    private void ResetNewCharacterChoices()
    {
        classPicker.Select(Array.IndexOf(NewClasses, CharacterClass.Templar));
        bodyPicker.Select(Array.IndexOf(Bodies, Body.Masculine));
        gameModePicker.Select(Array.IndexOf(NewGameModes, GameMode.Normal));
        seasonPicker.Select(Array.IndexOf(Seasons, Season.Current));
    }

    private void OnNameChanged(string name)
    {
        record.CharacterName = name;
        SaveEditedCharacter();
    }

    private void OnDescriptionChanged()
    {
        if (descriptionField.Text.Length > MaxDescriptionLength)
        {
            descriptionField.Text = descriptionField.Text[..MaxDescriptionLength];
            var lastLine = descriptionField.GetLineCount() - 1;
            descriptionField.SetCaretLine(lastLine);
            descriptionField.SetCaretColumn(descriptionField.GetLine(lastLine).Length);
        }

        record.Description = descriptionField.Text;

        // An edited description is sent to the server at the next login.
        if (!showingCharacter)
        {
            record.Changed = true;
        }

        SaveEditedCharacter();
    }

    private void SaveEditedCharacter()
    {
        if (showingCharacter)
        {
            return;
        }

        record.Changed = true;
        CharacterStore.Save(record);
    }

    private void StartIfAsked()
    {
        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            if (!argument.StartsWith(StartAsArgument, StringComparison.Ordinal))
            {
                continue;
            }

            var name = argument[StartAsArgument.Length..];
            if (record.Key.Name == name)
            {
                Callable.From(OnStartPressed).CallDeferred();
            }
            else
            {
                GD.PushWarning($"Not starting: the loaded character is \"{record.Key.Name}\", not \"{name}\".");
            }
        }
    }

    private void OnStartPressed()
    {
        UniqueId uniqueId;
        try
        {
            uniqueId = UniqueIdStore.LoadOrCreate();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't read or save the unique id: {exception.Message}");
            prompt.Tell("FYI", "Could not read or save this computer's unique id.");
            return;
        }

        var race = record.Key.UserNumber != 0
            ? record.Key.Race
            : new CharacterRace(
                NewClasses[classPicker.Selected],
                Bodies[bodyPicker.Selected],
                NewGameModes[gameModePicker.Selected],
                Seasons[seasonPicker.Selected]).ToNumber();
        var handshake = new LoginHandshake(record.Key, race, passwordField.Text, uniqueId.Low, uniqueId.High);
        var connection = new ServerConnection();
        startingSession = new LoginHandoff(record, handshake, settings.Server, connection);
        connection.Start(settings.Server, handshake);
        prompt.Tell("Connection", "STATUS: Connecting...");
        prompt.CanClose = false;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (startingSession is not { } session)
        {
            return;
        }

        // The state is read before the messages, because the connection queues its last message
        // before it changes state.
        var state = session.Connection.State;
        while (session.Connection.TryDequeueMessage(out var message))
        {
            prompt.ShowText($"STATUS: {message}");
        }

        if (state == ConnectionState.Connected)
        {
            EndStarting();
            LoginHandoff.Begin(session);
            GetTree().ChangeSceneToFile("res://Game/game.tscn");
        }
        else if (state is ConnectionState.Failed or ConnectionState.Closed)
        {
            EndStarting();
            session.Connection.Dispose();
            prompt.CanClose = true;
        }
    }

    private void EndStarting()
    {
        startingSession = null;
        SetProcess(false);
    }

    private void OnCreateAccountPressed() =>
        ConfirmTwice(
            "Do you really want to create a new account?",
            () =>
            {
                record.StartNewAccount();
                CharacterStore.Save(record);
                passwordField.Text = "";
                ResetNewCharacterChoices();
                ShowCharacter();
                ShowPreviousCharacters(null);
            });

    private void OnLoadFileSelected(string filePath) =>
        ConfirmTwice("Do you really want to switch accounts?", () => Import(filePath));

    private void Import(string filePath)
    {
        try
        {
            record = CharacterRecords.Read(filePath);

            // As in the current client, a character picked from a file sends its name and description again.
            record.Changed = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException)
        {
            GD.PushError($"Couldn't open the character file {filePath}: {exception.Message}");
            prompt.Tell("FYI", $"Could not open file {filePath}");
            return;
        }

        CharacterStore.Save(record);
        RememberCharacterFile(filePath);
        passwordField.Text = "";
        ShowCharacter();
    }

    private void OnSaveFileSelected(string filePath)
    {
        try
        {
            record.Write(filePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't save the character file {filePath}: {exception.Message}");
            prompt.Tell("FYI", $"Could not save file {filePath}");
            return;
        }

        RememberCharacterFile(filePath);
        prompt.Tell("FYI", $"Saved file as {filePath}");
    }

    private void OpenInLastFolder(FileDialog dialog)
    {
        var lastFolder = settings.CharacterHistory.LastFolder;
        if (lastFolder != null && Directory.Exists(lastFolder))
        {
            dialog.CurrentDir = lastFolder;
        }

        dialog.PopupCentered();
    }

    private void RememberCharacterFile(string filePath)
    {
        settings.CharacterHistory.Use(PreviousCharacter.Of(record, filePath));
        settings.Save();
        ShowPreviousCharacters(filePath);
    }

    // The launcher keeps its own copy of the character, so the file it came from is found by the character's name.
    private string? LoadedOnStart() =>
        settings.CharacterHistory.Entries.FirstOrDefault(entry => entry.Name == record.Key.Name)?.FilePath;

    // A character saved before its first Start has no facts in its file, so they come from the created one.
    private void DescribeCreatedCharacter(string? loadedFile)
    {
        if (loadedFile is null || record.Key.UserNumber == 0)
        {
            return;
        }

        var described = PreviousCharacter.Of(record, loadedFile);
        if (settings.CharacterHistory.Entries.Contains(described))
        {
            return;
        }

        settings.CharacterHistory.Describe(described);
        settings.Save();
    }

    private void ShowPreviousCharacters(string? loadedFile)
    {
        shownCharacters = settings.CharacterHistory.Entries.Select(WithFacts).ToList();
        previousCharacters.ShowRows(shownCharacters.Select(PreviousCharacterRow).ToList());
        previousCharacters.SelectedRow = shownCharacters.ToList()
            .FindIndex(entry => string.Equals(
                entry.FilePath,
                loadedFile,
                StringComparison.OrdinalIgnoreCase));
    }

    // Entries saved before the history kept a character's facts get them from its file.
    private static PreviousCharacter WithFacts(PreviousCharacter entry)
    {
        if (entry.Class.Length > 0)
        {
            return entry;
        }

        try
        {
            return PreviousCharacter.Of(CharacterRecords.Read(entry.FilePath), entry.FilePath) with
            {
                Name = entry.Name,
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException)
        {
            return entry;
        }
    }

    private static TableRow PreviousCharacterRow(PreviousCharacter entry) =>
        new(
        [
            new TableCell(entry.Name.Length > 0 ? entry.Name : Path.GetFileNameWithoutExtension(entry.FilePath)),
            new TableCell(entry.Class, CellTone.Dim),
            new TableCell(entry.GameMode, CellTone.Dim),
            new TableCell(entry.Season, CellTone.Dim),
        ]);

    private void StartUpdates()
    {
        updateWindow.Fonts = Fonts;
        rollbackWindow.Fonts = Fonts;
        var updateStatus = GetNode<UpdateStatusView>("%UpdateStatus");
        updateStatus.NotesFound += notes => ChangelogText.Fill(GetNode<RichTextLabel>("%UpdateNotes"), notes, Fonts);
        var updater = Updaters.Choose(OS.GetCmdlineUserArgs());
        if (updater is PreviewUpdater preview)
        {
            preview.Applied += version =>
                prompt.Tell("Preview", $"A real update would now restart the launcher on version {version}.");
        }
        else
        {
            updateStatus.Restarting += () => GetTree().Quit();
        }

        updateStatus.Start(updater, updateWindow, rollbackWindow);
    }

    private void OnClearMapPressed()
    {
        if (!confirmingClearMap)
        {
            ShowClearMapConfirming(true);
            return;
        }

        ShowClearMapConfirming(false);
        prompt.Tell(
            "FYI",
            ExploredMapKeeper.ClearSaved(settings.Server)
                ? $"Saved map data for {ServerLabel} has been cleared."
                : $"Could not clear the saved map data for {ServerLabel}.");
    }

    private void ShowClearMapConfirming(bool confirming)
    {
        confirmingClearMap = confirming;
        clearMapButton.Warning = confirming;
        clearMapButton.Caption = confirming ? ConfirmClearMapText : ClearMapText;
        clearMapButton.GetNode<Control>("Click").TooltipText = confirming
            ? $"Every area explored on {ServerLabel}, by all of your characters, will be forgotten."
            : "";
    }

    private void OpenFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't create the folder {folder}: {exception.Message}");
            prompt.Tell("FYI", $"Could not open the folder {folder}");
            return;
        }

        var opened = OS.ShellOpen(folder);
        if (opened != Error.Ok)
        {
            GD.PushError($"Couldn't open the folder {folder}: {opened}");
            prompt.Tell("FYI", $"Could not open the folder {folder}");
        }
    }

    private void ConfirmTwice(string question, Action onYes) =>
        prompt.Ask(
            "Are you sure?",
            question,
            "Your old account will no longer be accessible, unless you remembered to save it.",
            "You did save it, didn't you?",
            () => prompt.Ask(
                "Are you really sure?",
                "No Really!",
                "This will replace your current character.",
                "This action cannot be undone.",
                onYes));
}
