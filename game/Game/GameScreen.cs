using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Audio;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Hud;
using TheLastGate.Items;
using TheLastGate.Map;
using TheLastGate.Net;
using TheLastGate.Planner;
using TheLastGate.State;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Game;

public partial class GameScreen : Control
{
    private const string LauncherScene = "res://Launcher/launcher.tscn";
    private const string SpriteProbeScene = "res://World/sprite_probe.tscn";
    private const long SpriteCacheBudgetBytes = 256L * 1024 * 1024;
    private const int WorldViewIndex = 1;
    private const int ErrorFont = 0;
    private const int StatusFont = 1;

    private readonly GameSession gameSession;
    private readonly GameState gameState = new();
    private readonly NavigationKeys navigationKeys = new();
    private readonly TickPacer pacer = new();
    private readonly HashSet<string> reportedUnusualData = [];
    private readonly Stopwatch sessionClock = Stopwatch.StartNew();
    private ServerConnection connection = null!;
    private ExploredMapKeeper? exploredMap;
    private HudView hudView = null!;
    private ConnectionState lastConnectionState;
    private LoginHandoff login = null!;
    private MapNotesKeeper? mapNotes;
    private bool newCharacterSaved;
    private TickPlayback playback = null!;
    private TickProcessor processor = null!;
    private ScreenGammaLayer? screenGamma;
    private HashSet<DisplaySwap> shownSwaps = [];
    private SoundEffects soundEffects = null!;
    private SpriteLoader? sprites;
    private UserDataUpload? userDataUpload;
    private WorldView? worldView;

    public GameScreen() => gameSession = new GameSession(gameState);

    public override void _Ready()
    {
        navigationKeys.TurnOff();
        hudView = GetNode<HudView>("%HudView");
        soundEffects = GetNode<SoundEffects>("%SoundEffects");
        hudView.LeaveRequested += ReturnToLauncher;

        if (LoginHandoff.Take() is not { } startedLogin)
        {
            SetProcess(false);
            ReturnToLauncher();
            return;
        }

        login = startedLogin;
        connection = startedLogin.Connection;
        CursorShapes.Register(ClientSettings.Load().CursorScale);
        OpenExploredMap();
        AddWorldView();
        playback = new TickPlayback(pacer);
        processor = new TickProcessor(gameState);

        userDataUpload = UserDataUpload.For(login.Record, gameState.Chat);
        gameState.Player.NameReceived += SaveCharacterName;
        gameState.UniqueIdReceived += UniqueIdStore.Save;
        gameState.ExitRequested += LeaveOnServerExit;
        gameState.UnusualDataSeen += ReportUnusualDataOnce;
        gameSession.MapFrames.UnusualDataSeen += ReportUnusualDataOnce;
    }

    public override void _Process(double delta)
    {
        while (connection.TryDequeueMessage(out var message))
        {
            gameState.Chat.Add(new LogLine(message, StatusFont));
        }

        SaveMapsWhenTheConnectionEnds();
        exploredMap?.Advance(TimeSpan.FromSeconds(delta));

        if (!newCharacterSaved && connection.State == ConnectionState.Connected
            && login.Handshake.CreatedKey is { } createdKey)
        {
            SaveNewCharacter(createdKey);
        }

        while (connection.TryDequeueTick(out var tick))
        {
            playback.Receive(tick);
        }

        try
        {
            var ticksPlayed = 0;
            playback.PlayDueTicks(
                NowMilliseconds(),
                payload =>
                {
                    processor.Process(payload);
                    gameSession.SessionCurrencies.Observe(gameState.Player, sessionClock.Elapsed);
                    ticksPlayed++;
                    SendUserData();
                },
                connection.SendCommand);
            if (ticksPlayed > 0)
            {
                if (sprites != null)
                {
                    exploredMap?.See(gameState.Map, gameSession.MapFrames, sprites);
                }

                worldView?.Refresh();
                worldView?.RecordMovement(ticksPlayed, TickMilliseconds());
                hudView.Refresh(worldView?.SelectedCharacter ?? 0);
            }
        }
        catch (ProtocolDesyncException exception)
        {
            gameState.Chat.Add(new LogLine($"Lost track of the server's data: {exception.Message}", ErrorFont));
            connection.Disconnect();
        }
    }

    public override void _ExitTree()
    {
        navigationKeys.TurnOn();
        exploredMap?.Save();
        mapNotes?.Save();
        CursorShapes.Clear();
        connection?.Dispose();
        sprites?.Dispose();
    }

    private void OpenExploredMap()
    {
        var keeper = new ExploredMapKeeper(login.Server);
        gameState.MinimapBoxCleared += keeper.Recorder.ClearBox;
        gameState.SpectatingStarted += () => keeper.Recorder.Paused = true;
        gameState.SpectatingStopped += () => keeper.Recorder.Paused = false;
        exploredMap = keeper;
        mapNotes = new MapNotesKeeper(login.Server);
    }

    // The current client saves its maps when the connection fails, in case the game is closed straight after.
    private void SaveMapsWhenTheConnectionEnds()
    {
        var connectionState = connection.State;
        if (connectionState != lastConnectionState
            && connectionState is ConnectionState.Closed or ConnectionState.Failed)
        {
            exploredMap?.Save();
            mapNotes?.Save();
        }

        lastConnectionState = connectionState;
    }

    private void ApplyClientSettings(ClientSettings settings)
    {
        soundEffects.Enabled = settings.SoundEnabled;
        soundEffects.Volume = settings.SoundVolume;
        CursorShapes.Register(settings.CursorScale);
        gameState.ItemLooks.Enabled = settings.ItemTooltips;
        gameSession.UseQueue.Enabled = settings.UseQueue;
        gameSession.GiveMore.Enabled = settings.GiveMore;
        pacer.Adaptive = settings.AdaptivePacing;
        screenGamma?.ShowGamma(settings.ScreenGamma);
        if (worldView != null)
        {
            worldView.GlideMovement = settings.GlideMovement;
            worldView.PickTileDrawnUnderMouse = settings.PickTileDrawnUnderMouse;
            worldView.WorldGamma = settings.WorldGamma;
        }

        ShowSwaps(settings);
    }

    private void ShowSwaps(ClientSettings settings)
    {
        if (sprites == null || shownSwaps.SetEquals(settings.SwapsOn))
        {
            return;
        }

        shownSwaps = [.. settings.SwapsOn];
        sprites.Swaps = DisplaySwaps.SpritesFor(shownSwaps);
        worldView?.RedrawSprites();
    }

    private static long NowMilliseconds() => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

    private double TickMilliseconds() =>
        pacer.MeasuredPeriodMilliseconds >= 1 ? pacer.MeasuredPeriodMilliseconds : TickPacer.NominalTickMilliseconds;

    private void AddWorldView()
    {
        try
        {
            sprites = new SpriteLoader(DataPaths.Root(), SpriteCacheBudgetBytes);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"Couldn't open the sprite archives: {exception.Message}");
            return;
        }

        var fonts = new ClassicFonts(sprites);
        var characterSettings = login.Record.Settings();
        var clientSettings = ClientSettings.Load();
        worldView = new WorldView
        {
            State = gameState,
            Session = gameSession,
            Sprites = sprites,
            Fonts = fonts,
            SendCommand = SendAction,
            PlayClick = soundEffects.PlayClick,
            PickTileDrawnUnderMouse = clientSettings.PickTileDrawnUnderMouse,
            GlideMovement = clientSettings.GlideMovement,
            WorldGamma = clientSettings.WorldGamma,
            ShowShadows = clientSettings.Shadows,
            ShowNames = characterSettings.ShowNames,
            ShowHealthPercent = characterSettings.ShowHealthPercent,
            ShowHealthBars = characterSettings.ShowHealthBars,
            HideWalls = characterSettings.HideWalls,
        };
        AddChild(worldView);
        MoveChild(worldView, WorldViewIndex);
        ShowSwaps(clientSettings);
        hudView.DarkMode = clientSettings.DarkMode;
        hudView.Sprites = sprites;
        hudView.Settings = characterSettings;
        hudView.ClientSettings = clientSettings;
        hudView.PlannerChoices = new PlannerChoicesKeeper(() => login.Record.Key.UserNumber);
        hudView.ClientSettingsChanged += () => ApplyClientSettings(clientSettings);
        hudView.SettingsChanged += () => ApplyCharacterSettings(characterSettings);
        hudView.PlayClick = soundEffects.PlayClick;
        hudView.StartAreaUse = worldView.StartAreaUse;
        hudView.ReadDebugFacts = ReadDebugFacts;
        hudView.ExploredMap = exploredMap?.Recorder.Map;
        hudView.NotesSetAsideFile = mapNotes is { WasSetAside: true } ? mapNotes.FileName : null;
        hudView.MapNotes = mapNotes?.Notes;
        hudView.SaveMapNotes = mapNotes == null ? null : mapNotes.Save;
        hudView.Show(gameState, gameSession, fonts, SendAction);
        soundEffects.Start(DataPaths.Root(), clientSettings.SoundEnabled, clientSettings.SoundVolume);
        gameState.SoundRequested += soundEffects.PlayServerSound;
        gameState.ItemLooks.SendLook = connection.SendCommand;
        gameState.ItemLooks.Enabled = clientSettings.ItemTooltips;
        gameSession.UseQueue.Enabled = clientSettings.UseQueue;

        // A queued use has already clicked when it was queued.
        gameSession.UseQueue.Send = connection.SendCommand;
        gameSession.GiveMore.Enabled = clientSettings.GiveMore;
        pacer.Adaptive = clientSettings.AdaptivePacing;
        gameSession.GiveMore.Send = SendAction;
        gameState.ShopRefresh.Send = connection.SendCommand;
        gameState.NameRequests.Send = connection.SendCommand;
        gameSession.AutoWalk.Send = connection.SendCommand;
        gameState.BuffWatch.Say = text =>
        {
            foreach (var packet in ChatPackets.Say(text))
            {
                connection.SendCommand(packet);
            }
        };
        screenGamma = new ScreenGammaLayer();
        AddChild(screenGamma);
        screenGamma.ShowGamma(clientSettings.ScreenGamma);
        if (SpriteProbeView.IsAvailable)
        {
            AddSpriteProbe(worldView, fonts);
        }
    }

    private void AddSpriteProbe(WorldView world, IFontSet fonts)
    {
        var probe = GD.Load<PackedScene>(SpriteProbeScene).Instantiate<SpriteProbeView>();
        probe.World = world;
        AddChild(probe);
        probe.UseFont(fonts.For(TextColour.White));
    }

    private DebugFacts ReadDebugFacts() => new(
        typeof(GameScreen).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "",
        login.Handshake.ServerVersion,
        Engine.GetVersionInfo()["string"].AsString(),
        RenderingServer.GetVideoAdapterName(),
        RenderingServer.GetVideoAdapterVendor(),
        RenderingServer.GetVideoAdapterApiVersion(),
        playback.QueuedTicks,
        pacer.BufferTarget,
        pacer.MeasuredPeriodMilliseconds);

    private void SendAction(byte[] packet)
    {
        soundEffects.PlayClickFor(packet);
        gameSession.UseQueue.Sent(packet);
        if (ItemLooks.IsManualLook(packet))
        {
            gameState.ItemLooks.ManualLookSent();
        }

        // Using or looking at something lets the next window the server opens stay open.
        if ((ClientCommand)packet[0] is ClientCommand.Use or ClientCommand.Look)
        {
            gameSession.Pause.Cancel();
        }

        connection.SendCommand(packet);
    }

    // Once sent, the character is no longer changed, so the next login doesn't send it again.
    private void SendUserData()
    {
        if (userDataUpload?.Tick() is not { } packet)
        {
            return;
        }

        connection.SendCommand(packet);
        if (userDataUpload.Done)
        {
            userDataUpload = null;
            login.Record.Changed = false;
            CharacterStore.Save(login.Record);
        }
    }

    private void SaveNewCharacter(LoginKey createdKey)
    {
        newCharacterSaved = true;
        var record = login.Record;
        record.Key = createdKey;
        var settings = record.Settings();
        settings.SetStartingHotbar(CharacterRace.FromNumber(createdKey.Race).Class);
        record.Store(settings);
        CharacterStore.Save(record);
    }

    private void ApplyCharacterSettings(CharacterSettings settings)
    {
        if (worldView != null)
        {
            worldView.ShowNames = settings.ShowNames;
            worldView.ShowHealthPercent = settings.ShowHealthPercent;
            worldView.ShowHealthBars = settings.ShowHealthBars;
            worldView.HideWalls = settings.HideWalls;
            worldView.Refresh();
        }

        login.Record.Store(settings);
        CharacterStore.Save(login.Record);
    }

    private void SaveCharacterName()
    {
        var record = login.Record;
        record.Key = record.Key with { Name = gameState.Player.Name, Race = gameState.Player.Race };
        CharacterStore.Save(record);
    }

    private void ReportUnusualDataOnce(string description)
    {
        if (reportedUnusualData.Add(description))
        {
            GD.PushWarning(description);
        }
    }

    private void LeaveOnServerExit(ServerExit exit)
    {
        gameState.Chat.Add(new LogLine(" ", StatusFont));
        gameState.Chat.Add(new LogLine($"EXIT: {exit.Reason}", StatusFont));
        connection.Disconnect();
        hudView.ServerEnded(exit.Code);
    }

    private void ReturnToLauncher() => GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, LauncherScene);
}
