using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Game;
using TheLastGate.Map;
using TheLastGate.Net;
using TheLastGate.Updates;

namespace TheLastGate.Launcher;

public partial class LauncherScreen : Control
{
    private const string StartAsArgument = "--start-as=";
    private const string DiscordInvite = "https://discord.gg/jJbPv2R";
    private const int MaxDescriptionLength = 159;
    private const int WindowedOptionId = 1;
    private const int DialogButtonGap = 10;

    private static readonly Color LockedRowColour = new(1, 1, 1, 0.6f);
    private static readonly Vector2 PreviousCharacterButtonSize = new(240, 24);

    private Label accountName = null!;
    private OptionButton bodyOption = null!;
    private OptionButton classOption = null!;
    private ConfirmationDialog confirmDialog = null!;
    private Label confirmExplanation = null!;
    private Label confirmQuestion = null!;
    private Label confirmTitle = null!;
    private Label confirmWarning = null!;
    private CheckBox darkGuiCheck = null!;
    private TextEdit descriptionField = null!;
    private OptionButton gameModeOption = null!;
    private FileDialog loadDialog = null!;
    private AcceptDialog messageDialog = null!;
    private Label messageText = null!;
    private Label messageTitle = null!;
    private ColorRect modalDim = null!;
    private LineEdit nameField = null!;
    private LineEdit passwordField = null!;
    private Action? pendingConfirmation;
    private VBoxContainer previousCharacterList = null!;
    private (Label Label, OptionButton Option)[] raceRows = [];
    private CharacterRecord record = null!;
    private RollbackWindowView rollbackWindow = null!;
    private FileDialog saveDialog = null!;
    private OptionButton seasonOption = null!;
    private int selectableClassCount;
    private int selectableGameModeCount;
    private ClientSettings settings = null!;
    private CheckBox shadowsCheck = null!;
    private bool showingCharacter;
    private CheckBox soundCheck = null!;
    private LoginHandoff? startingSession;
    private UpdateWindowView updateWindow = null!;
    private HSlider volumeSlider = null!;
    private Label volumeValue = null!;
    private OptionButton windowTypeOption = null!;

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

        settings = ClientSettings.Load();
        ApplyWindowMode();
        ShowOptions();

        record = CharacterStore.Load() ?? CharacterRecords.CreateNew();
        CharacterStore.Save(record);
        selectableClassCount = classOption.ItemCount;
        selectableGameModeCount = gameModeOption.ItemCount;
        AddDescriptions();
        ShowCharacter();
        ShowPreviousCharacters();

        ConnectSignals();
        StartUpdates();
    }

    // Buttons are written in the bold font, everything else in the regular one.
    private void ApplyFonts()
    {
        var themed = WindowFonts.WithFont(Theme, Fonts.Regular, Fonts.Size);
        themed.SetFont("font", "Button", Fonts.Bold);
        themed.SetFont("font", "OptionButton", Fonts.Regular);
        Theme = themed;
    }

    private void FindNodes()
    {
        accountName = GetNode<Label>("%AccountName");
        nameField = GetNode<LineEdit>("%NameField");
        passwordField = GetNode<LineEdit>("%PasswordField");
        descriptionField = GetNode<TextEdit>("%DescriptionField");
        classOption = GetNode<OptionButton>("%ClassOption");
        bodyOption = GetNode<OptionButton>("%BodyOption");
        gameModeOption = GetNode<OptionButton>("%GameModeOption");
        seasonOption = GetNode<OptionButton>("%SeasonOption");
        raceRows =
        [
            (GetNode<Label>("%ClassLabel"), classOption),
            (GetNode<Label>("%BodyLabel"), bodyOption),
            (GetNode<Label>("%GameModeLabel"), gameModeOption),
            (GetNode<Label>("%SeasonLabel"), seasonOption),
        ];
        previousCharacterList = GetNode<VBoxContainer>("%PreviousCharacterList");
        windowTypeOption = GetNode<OptionButton>("%WindowTypeOption");
        shadowsCheck = GetNode<CheckBox>("%ShadowsCheck");
        soundCheck = GetNode<CheckBox>("%SoundCheck");
        volumeSlider = GetNode<HSlider>("%VolumeSlider");
        volumeValue = GetNode<Label>("%VolumeValue");
        darkGuiCheck = GetNode<CheckBox>("%DarkGuiCheck");
        modalDim = GetNode<ColorRect>("%ModalDim");
        messageDialog = GetNode<AcceptDialog>("%MessageDialog");
        messageTitle = GetNode<Label>("%MessageTitle");
        messageText = GetNode<Label>("%MessageText");
        confirmDialog = GetNode<ConfirmationDialog>("%ConfirmDialog");
        confirmTitle = GetNode<Label>("%ConfirmTitle");
        confirmQuestion = GetNode<Label>("%ConfirmQuestion");
        confirmExplanation = GetNode<Label>("%ConfirmExplanation");
        confirmWarning = GetNode<Label>("%ConfirmWarning");
        loadDialog = GetNode<FileDialog>("%LoadDialog");
        saveDialog = GetNode<FileDialog>("%SaveDialog");
        updateWindow = GetNode<UpdateWindowView>("%UpdateWindow");
        rollbackWindow = GetNode<RollbackWindowView>("%RollbackWindow");
    }

    private void ConnectSignals()
    {
        GetNode<Button>("%CreateAccountButton").Pressed += OnCreateAccountPressed;
        GetNode<Button>("%LoadButton").Pressed += () => loadDialog.PopupCentered();
        GetNode<Button>("%SaveButton").Pressed += () => saveDialog.PopupCentered();
        GetNode<Button>("%DiscordButton").Pressed += () => OS.ShellOpen(DiscordInvite);
        GetNode<Button>("%ExitButton").Pressed += () => GetTree().Quit();
        GetNode<Button>("%StartButton").Pressed += OnStartPressed;
        GetNode<Button>("%ClearMapButton").Pressed += () => Confirm(
            "Clear saved map?",
            $"Do you really want to clear the saved map for {ServerLabel}?",
            "",
            "Every area explored on this server, by all of your characters, will be forgotten.",
            ClearSavedMap);
        GetNode<Button>("%OpenDataFolderButton").Pressed += () => OpenFolder(DataPaths.PlayerData());
        GetNode<Button>("%OpenOverridesFolderButton").Pressed += () => OpenFolder(DataPaths.PlayerOverrides());

        nameField.TextChanged += OnNameChanged;
        descriptionField.TextChanged += OnDescriptionChanged;
        classOption.ItemSelected += _ => ShowSelectedDescription(classOption);
        gameModeOption.ItemSelected += _ => ShowSelectedDescription(gameModeOption);
        seasonOption.ItemSelected += _ => ShowSelectedDescription(seasonOption);

        windowTypeOption.ItemSelected += _ => ChangeSetting(
            () => settings.Windowed = windowTypeOption.GetSelectedId() == WindowedOptionId,
            ApplyWindowMode);
        shadowsCheck.Toggled += pressed => ChangeSetting(() => settings.Shadows = pressed);
        soundCheck.Toggled += pressed => ChangeSetting(() => settings.SoundEnabled = pressed);
        darkGuiCheck.Toggled += pressed => ChangeSetting(() => settings.DarkMode = pressed);
        volumeSlider.ValueChanged += value => ChangeSetting(
            () => settings.SoundVolume = (int)value,
            () => volumeValue.Text = settings.SoundVolume.ToString());

        loadDialog.FileSelected += OnLoadFileSelected;
        saveDialog.FileSelected += OnSaveFileSelected;
        confirmDialog.Confirmed += OnConfirmed;
        confirmDialog.Canceled += () => pendingConfirmation = null;
        confirmDialog.VisibilityChanged += () => KeepButtonsTogetherWhenShown(confirmDialog);
        updateWindow.VisibilityChanged += () => KeepButtonsTogetherWhenShown(updateWindow);
        rollbackWindow.VisibilityChanged += () => KeepButtonsTogetherWhenShown(rollbackWindow);
        messageDialog.VisibilityChanged += ShowModalDim;
        confirmDialog.VisibilityChanged += ShowModalDim;
        updateWindow.VisibilityChanged += ShowModalDim;
        rollbackWindow.VisibilityChanged += ShowModalDim;
        StartIfAsked();
    }

    private void ShowOptions()
    {
        windowTypeOption.Select(windowTypeOption.GetItemIndex(settings.Windowed ? WindowedOptionId : 0));
        shadowsCheck.ButtonPressed = settings.Shadows;
        soundCheck.ButtonPressed = settings.SoundEnabled;
        darkGuiCheck.ButtonPressed = settings.DarkMode;
        volumeSlider.Value = settings.SoundVolume;
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

    private void AddDescriptions()
    {
        AddDescriptions(classOption, CharacterChoiceText.ClassDescriptions);
        AddDescriptions(gameModeOption, CharacterChoiceText.GameModeDescriptions);
        AddDescriptions(seasonOption, CharacterChoiceText.SeasonDescriptions);
    }

    private static void AddDescriptions<TChoice>(OptionButton option, IReadOnlyDictionary<TChoice, string> descriptions)
        where TChoice : struct, Enum
    {
        foreach (var (choice, description) in descriptions)
        {
            var index = option.GetItemIndex(Convert.ToInt32(choice));
            if (index >= 0)
            {
                option.SetItemTooltip(index, description);
            }
        }
    }

    private static void ShowSelectedDescription(OptionButton option) =>
        option.TooltipText = option.Selected >= 0 ? option.GetItemTooltip(option.Selected) : "";

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
            SelectOrAdd(classOption, (int)race.Class, CharacterChoiceText.ClassNames[race.Class]);
            SelectOrAdd(bodyOption, (int)race.Body, race.Body.ToString());
            SelectOrAdd(
                gameModeOption,
                (int)race.GameMode,
                CharacterChoiceText.GameModeNames.GetValueOrDefault(race.GameMode, race.GameMode.ToString()));
            SelectOrAdd(seasonOption, (int)race.Season, race.Season.ToString());
        }
        else
        {
            RemoveLockedItems(classOption, selectableClassCount);
            RemoveLockedItems(gameModeOption, selectableGameModeCount);
        }

        foreach (var (label, option) in raceRows)
        {
            option.Disabled = existingCharacter;
            label.Modulate = existingCharacter ? LockedRowColour : Colors.White;
            ShowSelectedDescription(option);
        }

        showingCharacter = false;
    }

    private static void SelectOrAdd(OptionButton option, int id, string name)
    {
        if (option.GetItemIndex(id) < 0)
        {
            option.AddItem(name, id);
        }

        option.Select(option.GetItemIndex(id));
    }

    private static void RemoveLockedItems(OptionButton option, int selectableCount)
    {
        while (option.ItemCount > selectableCount)
        {
            option.RemoveItem(option.ItemCount - 1);
        }

        if (option.Selected < 0)
        {
            option.Select(0);
        }
    }

    private void ResetNewCharacterChoices()
    {
        classOption.Select(classOption.GetItemIndex((int)CharacterClass.Templar));
        bodyOption.Select(bodyOption.GetItemIndex((int)Body.Masculine));
        gameModeOption.Select(gameModeOption.GetItemIndex((int)GameMode.Normal));
        seasonOption.Select(seasonOption.GetItemIndex((int)Season.Current));
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
            ShowMessage("FYI", "Could not read or save this computer's unique id.");
            return;
        }

        var newCharacterRace = new CharacterRace(
            (CharacterClass)classOption.GetSelectedId(),
            (Body)bodyOption.GetSelectedId(),
            (GameMode)gameModeOption.GetSelectedId(),
            (Season)seasonOption.GetSelectedId());
        var handshake = new LoginHandshake(
            record.Key,
            newCharacterRace.ToNumber(),
            passwordField.Text,
            uniqueId.Low,
            uniqueId.High);
        var connection = new ServerConnection();
        startingSession = new LoginHandoff(record, handshake, settings.Server, connection);
        connection.Start(settings.Server, handshake);
        ShowMessage("Connection", "STATUS: Connecting...");
        LetMessageClose(false);
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
            messageText.Text = $"STATUS: {message}";
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
            LetMessageClose(true);
        }
    }

    private void EndStarting()
    {
        startingSession = null;
        SetProcess(false);
    }

    private void LetMessageClose(bool canClose)
    {
        messageDialog.GetOkButton().Disabled = !canClose;
        messageDialog.DialogCloseOnEscape = canClose;
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
            ShowMessage("FYI", $"Could not open file {filePath}");
            return;
        }

        record.Changed = true;
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
            ShowMessage("FYI", $"Could not save file {filePath}");
            return;
        }

        RememberCharacterFile(filePath);
        ShowMessage("FYI", $"Saved file as {filePath}");
    }

    private void RememberCharacterFile(string filePath)
    {
        settings.CharacterHistory.Use(record.Key.Name, filePath);
        settings.Save();
        ShowPreviousCharacters();
    }

    private void ShowPreviousCharacters()
    {
        foreach (var child in previousCharacterList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var entry in settings.CharacterHistory.Entries)
        {
            var button = new Button
            {
                Text = entry.Name.Length > 0 ? entry.Name : Path.GetFileNameWithoutExtension(entry.FilePath),
                TooltipText = entry.FilePath,
                CustomMinimumSize = PreviousCharacterButtonSize,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            };
            var filePath = entry.FilePath;
            button.Pressed += () => Import(filePath);
            previousCharacterList.AddChild(button);
        }
    }

    private void StartUpdates()
    {
        updateWindow.Fonts = Fonts;
        rollbackWindow.Fonts = Fonts;
        var updateButton = GetNode<UpdateButtonView>("%UpdateButton");
        var updater = Updaters.Choose(OS.GetCmdlineUserArgs());
        if (updater is PreviewUpdater preview)
        {
            preview.Applied += version =>
                ShowMessage("Preview", $"A real update would now restart the launcher on version {version}.");
        }
        else
        {
            updateButton.Restarting += () => GetTree().Quit();
        }

        updateButton.Start(updater, updateWindow, rollbackWindow);
    }

    private void ClearSavedMap() =>
        ShowMessage(
            "FYI",
            ExploredMapKeeper.ClearSaved(settings.Server)
                ? $"Saved map data for {ServerLabel} has been cleared."
                : $"Could not clear the saved map data for {ServerLabel}.");

    private void OpenFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't create the folder {folder}: {exception.Message}");
            ShowMessage("FYI", $"Could not open the folder {folder}");
            return;
        }

        var opened = OS.ShellOpen(folder);
        if (opened != Error.Ok)
        {
            GD.PushError($"Couldn't open the folder {folder}: {opened}");
            ShowMessage("FYI", $"Could not open the folder {folder}");
        }
    }

    private void ShowMessage(string title, string text)
    {
        messageTitle.Text = title;
        messageText.Text = text;
        messageDialog.PopupCentered();
    }

    private void ConfirmTwice(string question, Action onYes) =>
        Confirm(
            "Are you sure?",
            question,
            "Your old account will no longer be accessible, unless you remembered to save it.",
            "You did save it, didn't you?",
            () => Confirm(
                "Are you really sure?",
                "No Really!",
                "This will replace your current character.",
                "This action cannot be undone.",
                onYes));

    private void Confirm(string title, string question, string explanation, string warning, Action onYes)
    {
        pendingConfirmation = onYes;
        confirmTitle.Text = title;
        confirmQuestion.Text = question;
        confirmExplanation.Text = explanation;
        confirmExplanation.Visible = explanation.Length > 0;
        confirmWarning.Text = warning;
        confirmDialog.CallDeferred(Window.MethodName.PopupCentered);
    }

    private void ShowModalDim() =>
        modalDim.Visible = messageDialog.Visible || confirmDialog.Visible || updateWindow.Visible
            || rollbackWindow.Visible;

    private static void KeepButtonsTogetherWhenShown(ConfirmationDialog dialog)
    {
        if (dialog.Visible)
        {
            Callable.From(() => KeepButtonsTogether(dialog)).CallDeferred();
        }
    }

    private static void KeepButtonsTogether(ConfirmationDialog dialog)
    {
        var buttonRow = dialog.GetChildren(true)
            .OfType<HBoxContainer>()
            .First(row => row.IsAncestorOf(dialog.GetOkButton()));
        buttonRow.AddThemeConstantOverride("separation", DialogButtonGap);
        var okIndex = dialog.GetOkButton().GetIndex();
        var cancelIndex = dialog.GetCancelButton().GetIndex();
        for (var childIndex = 0; childIndex < buttonRow.GetChildCount(); childIndex++)
        {
            if (buttonRow.GetChild(childIndex) is Control spacer and not Button)
            {
                spacer.Visible = childIndex < Math.Min(okIndex, cancelIndex)
                    || childIndex > Math.Max(okIndex, cancelIndex);
            }
        }
    }

    private void OnConfirmed()
    {
        var confirmed = pendingConfirmation;
        pendingConfirmation = null;
        confirmed?.Invoke();
    }
}
