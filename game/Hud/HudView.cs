using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Chat;
using TheLastGate.Config;
using TheLastGate.Cursors;
using TheLastGate.Game;
using TheLastGate.Hotbar;
using TheLastGate.Hotkeys;
using TheLastGate.Items;
using TheLastGate.Map;
using TheLastGate.Net;
using TheLastGate.Notes;
using TheLastGate.Planner;
using TheLastGate.Profiling;
using TheLastGate.State;
using TheLastGate.Stats;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Hud;

[Tool]
public partial class HudView : Control
{
    private const long EditorCacheBudgetBytes = 64L * 1024 * 1024;
    private const int ReservedSlot = 4;
    private const int MaximumSlot = 5;
    private const string StrongholdName = "Stronghold";
    private const string CasinoName = "Casino";
    private const string ContractName = "Contract";
    private const string SwapSetHint = "Swap equipment with a stored alternate set.";
    private const string TrashCanName = "TrashCan";
    private const string ItemTooltipBinding = "item_tooltip_hotkey";
    private const string WorldMapWindowScenePath = "res://Map/world_map_window.tscn";
    private const string PlannerWindowScenePath = "res://Planner/planner_window.tscn";
    private const string SessionStatsScenePath = "res://Hud/session_stats_window.tscn";
    private const string PlannerAdvancedScenePath = "res://Planner/planner_advanced_window.tscn";
    private const string TimingsScenePath = "res://Profiling/timings_view.tscn";
    private const int HintFont = 1;
    private const int WarningFont = 0;
    private const int HitPointsRow = 5;
    private const int ManaRow = 6;
    private const int HitScoreMeta = 5;
    private const int ParryScoreMeta = 6;

    private const int ExpandedQuickStatsPicture = 2;
    private const int CompactQuickStatsPicture = 3;
    private const int DarkPictureOffset = 10000;
    private const float ExperienceBarTop = 71;
    private const float HiddenQuickStatsRows = 28;
    private static readonly Vector2 HeldItemOffset = new(16, 16);

    private static readonly (string Button, Func<TextWindows, TextWindowAction> Click)[] TextWindowButtons =
    [
        ("CloseButton", windows => windows.Close()),
        ("OkButton", windows => windows.Ok()),
        ("LeftButton", windows => windows.Left()),
        ("RightButton", windows => windows.Right()),
    ];

    private static readonly Vector2 OffScreen = new(-1, -1);

    private readonly DepotPage depotPage = new();
    private readonly ExitSequence exit = new();
    private readonly FocusCache focusCache = new();
    private readonly LookPanel lookPanel = new();
    private readonly byte[] minimapPixels = new byte[MinimapView.Pixels * MinimapView.Pixels * 4];
    private readonly MinimapView minimapView = new();
    private readonly SkillShortcut skillShortcut = new();
    private ClientSettings? clientSettings;

    private bool darkMode;
    private SpriteLoader? editorSprites;
    private IFontSet? fonts;
    private GameSession? gameSession;
    private int? hoveredShopCell;
    private (ClickArea Area, Func<MouseCursor> Cursor)? hoveredSlot;
    private bool itemTooltipsShown = true;
    private NoteSet? mapNotes;
    private int mapNotesStamp;
    private Image? minimapImage;
    private (int Revision, int X, int Y, int Magnification, int NotesStamp)? minimapShown;
    private ImageTexture? minimapTexture;
    private Vector2 mousePosition;
    private bool notesNoticeShown;
    private PlannerAdvancedWindowView? plannerAdvanced;
    private PlannerWindowView? plannerWindow;
    private int selectedCharacter;
    private Action<byte[]> sendCommand = _ => { };
    private SessionStatsWindowView? sessionStatsWindow;
    private SpriteLoader? sprites;
    private StatPlan statPlan = new();
    private GameState? state;
    private TimingsView? timingsView;
    private WorldMapWindowView? worldMapWindow;

    [Export]
    public bool DarkMode
    {
        get => darkMode;
        set
        {
            darkMode = value;
            ShowContext();
        }
    }

    public SpriteLoader? Sprites
    {
        get => sprites;
        set
        {
            sprites = value;
            ShowContext();
        }
    }

    public CharacterSettings Settings { get; set; } = new();

    public ClientSettings? ClientSettings
    {
        get => clientSettings;
        set
        {
            clientSettings = value;
            ShowContext();
        }
    }

    public PlannerChoicesKeeper? PlannerChoices { get; set; }

    public IFontSet? Fonts
    {
        get => fonts;
        set
        {
            fonts = value;
            ApplyWindowFonts(this);
            ShowContext();
        }
    }

    public Action PlayClick { get; set; } = () => { };

    public Action StartAreaUse { get; set; } = () => { };

    public Func<DebugFacts>? ReadDebugFacts { get; set; }

    public ExploredMap? ExploredMap { get; set; }

    public string? NotesSetAsideFile { get; set; }

    public NoteSet? MapNotes
    {
        get => mapNotes;
        set
        {
            mapNotes = value;
            mapNotesStamp++;
        }
    }

    public Action? SaveMapNotes { get; set; }

    private bool PlannerEnabled => ClientSettings?.SkillPlanner == true;

    private PlayerState Player => state!.Player;

    private bool HoldingItem => Player.HeldItem.Sprite != 0;

    private HudContext Context => new(fonts, sprites, clientSettings, darkMode);

    // Each slot says it is hovered through its own mouse signals, so the HUD never works it out from the tree.
    private void WatchCursor(ClickArea area, Func<MouseCursor> cursor)
    {
        area.MouseEntered += () => hoveredSlot = (area, cursor);
        area.MouseExited += () =>
        {
            if (hoveredSlot?.Area == area)
            {
                hoveredSlot = null;
            }
        };
    }

    private MouseCursor ShopCursor(int slot)
    {
        var items = state!.Shop.ShopItems;
        return ShopClicks.Cursor(slot < items.Length && items[slot].Sprite != 0, HoldingItem);
    }

    public event Action? SettingsChanged;

    public event Action? LeaveRequested;

    public event Action? ClientSettingsChanged;

    public void Show(GameState shownState, GameSession shownSession, IFontSet fontSet, Action<byte[]> sendToServer)
    {
        state = shownState;
        gameSession = shownSession;
        statPlan = shownSession.StatPlan;
        sendCommand = sendToServer;
        for (var slot = WornSlot.Head; slot <= WornSlot.SecondCharm; slot++)
        {
            var clickedSlot = slot;
            var wornView = GetNode<ItemSlotView>("%" + slot);
            wornView.Released += button => ClickWorn(clickedSlot, button);
            wornView.TooltipRequest = () => WornTooltip(clickedSlot);
            wornView.MouseEntered += () => state?.ItemLooks.Prefer(ItemSlotKind.Worn, (int)clickedSlot);
            wornView.MouseExited += () => state?.ItemLooks.ClearPreference();
            WatchCursor(
                wornView,
                () => EquipmentClicks.Cursor(
                    Player.Worn[(int)clickedSlot].Sprite != 0,
                    HoldingItem,
                    HeldKeys.Current()));
        }

        GetNode<ClickArea>("%SwapSetButton").Released += ClickSwapSet;
        var trashCan = GetNode<ClickArea>("%" + TrashCanName);
        trashCan.Released += ClickTrashCan;
        WatchCursor(trashCan, () => TrashCan.Cursor(HoldingItem));
        foreach (var node in GetNode("%OptionButtons").GetChildren())
        {
            var optionButton = (OptionButtonView)node;
            optionButton.Released += button => ClickOption(optionButton.Kind, button);
        }

        foreach (var node in GetNode("%MoneyButtons").GetChildren())
        {
            var moneyButton = (MoneyButton)node;
            moneyButton.Released += button => ClickMoney(moneyButton.Silver, button);
        }

        foreach (var node in GetNode("%StatRows").GetChildren().Concat(GetNode("%SkillRows").GetChildren()))
        {
            var row = (StatRow)node;
            row.GetNode<ClickArea>("NameArea").Released += button => ClickStatName(row, button);
            row.GetNode<ClickArea>("RaiseArea").Released += button => ClickStat(row, true, button);
            row.GetNode<ClickArea>("LowerArea").Released += button => ClickStat(row, false, button);
        }

        foreach (var node in GetNode("%StatModeButtons").GetChildren())
        {
            var modeButton = (StatModeButton)node;
            modeButton.Released += button => ClickStatMode(modeButton.Mode, button);
            modeButton.MouseEntered += AskForMetaText;
        }

        GetNode<ClickArea>("%UpdateButton").Released += ClickUpdate;
        foreach (var node in GetNode("%Hotbar").GetChildren())
        {
            var hotbarButton = (HotbarButtonView)node;
            hotbarButton.Released += button => ClickHotbar(hotbarButton.Slot, button);
        }

        GetNode<HotbarMenu>("%HotbarMenu").SkillChosen += ChooseHotbarSkill;
        var inventory = GetNode<InventoryGrid>("%Inventory");
        for (var cell = 0; cell < InventoryScroll.VisibleSlots; cell++)
        {
            var clickedCell = cell;
            var cellView = inventory.GetChild<ItemSlotView>(cell);
            cellView.TooltipRequest = () => InventoryTooltip(inventory.SlotShownIn(clickedCell));
            cellView.MouseEntered += () =>
                state?.ItemLooks.Prefer(ItemSlotKind.Inventory, inventory.SlotShownIn(clickedCell));
            cellView.MouseExited += () => state?.ItemLooks.ClearPreference();
            cellView.Released +=
                button => ClickInventory(inventory.SlotShownIn(clickedCell), button);
            WatchCursor(
                cellView,
                () => InventoryClicks.Cursor(
                    Player.Inventory[inventory.SlotShownIn(clickedCell)],
                    HoldingItem,
                    HeldKeys.Current()));
        }

        state.LookShown += ShowLook;
        Fonts = fontSet;
        GetNode<ChatLog>("%ChatLog").Show(state.Chat, fontSet);
        var chatLine = GetNode<ChatLine>("%ChatLine");
        chatLine.LineSent += Say;
        chatLine.EscapePressed += PressEscape;
        var shopWindow = GetNode<ShopWindowView>("%ShopWindow");
        shopWindow.GetNode<ClickArea>("CloseButton").Released += ClickCloseShop;
        foreach (var node in shopWindow.GetNode("Grid").GetChildren())
        {
            var shopCell = (ItemSlotView)node;
            var cell = shopCell.GetIndex();
            shopCell.Released += button => ClickShop(cell, button);
            shopCell.MouseEntered += () => hoveredShopCell = cell;
            shopCell.MouseExited += () => hoveredShopCell = null;
            WatchCursor(shopCell, () => ShopCursor(ShopClicks.SlotFor(cell)));
        }

        var depotWindow = GetNode<DepotWindowView>("%DepotWindow");
        depotWindow.GetNode<ClickArea>("CloseButton").Released += ClickCloseShop;
        depotWindow.GetNode<Control>("Blocker").GuiInput += ScrollDepot;
        foreach (var node in depotWindow.GetNode("Blocker/Grid").GetChildren())
        {
            var depotCell = (ItemSlotView)node;
            var cell = depotCell.GetIndex();
            depotCell.Released += button => ClickDepot(cell, button);
            WatchCursor(
                depotCell,
                () => ShopClicks.Cursor(state.Shop.Depot[depotPage.Current][cell].Sprite != 0, HoldingItem));
        }

        for (var page = 0; page < DepotPackets.Pages; page++)
        {
            var shownPage = page;
            var pageButton = depotWindow.GetNode<ClickArea>($"Blocker/PageButton{page}");
            pageButton.Released += button => ClickDepotPage(shownPage, button);
        }

        var smithWindow = GetNode<SmithWindowView>("%SmithWindow");
        smithWindow.GetNode<ClickArea>("CloseButton").Released += ClickCloseShop;
        foreach (var (node, slot) in SmithWindowView.ItemSlots)
        {
            smithWindow.GetNode<ItemSlotView>(node).Released += button => ClickSmithItem(slot, button);
        }

        for (var number = SmithClicks.FirstButton; number <= SmithClicks.LastButton; number++)
        {
            var buttonNumber = number;
            var smithButton = smithWindow.GetNode<ClickArea>($"Button{number}");
            smithButton.Released += button => ClickSmithButton(buttonNumber, button);
        }

        var waypointWindow = GetNode<WaypointWindowView>("%WaypointWindow");
        waypointWindow.GetNode<ClickArea>("CloseButton").Released += ClickCloseWaypoints;
        waypointWindow.ScrollBar.ValueChanged += DragWaypoints;
        waypointWindow.GetNode<Control>("Blocker").GuiInput += ScrollWaypoints;
        waypointWindow.SelectedChosen += ChooseSelectedWaypoint;
        waypointWindow.CloseAsked += () => ClickCloseWaypoints(MapButton.Left);
        for (var row = 0; row < WaypointWindow.VisibleRows; row++)
        {
            var shownRow = row;
            ((ClickArea)waypointWindow.Row(row)).Released += button => ClickWaypoint(shownRow, button);
        }

        var skillTreeWindow = GetNode<SkillTreeWindowView>("%SkillTreeWindow");
        skillTreeWindow.GetNode<ClickArea>("CloseButton").Released += ClickCloseSkillTree;
        for (var node = 0; node < SkillTrees.Nodes; node++)
        {
            var shownNode = node;
            skillTreeWindow.Node(node).Released += button => ClickSkillTreeNode(shownNode, button);
        }

        GetNode<ClickArea>("%TreeButton").Released += ClickTreeButton;
        var minimap = GetNode<ClickArea>("%Minimap");
        minimap.Released += button => ClickMinimap(MinimapButton.Map, button);
        minimap.GuiInput += ScrollMinimap;
        minimap.TooltipFor = MinimapPinName;
        GetNode<ClickArea>("%MinimapZoomIn").Released += button => ClickMinimap(MinimapButton.ZoomIn, button);
        GetNode<ClickArea>("%MinimapZoomOut").Released += button => ClickMinimap(MinimapButton.ZoomOut, button);
        worldMapWindow = GD.Load<PackedScene>(WorldMapWindowScenePath).Instantiate<WorldMapWindowView>();
        AddChild(worldMapWindow);
        worldMapWindow.NotesChanged += MapNotesChanged;
        plannerWindow = GD.Load<PackedScene>(PlannerWindowScenePath).Instantiate<PlannerWindowView>();
        AddChild(plannerWindow);
        plannerWindow.CommitPressed += CommitPlannerPlan;
        plannerWindow.ResetPressed += ResetPlan;
        plannerWindow.ClosePressed += ClosePlanner;
        plannerWindow.RowClicked += ChangePlan;
        plannerWindow.WeaponChosen += weapon => ChangePlannerChoices(choices => choices.Weapon = weapon);
        plannerWindow.OffhandChosen += offhand => ChangePlannerChoices(choices => choices.Offhand = offhand);
        plannerWindow.FocusClicked += ToggleFocus;
        plannerWindow.PlanItPressed += PlanFocus;
        plannerWindow.AdvancedPressed += OpenPlannerAdvanced;
        plannerAdvanced = GD.Load<PackedScene>(PlannerAdvancedScenePath).Instantiate<PlannerAdvancedWindowView>();
        AddChild(plannerAdvanced);
        plannerAdvanced.ClosePressed += ClosePlannerAdvanced;
        plannerAdvanced.ChoiceChanged += ChangePlannerChoices;
        sessionStatsWindow = GD.Load<PackedScene>(SessionStatsScenePath).Instantiate<SessionStatsWindowView>();
        AddChild(sessionStatsWindow);
        sessionStatsWindow.ResetPressed += ResetSessionStats;
        sessionStatsWindow.ClosePressed += () => ShowSessionStats(false);
        ShowSessionStats(false);
        GetNode<ClickArea>("%PlannerButton").Released += ClickPlannerButton;
        var optionsWindow = GetNode<OptionsWindowView>("%OptionsWindow");
        optionsWindow.TabChosen += ChooseOptionsTab;
        optionsWindow.CloseChosen += CloseOptions;
        optionsWindow.SoundEnabledChanged +=
            enabled => ChangeClientSettings(settings => settings.SoundEnabled = enabled);
        optionsWindow.SoundVolumeChanged += volume => ChangeClientSettings(settings => settings.SoundVolume = volume);
        optionsWindow.OptionChanged += (option, isOn) => ChangeClientSettings(settings => settings[option] = isOn);
        optionsWindow.CursorScaleChanged += scale => ChangeClientSettings(settings => settings.CursorScale = scale);
        optionsWindow.CaptureChosen += id => ChangeOptionsWindow(window => window.StartCapture(id));
        optionsWindow.CaptureCancelled += () => ChangeOptionsWindow(window => window.CancelCapture());
        optionsWindow.CaptureUnbound += () => ChangeKeyBindings(window =>
        {
            if (window.UnbindCaptured() is { } change)
            {
                BindingActions.Apply([change]);
            }

            return null;
        });
        optionsWindow.KeyCaptured += chord => ChangeKeyBindings(window =>
        {
            var captured = window.Capture(chord, BindingActions.Current());
            BindingActions.Apply(captured?.Changes ?? []);
            return captured?.Line;
        });
        optionsWindow.BindingResetChosen += id => ChangeKeyBindings(_ =>
        {
            BindingActions.Apply([new BindingChange(id, BindingActions.Defaults()[id])]);
            return null;
        });
        optionsWindow.ResetChosen += () => ChangeKeyBindings(window =>
        {
            var line = window.ChooseReset();
            if (line != null)
            {
                ClientSettings?.Reset(window.Tab);
            }

            return line;
        });
        optionsWindow.ScreenGammaChanged += gamma => ChangeClientSettings(settings => settings.ScreenGamma = gamma);
        optionsWindow.WorldGammaChanged += gamma => ChangeClientSettings(settings => settings.WorldGamma = gamma);
        optionsWindow.BackgroundOpacityChanged += (window, opacity) =>
            ChangeClientSettings(settings => settings.BackgroundOpacity[window] = opacity);
        optionsWindow.AccessibilityChanged += (option, isOn) => ChangeClientSettings(settings =>
        {
            if (isOn)
            {
                settings.AccessibilityOn.Add(option);
            }
            else
            {
                settings.AccessibilityOn.Remove(option);
            }
        });

        var textWindows = GetNode<TextWindowView>("%TextWindows");
        foreach (var (buttonName, click) in TextWindowButtons)
        {
            textWindows.GetNode<ClickArea>(buttonName).Released += button => ClickTextWindow(button, click);
        }

        chatLine.GrabFocus();
        chatLine.EnterToTalk = ClientSettings?.EnterToTalk ?? false;
        Refresh(0);
    }

    public void Refresh(int selected)
    {
        using var timing = Timings.Measure("hud.refresh");
        selectedCharacter = selected;
        if (state == null || gameSession == null)
        {
            return;
        }

        var player = state.Player;
        ShowPool("HitPoints", player.ActiveHitPoints, player.HitPoints[MaximumSlot]);
        ShowPool("Endurance", player.ActiveEndurance, player.Endurance[MaximumSlot]);
        ShowPool("Mana", player.ActiveMana, player.Mana[MaximumSlot]);
        SetText("Money", Currencies.MoneyText(player.Gold));
        ShowPoints("Stronghold", StrongholdName, player.BlackStrongholdPoints);
        ShowPoints("Casino", CasinoName, player.Tokens);
        ShowPoints("Contract", ContractName, player.OsirisPoints);
        SetText("Location", player.Location);
        if (gameSession.TextWindows.AnyOpen || gameSession.WaypointWindow.IsOpen
            || gameSession.SkillTreeWindow.Open != null
            || state.OpenShop != null)
        {
            lookPanel.Hide();
        }

        ShowTextWindows();
        RefreshSessionStats();
        GetNode<ShopWindowView>("%ShopWindow").ShowItems(state);
        GetNode<DepotWindowView>("%DepotWindow").ShowItems(state, depotPage);
        GetNode<SmithWindowView>("%SmithWindow").ShowItems(state);
        ShowWaypoints();
        GetNode<SkillTreeWindowView>("%SkillTreeWindow")
            .ShowTree(gameSession.SkillTreeWindow, state.Player, state.Terminology);
        ShowOptions();
        if (lookPanel.Shown is { } look)
        {
            ShowPortrait(look);
        }
        else
        {
            ShowPortrait(player, gameSession.MapFrames.CharacterSprite(WorldMap.PlayerTile));
        }

        foreach (var node in GetNode("%OptionButtons").GetChildren())
        {
            var optionButton = (OptionButtonView)node;
            optionButton.ShowMarked(OptionButtons.IsMarked(optionButton.Kind, Settings, player.Mode));
        }

        foreach (var node in GetNode("%Hotbar").GetChildren())
        {
            var hotbarButton = (HotbarButtonView)node;
            var slot = hotbarButton.Slot;
            hotbarButton.ShowName(HotbarButtons.Name(Settings.HotbarActions[slot], Settings.HotbarNames[slot]));
            hotbarButton.ShowKey();
        }

        GetNode<InventoryGrid>("%Inventory").Show(player, Settings.HotbarActions);
        ShowStats(player);
        ShowQuickStats(player);
        ShowMinimap();
        ShowWorldMap();
        ShowPlanner();
        var buffIcons = BuffIcons.For(player.Buffs);
        GetNode<BuffIconsView>("%Buffs").Show(buffIcons.Buffs);
        GetNode<BuffIconsView>("%Debuffs").Show(buffIcons.Debuffs);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouse mouse)
        {
            mousePosition = mouse.Position;
        }

        if (inputEvent is InputEventMouseButton press)
        {
            TextFields.StopOnClickOutside(GetViewport(), press);
        }
    }

    // Only keys the chat line didn't take arrive here: an open line keeps its typing and editing keys, and a closed one
    // takes none. Held keys repeat their hotkey, and a key can run its hotkey and open the line too, as in the current
    // client.
    public override void _ShortcutInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        if (TextFields.BeingTypedIn(GetViewport()) is { } field)
        {
            if (TextFields.StopOnEscape(field, key))
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        var chatLine = GetNode<ChatLine>("%ChatLine");
        var handled = chatLine.RunsHotkeys && RunBoundHotkey(key);
        if (!chatLine.TakesTyping && ChatKeyFor(key.Keycode) is { } chatKey)
        {
            if (chatKey == ChatKey.Escape)
            {
                PressEscape();
                handled = true;
            }
            else
            {
                handled |= chatLine.Open(chatKey);
            }
        }

        if (LogPagesFor(key.Keycode) is { } pages)
        {
            GetNode<ChatLog>("%ChatLog").ScrollPages(pages);
            handled = true;
        }

        if (handled)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private static int? LogPagesFor(Key key) => key switch
    {
        Key.Pageup => 1,
        Key.Pagedown => -1,
        _ => null,
    };

    private bool RunBoundHotkey(InputEventKey key) =>
        Bindings.All.FirstOrDefault(binding => !binding.Hold && key.IsActionPressed(binding.Id, true, true))
            is { } binding
        && RunHotkey(binding.Id);

    private static ChatKey? ChatKeyFor(Key key) => key switch
    {
        Key.Enter or Key.KpEnter => ChatKey.Enter,
        Key.Escape => ChatKey.Escape,
        Key.Slash => ChatKey.Slash,
        Key.Up => ChatKey.Older,
        Key.Down => ChatKey.Newer,
        _ => null,
    };

    public override void _Process(double delta)
    {
        using var timing = Timings.Measure("hud.process");
        if (state == null)
        {
            return;
        }

        RecheckTooltipWhenShowingChanges();
        exit.Advance(TimeSpan.FromSeconds(delta));
        var lookWasShown = lookPanel.Shown != null;
        lookPanel.Advance(TimeSpan.FromSeconds(delta));
        if (lookWasShown && lookPanel.Shown == null)
        {
            Refresh(selectedCharacter);
        }

        ShowHeldItem(state.Player.HeldItem);
        ShowTreeButton();
        var buyingTen = Input.IsKeyPressed(Key.Ctrl) || Input.IsKeyPressed(Key.Alt);
        GetNode<ShopWindowView>("%ShopWindow").ShowText(state, hoveredShopCell, buyingTen);
        if (hoveredSlot is not { } hover)
        {
            return;
        }

        var (hovered, cursorFor) = hover;

        var shape = (CursorShape)CursorShapes.For(cursorFor());
        if (hovered.MouseDefaultCursorShape != shape)
        {
            hovered.MouseDefaultCursorShape = shape;
            GetViewport().UpdateMouseCursorState();
        }
    }

    public override void _EnterTree()
    {
        GetTree().NodeAdded += ShowContextToNewNode;
        if (!Engine.IsEditorHint())
        {
            ChildEnteredTree += ApplyWindowFonts;
            return;
        }

        try
        {
            editorSprites = new SpriteLoader(DataPaths.Root(), EditorCacheBudgetBytes);
            sprites = editorSprites;
            fonts = new ClassicFonts(editorSprites);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException
            or UnauthorizedAccessException)
        {
            GD.PushWarning($"The interface sprites can't be shown: {exception.Message}");
        }
    }

    public override void _ExitTree()
    {
        GetTree().NodeAdded -= ShowContextToNewNode;
        ChildEnteredTree -= ApplyWindowFonts;
        if (editorSprites == null)
        {
            return;
        }

        editorSprites.Dispose();
        editorSprites = null;
        sprites = null;
        fonts = null;
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    // Every window under the HUD, and anything without a font of its own, is written in the font set's white letters.
    // The editor keeps the scenes' own fonts, so a saved scene never holds the font set.
    private void ApplyWindowFonts(Node node)
    {
        if (Engine.IsEditorHint() || fonts == null)
        {
            return;
        }

        var font = fonts.For(TextColour.White);
        if (node == this)
        {
            Theme = WindowFonts.WithFont(new Theme(), font.Font, font.Size);
        }

        WindowFonts.Apply(node, font.Font, font.Size);
    }

    private void ShowContext()
    {
        if (!IsInsideTree())
        {
            return;
        }

        var context = Context;
        foreach (var node in GetTree().GetNodesInGroup(HudContext.Group))
        {
            if (node is IHudContextUser user && IsAncestorOf(node))
            {
                user.ShowContext(context);
            }
        }
    }

    private void ShowContextToNewNode(Node node)
    {
        if (node is IHudContextUser user && node.IsInGroup(HudContext.Group) && IsAncestorOf(node))
        {
            user.ShowContext(Context);
        }
    }

    private void ClickWorn(WornSlot slot, MapButton button)
    {
        if (SkillShortcut.SelectsItem(button, HeldKeys.Current()))
        {
            state?.Chat.Add(skillShortcut.ToggleEquipment(slot));
            return;
        }

        if (EquipmentClicks.Packet(slot, button, HeldKeys.Current(), selectedCharacter) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickInventory(int slot, MapButton button)
    {
        if (state == null)
        {
            return;
        }

        var keys = HeldKeys.Current();
        if (SkillShortcut.SelectsItem(button, keys))
        {
            state.Chat.Add(skillShortcut.ToggleInventory(slot));
            return;
        }

        var player = state.Player;
        var packets = InventoryClicks.Packets(
            slot,
            player.Inventory[slot],
            player.HeldItem.Sprite != 0,
            button,
            keys,
            SaleToOpenShop(),
            selectedCharacter);
        foreach (var packet in packets)
        {
            sendCommand(packet);
        }
    }

    private ShopSale? SaleToOpenShop() =>
        state?.OpenShop is { } shop and not ShopKind.WeaponSmith and not ShopKind.ArmourSmith
            ? new ShopSale(state.Shop.Number, depotPage.Current)
            : null;

    private bool RunHotkey(string id)
    {
        if (Bindings.SpellSlot(id) is { } slot)
        {
            UseHotbar(slot);
            return true;
        }

        if (id == "toggle_options_hotkey")
        {
            ToggleOptions();
            return true;
        }

        if (id == "toggle_planner_hotkey")
        {
            TogglePlanner();
            return true;
        }

        if (id == "toggle_session_stats_hotkey")
        {
            ShowSessionStats(gameSession is { SessionStatsOpen: false });
            return true;
        }

        if (id == "toggle_worldmap_hotkey")
        {
            gameSession?.WorldMapWindow.Toggle();
            ShowWorldMap();
            return true;
        }

        if (id == "area_use_hotkey")
        {
            StartAreaUse();
            return true;
        }

        if (id == "swap_gear_hotkey")
        {
            ClickSwapSet(MapButton.Left);
            return true;
        }

        if (id == "toggle_gamma_hotkey")
        {
            StepWorldGamma();
            return true;
        }

        if (id == "reset_window_size_hotkey")
        {
            ResetWindowSize();
            return true;
        }

        if (id == "display_debug_information_hotkey")
        {
            ShowDebugInformation();
            return true;
        }

        if (HotkeyCommands.Says(id) is { } line)
        {
            Say(line);
            return true;
        }

        if (OptionForHotkey(id) is not { } option)
        {
            return false;
        }

        ClickOption(option, MapButton.Left);
        return true;
    }

    // A maximised window is brought back to a plain window first, or the new size would not show.
    private void StepWorldGamma()
    {
        if (ClientSettings == null)
        {
            return;
        }

        ChangeClientSettings(settings => settings.WorldGamma = Gamma.NextStep(settings.WorldGamma));
        ShowOptions();
        state?.Chat.Add(Gamma.WorldGammaLine(ClientSettings.WorldGamma));
    }

    private void ResetWindowSize()
    {
        if (WindowReset.Line(ClientSettings?.Windowed ?? true) is not { } line)
        {
            return;
        }

        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(new Vector2I(WindowReset.Size.Width, WindowReset.Size.Height));
        state?.Chat.Add(line);
    }

    public void ShowTimings(TimingRecorder recorder, PacketCounter packets)
    {
        var view = GD.Load<PackedScene>(TimingsScenePath).Instantiate<TimingsView>();
        AddChild(view);
        view.Watch(recorder, packets, () => ReadDebugFacts?.Invoke(), fonts!);
        timingsView = view;
    }

    private void ShowDebugInformation()
    {
        if (timingsView != null)
        {
            timingsView.Toggle();
            return;
        }

        if (ReadDebugFacts?.Invoke() is not { } facts)
        {
            return;
        }

        foreach (var line in DebugInformation.Lines(facts))
        {
            state?.Chat.Add(line);
        }
    }

    private static OptionButton? OptionForHotkey(string id) => id switch
    {
        "speed_fast_hotkey" => OptionButton.Fast,
        "speed_normal_hotkey" => OptionButton.Normal,
        "speed_slow_hotkey" => OptionButton.Slow,
        "toggle_percent_hotkey" => OptionButton.HealthPercent,
        "toggle_stat_base_hotkey" => OptionButton.StatBase,
        "toggle_hide_sprite_hotkey" => OptionButton.HideWalls,
        "toggle_names_hotkey" => OptionButton.Names,
        "toggle_health_bars_hotkey" => OptionButton.HealthBars,
        "exit_hotkey" => OptionButton.Exit,
        _ => null,
    };

    private void ClickOption(OptionButton option, MapButton button)
    {
        if (button == MapButton.Right)
        {
            if (OptionButtons.Help(option) is { } help)
            {
                state?.Chat.Add(new LogLine(help, HintFont));
            }

            return;
        }

        if (button != MapButton.Left)
        {
            return;
        }

        if (option == OptionButton.Exit)
        {
            ClickExit();
            return;
        }

        if (option == OptionButton.Options)
        {
            ToggleOptions();
            return;
        }

        if (OptionButtons.Packet(option) is { } packet)
        {
            sendCommand(packet);
        }

        if (OptionButtons.Toggle(option, Settings))
        {
            SettingsChanged?.Invoke();
        }
    }

    public void ServerEnded(int reasonCode)
    {
        if (exit.ServerEnded(reasonCode))
        {
            LeaveRequested?.Invoke();
        }
    }

    private void ClickExit()
    {
        switch (exit.Click())
        {
            case ExitStep.Warn:
                foreach (var packet in ChatPackets.Say(ExitSequence.TavernCommand))
                {
                    sendCommand(packet);
                }

                AddWarnings(ExitSequence.WarningLines);
                break;
            case ExitStep.RequestExit:
                sendCommand(ExitPackets.Exit());
                AddWarnings(ExitSequence.RequestLines);
                break;
            case ExitStep.Leave:
                LeaveRequested?.Invoke();
                break;
            case ExitStep.Wait:
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void AddWarnings(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            state?.Chat.Add(new LogLine(line, WarningFont));
        }
    }

    private void ClickMoney(int silver, MapButton button)
    {
        if (MoneyButtons.Packet(silver, button, selectedCharacter) is { } packet)
        {
            sendCommand(packet);
        }
        else if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(MoneyButtons.Hint(silver), HintFont));
        }
    }

    private void ClickTrashCan(MapButton button)
    {
        if (TrashCan.Packet(button, selectedCharacter) is { } packet)
        {
            sendCommand(packet);
        }
        else if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(TrashCan.Hint, HintFont));
        }
    }

    // Redrawn only when the map, the player's tile, the magnification or the notes have changed since the last
    // picture.
    private void ShowMinimap()
    {
        if (ExploredMap is not { } map || state == null)
        {
            return;
        }

        var player = state.Map.Tiles[WorldMap.PlayerTile];
        var shown = (map.Revision, player.X, player.Y, minimapView.Magnification, mapNotesStamp);
        if (shown == minimapShown)
        {
            return;
        }

        minimapShown = shown;
        minimapView.Render(map, player.X, player.Y, minimapPixels);
        var (originX, originY) = minimapView.Origin(player.X, player.Y);
        MinimapOverlay.Paint(mapNotes, minimapPixels, originX, originY, minimapView.Magnification);
        if (minimapImage == null || minimapTexture == null)
        {
            minimapImage = Image.CreateFromData(
                MinimapView.Pixels,
                MinimapView.Pixels,
                false,
                Image.Format.Rgba8,
                minimapPixels);
            minimapTexture = ImageTexture.CreateFromImage(minimapImage);
            GetNode<TextureRect>("%Minimap/Picture").Texture = minimapTexture;
            return;
        }

        minimapImage.SetData(MinimapView.Pixels, MinimapView.Pixels, false, Image.Format.Rgba8, minimapPixels);
        minimapTexture.Update(minimapImage);
    }

    // A modifier key makes the zoom buttons jump to the end; clicking the map itself waits for the world map window.
    private void ClickMinimap(MinimapButton clicked, MapButton button)
    {
        if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(MinimapView.Help(clicked), HintFont));
            return;
        }

        if (button != MapButton.Left)
        {
            return;
        }

        var toTheEnd = HeldKeys.Current() != MapKeys.None;
        switch (clicked)
        {
            case MinimapButton.ZoomIn:
                minimapView.ZoomIn(toTheEnd);
                break;
            case MinimapButton.ZoomOut:
                minimapView.ZoomOut(toTheEnd);
                break;
            case MinimapButton.Map:
                gameSession?.WorldMapWindow.OpenAtPlayer();
                ShowWorldMap();
                return;
            default:
                throw new UnreachableException();
        }

        ShowMinimap();
    }

    private void ScrollMinimap(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true } wheel
            || wheel.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown))
        {
            return;
        }

        minimapView.Scroll(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
        ShowMinimap();
    }

    // The minimap's rows run down it with map x and its columns across with map y.
    private string MinimapPinName(Vector2 position)
    {
        if (state == null)
        {
            return "";
        }

        var player = state.Map.Tiles[WorldMap.PlayerTile];
        var (originX, originY) = minimapView.Origin(player.X, player.Y);
        var pin = MinimapOverlay.PinAt(
            mapNotes,
            originX,
            originY,
            minimapView.Magnification,
            (int)position.Y,
            (int)position.X);
        return pin?.Label ?? "";
    }

    // The current client saves the notes straight after every change.
    private void MapNotesChanged()
    {
        mapNotesStamp++;
        SaveMapNotes?.Invoke();
        ShowMinimap();
    }

    // The current client shows the set-aside notice the first time the world map opens.
    private void ShowWorldMap()
    {
        if (state == null || gameSession == null || worldMapWindow == null)
        {
            return;
        }

        var player = state.Map.Tiles[WorldMap.PlayerTile];
        worldMapWindow.ShowWindow(gameSession.WorldMapWindow, ExploredMap, mapNotes, (player.X, player.Y));
        if (gameSession.WorldMapWindow.IsOpen && !notesNoticeShown && NotesSetAsideFile is { } fileName)
        {
            notesNoticeShown = true;
            gameSession.WorldMapWindow.ShowSetAsideNotice(fileName, Time.GetTicksMsec() / 1000.0);
        }
    }

    private void ClickSwapSet(MapButton button)
    {
        if (EquipmentClicks.SwapSetPacket(button, selectedCharacter) is { } packet)
        {
            sendCommand(packet);
        }
        else if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(SwapSetHint, HintFont));
        }
    }

    private void ShowHeldItem(ItemSlot item)
    {
        var held = GetNode<ItemSlotView>("%HeldItem");
        held.Show(item, ItemOverlays.Worn(item), false);
        held.Position = mousePosition - HeldItemOffset;
        held.SetHighlighted(HeldItem.IsHighlighted(CursorShapes.CursorFor(Input.GetCurrentCursorShape())));
    }

    private void ShowStats(PlayerState player)
    {
        var skillList = GetNode<SkillList>("%SkillRows");
        var panel = skillList.Panel;
        var showBase = Settings.ShowStatBase;
        if (panel.ShowsMetaOnTop)
        {
            ShowTopMeta(panel);
        }
        else
        {
            ShowAttributes(player, showBase);
        }

        skillList.Show(player, state!.Terminology, statPlan, showBase);
        SetText("UpdatePoints", Number(player.Points - statPlan.PointsUsed));
        var skillsBelow = !panel.ShowsMetaStats;
        foreach (var name in (string[])["%SkillPanel", "%UpdateLabel", "%UpdatePoints", "%UpdateButton"])
        {
            GetNode<CanvasItem>(name).Visible = skillsBelow;
        }

        GetNode<CanvasItem>("%MetaPanel").Visible = panel.Mode == StatListMode.SkillsAndMeta;
        foreach (var node in GetNode("%StatModeButtons").GetChildren())
        {
            ((StatModeButton)node).ShowMarked(panel);
        }
    }

    private void ShowAttributes(PlayerState player, bool showBase)
    {
        var rows = GetNode("%StatRows");
        for (var attribute = 0; attribute < PlayerState.AttributeCount; attribute++)
        {
            var line = StatLines.Attribute(attribute, player.Attributes[attribute], statPlan, player.Points);
            rows.GetChild<StatRow>(attribute).Show(line, attribute, showBase);
        }

        var hitPoints = StatLines.HitPoints(player.HitPoints, statPlan, player.Points);
        rows.GetChild<StatRow>(HitPointsRow).Show(hitPoints, StatSlots.HitPoints, showBase);
        var mana = StatLines.Mana(player.Mana, statPlan, player.Points);
        rows.GetChild<StatRow>(ManaRow).Show(mana, StatSlots.Mana, showBase);
    }

    private void ShowTopMeta(StatPanel panel)
    {
        var rows = GetNode("%StatRows");
        for (var row = 0; row < StatPanel.TopRows; row++)
        {
            var meta = panel.TopMeta(row)!.Value;
            rows.GetChild<StatRow>(row).ShowMeta(MetaLines.For(state!.Terminology.MetaStats[meta]), meta);
        }
    }

    private void AskForMetaText()
    {
        if (state?.Terminology.AskForMetaText() == true)
        {
            sendCommand(MetaPackets.Request());
        }
    }

    private void ClickStatMode(StatListMode mode, MapButton button)
    {
        if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(StatPanel.Help(mode), HintFont));
            return;
        }

        if (button != MapButton.Left || state == null)
        {
            return;
        }

        GetNode<SkillList>("%SkillRows").Choose(mode);
        ShowStats(state.Player);
        ShowPlanner();
    }

    private void ClickShop(int cell, MapButton button)
    {
        if (state == null)
        {
            return;
        }

        var slot = ShopClicks.SlotFor(cell);
        if (ShopClicks.Packet(button, HeldKeys.Current(), state.Shop.Number, slot) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickDepot(int cell, MapButton button)
    {
        if (state != null && DepotClicks.Packet(button, state.Shop.Number, depotPage.Current, cell) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickDepotPage(int page, MapButton button)
    {
        if (button != MapButton.Left || state == null)
        {
            return;
        }

        depotPage.Show(page);
        PlayClick();
        GetNode<DepotWindowView>("%DepotWindow").ShowItems(state, depotPage);
    }

    private void ScrollDepot(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true } wheel || state == null
            || wheel.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)
            || !DepotWindowView.InWheelArea(wheel.Position))
        {
            return;
        }

        depotPage.Turn(wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
        GetNode<DepotWindowView>("%DepotWindow").ShowItems(state, depotPage);
    }

    private void ClickSmithItem(int slot, MapButton button)
    {
        if (state != null && SmithClicks.ItemPacket(button, HeldKeys.Current(), state.Shop.Number, slot) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickSmithButton(int buttonNumber, MapButton button)
    {
        if (state != null && SmithClicks.ButtonPacket(button, state.Shop.Number, buttonNumber) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ShowWaypoints()
    {
        if (state != null && gameSession != null)
        {
            GetNode<WaypointWindowView>("%WaypointWindow").ShowWaypoints(gameSession.WaypointWindow, state.Player);
        }
    }

    private void ChooseSelectedWaypoint()
    {
        if (state != null && gameSession?.WaypointWindow.ChooseSelected(state.Player.Waypoints) is { } packet)
        {
            sendCommand(packet);
            ShowWaypoints();
        }
    }

    private void ClickWaypoint(int row, MapButton button)
    {
        if (state != null && gameSession?.WaypointWindow.Choose(row, button, state.Player.Waypoints) is { } packet)
        {
            sendCommand(packet);
            ShowWaypoints();
        }
    }

    private void DragWaypoints(double row)
    {
        if (gameSession != null)
        {
            gameSession.WaypointWindow.ScrollTo((int)row);
            ShowWaypoints();
        }
    }

    private void ScrollWaypoints(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true } wheel || gameSession == null
            || wheel.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)
            || !WaypointWindowView.InWheelArea(wheel.Position))
        {
            return;
        }

        gameSession.WaypointWindow.ScrollBy(wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
        ShowWaypoints();
    }

    private void ClickCloseWaypoints(MapButton button)
    {
        if (button == MapButton.Left && gameSession?.WaypointWindow.Close() == true)
        {
            Refresh(selectedCharacter);
        }
    }

    // The button answers clicks even while it isn't drawn, so a character with only contract points can open that tree.
    private void ShowTreeButton()
    {
        if (state == null || gameSession == null)
        {
            return;
        }

        var button = GetNode<Control>("%TreeButton");
        var player = state.Player;
        button.GetNode<CanvasItem>("Picture").Visible = SkillTreeWindow.ButtonShows(player.TreePoints);
        var glow = button.GetNode<CanvasItem>("Glow");
        glow.Visible = SkillTreeWindow.ButtonShows(player.TreePoints)
            && gameSession.SkillTreeWindow.ButtonGlows(player.TreePoints, player.OsirisTree);
        var brightness = SpriteLighting.Brightness(SkillTreeWindow.GlowLight((long)Time.GetTicksMsec()));
        glow.Modulate = new Color(brightness, brightness, brightness);
    }

    private void ClickTreeButton(MapButton button)
    {
        if (button == MapButton.Left && gameSession != null)
        {
            gameSession.ToggleSkillTree(HeldKeys.Current());
            Refresh(selectedCharacter);
        }
    }

    private void ClickSkillTreeNode(int node, MapButton button)
    {
        if (gameSession?.SkillTreeWindow.Click(node, button, HeldKeys.Current()) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickCloseSkillTree(MapButton button)
    {
        if (button == MapButton.Left && gameSession?.SkillTreeWindow.Close() == true)
        {
            Refresh(selectedCharacter);
        }
    }

    private void ShowOptions()
    {
        if (gameSession != null)
        {
            GetNode<OptionsWindowView>("%OptionsWindow")
                .ShowOptions(gameSession.OptionsWindow, ClientSettings, Settings);
        }
    }

    private void ChangeOptionsWindow(Action<OptionsWindow> change)
    {
        if (gameSession != null)
        {
            change(gameSession.OptionsWindow);
            ShowOptions();
        }
    }

    // The hotbar's key labels are refreshed with the window, since they show the bindings too.
    private void ChangeKeyBindings(Func<OptionsWindow, LogLine?> change)
    {
        if (state == null || gameSession == null)
        {
            return;
        }

        LogLine? line = null;
        ChangeClientSettings(_ => line = change(gameSession.OptionsWindow));
        if (line is { } shown)
        {
            state.Chat.Add(shown);
        }

        Refresh(selectedCharacter);
    }

    private void ToggleOptions()
    {
        gameSession?.OptionsWindow.Toggle();
        ShowOptions();
    }

    private void ChooseOptionsTab(OptionsTab tab)
    {
        gameSession?.OptionsWindow.Show(tab);
        ShowOptions();
    }

    private void CloseOptions()
    {
        gameSession?.OptionsWindow.Close();
        ShowOptions();
    }

    // Options are saved the moment they change, so nothing is lost if the game closes unexpectedly.
    private void ChangeClientSettings(Action<ClientSettings> change)
    {
        if (ClientSettings == null)
        {
            return;
        }

        change(ClientSettings);
        ClientSettings.Save();
        ShowQuickStatsLayout();
        GetNode<ChatLine>("%ChatLine").EnterToTalk = ClientSettings.EnterToTalk;
        ShowContext();
        ClientSettingsChanged?.Invoke();
    }

    // Without Hit and Parry the current client draws a shorter box, and the experience bar moves up with the rows.
    private void ShowQuickStatsLayout()
    {
        var expanded = ClientSettings?.ExpandedQuickStats ?? true;
        var quickStats = GetNode<Control>("QuickStats");
        quickStats.GetNode<CanvasItem>("RowsShift/Rows/HitRow").Visible = expanded;
        quickStats.GetNode<CanvasItem>("RowsShift/Rows/ParryRow").Visible = expanded;
        var background = quickStats.GetNode<UiPicture>("Background");
        background.PictureNumber = expanded ? ExpandedQuickStatsPicture : CompactQuickStatsPicture;
        background.DarkPictureNumber = background.PictureNumber + DarkPictureOffset;
        var bar = GetNode<Control>("%ExperienceBar");
        bar.Position = bar.Position with { Y = expanded ? ExperienceBarTop : ExperienceBarTop - HiddenQuickStatsRows };
    }

    private void ClickCloseShop(MapButton button)
    {
        if (button == MapButton.Left && gameSession?.CloseShop() == true)
        {
            Refresh(selectedCharacter);
        }
    }

    // With a window open, Escape only closes it; otherwise it stops what the character is doing.
    private void PressEscape()
    {
        var closedAWindow = gameSession?.CloseWindowsByEscape() == true;
        if (closedAWindow)
        {
            Refresh(selectedCharacter);
        }

        if (GeneralOptions.EscapeSendsReset(ClientSettings?.EscapeClosesWindowsFirst ?? true, closedAWindow))
        {
            sendCommand(ClientPackets.ShortAndInt(ClientCommand.Reset, 0, 0));
        }
    }

    private void ClickTextWindow(MapButton button, Func<TextWindows, TextWindowAction> click)
    {
        if (button != MapButton.Left || gameSession == null)
        {
            return;
        }

        var action = click(gameSession.TextWindows);
        foreach (var packet in action.Packets)
        {
            sendCommand(packet);
        }

        if (action.PlaysClick)
        {
            PlayClick();
        }

        ShowTextWindows();
    }

    private void ShowTextWindows()
    {
        if (gameSession != null)
        {
            GetNode<TextWindowView>("%TextWindows").Show(gameSession.TextWindows, gameSession.MessageOfTheDayLines);
        }
    }

    private void Say(string line)
    {
        foreach (var packet in ChatPackets.Say(line))
        {
            sendCommand(packet);
        }
    }

    private bool ItemTooltipsShown() =>
        ClientSettings is not { } settings
        || GeneralOptions.ShowsItemTooltips(
            settings.ItemTooltips,
            settings.ItemTooltipsAlways,
            Input.IsActionPressed(ItemTooltipBinding));

    // Godot looks for a tooltip again only when the mouse enters a control or moves 5 pixels, so when the tooltip
    // key goes down or up the mouse is moved off everything and back, entering the slot afresh.
    private void RecheckTooltipWhenShowingChanges()
    {
        var shown = ItemTooltipsShown();
        if (shown == itemTooltipsShown)
        {
            return;
        }

        itemTooltipsShown = shown;
        var away = new InputEventMouseMotion { Position = OffScreen, GlobalPosition = OffScreen };
        var back = new InputEventMouseMotion { Position = mousePosition, GlobalPosition = mousePosition };
        GetViewport().PushInput(away, true);
        GetViewport().PushInput(back, true);
    }

    private ItemTooltipRequest? InventoryTooltip(int slot)
    {
        if (!itemTooltipsShown || state is not { ItemLooks.Enabled: true } shown || shown.Player.HeldItem.Sprite != 0
            || shown.Player.Inventory[slot].Sprite == 0)
        {
            return null;
        }

        return new ItemTooltipRequest(ItemTooltipMode.Plain, shown.ItemLooks.For(ItemSlotKind.Inventory, slot), null);
    }

    // Holding an item over gear it fits compares the two; gear it doesn't fit shows its usual tooltip.
    private ItemTooltipRequest? WornTooltip(WornSlot slot)
    {
        if (!itemTooltipsShown || state is not { ItemLooks.Enabled: true } shown)
        {
            return null;
        }

        var player = shown.Player;
        var filled = player.Worn[(int)slot].Sprite != 0;
        var worn = shown.ItemLooks.For(ItemSlotKind.Worn, (int)slot);
        if (player.HeldItem.Sprite != 0 && !Equipment.BlockedSlots(player.HeldItem, player.Worn).Contains(slot))
        {
            var held = shown.ItemLooks.Held;
            if (filled)
            {
                return new ItemTooltipRequest(ItemTooltipMode.Comparing, worn, held);
            }

            return held != null ? new ItemTooltipRequest(ItemTooltipMode.EmptySlot, null, held) : null;
        }

        return filled ? new ItemTooltipRequest(ItemTooltipMode.Plain, worn, null) : null;
    }

    private void ShowQuickStats(PlayerState player)
    {
        ShowQuickStatsLayout();
        var metaStats = state!.Terminology.MetaStats;
        SetText("WeaponValue", Number(player.Weapon));
        SetText("ArmorValue", Number(player.Armor));
        SetText("HitScore", Number(metaStats[HitScoreMeta].Value));
        SetText("ParryScore", Number(metaStats[ParryScoreMeta].Value));
        SetText("Experience", Number(player.PointsTotal));
        GetNode<ProgressBar>("%ExperienceBar").Value = Ranks.Progress(player.PointsTotal);
    }

    private void ClickHotbar(int slot, MapButton button)
    {
        if (button == MapButton.Left)
        {
            UseHotbar(slot);
            return;
        }

        if (button != MapButton.Right || state == null)
        {
            return;
        }

        if (skillShortcut.Selected is { } selectedAction)
        {
            AssignHotbar(slot, selectedAction);
            return;
        }

        var skillNames = state.Terminology.Skills;
        var skills = HotbarAssignments.MenuSkills(state.Player.Skills, state.Player.Kindred)
            .Select(number => (number, skillNames[number].Name));
        GetNode<HotbarMenu>("%HotbarMenu").Open(slot, skills, mousePosition);
    }

    private void ChooseHotbarSkill(int slot, int? skill)
    {
        if (skill is { } number)
        {
            AssignHotbar(slot, number);
            return;
        }

        HotbarAssignments.Clear(Settings, slot);
        HotbarChanged();
    }

    private void AssignHotbar(int slot, int action)
    {
        var name = action < HotbarButtons.FirstInventoryAction
            ? state!.Terminology.Skills[action].Name
            : HotbarButtons.ItemName(action);
        var keyName = BindingRules.HotbarKeyName(slot);
        state!.Chat.Add(HotbarAssignments.Assign(Settings, slot, action, name, keyName));
        HotbarChanged();
    }

    private void HotbarChanged()
    {
        SettingsChanged?.Invoke();
        Refresh(selectedCharacter);
    }

    private void UseHotbar(int slot)
    {
        if (HotbarButtons.UsePacket(Settings.HotbarActions[slot], selectedCharacter) is { } packet)
        {
            sendCommand(packet);
        }
    }

    private void ClickStatName(StatRow row, MapButton button)
    {
        if (state == null)
        {
            return;
        }

        if (row.Meta is { } meta)
        {
            var stat = state.Terminology.MetaStats[meta];
            if (button == MapButton.Right && stat.Show != 0)
            {
                foreach (var line in MetaLines.Description(stat.Name, stat.Description))
                {
                    state.Chat.Add(line);
                }
            }

            return;
        }

        if (row.Slot < StatSlots.FirstSkill)
        {
            if (button == MapButton.Right)
            {
                state.Chat.Add(SkillClicks.StatHelp(row.Slot));
            }

            return;
        }

        var player = state.Player;
        var number = row.Slot - StatSlots.FirstSkill;
        var text = state.Terminology.Skills[number];
        var learnt = player.Skills[number][0] != 0;
        if (button == MapButton.Left)
        {
            sendCommand(SkillPackets.Use(number, selectedCharacter));
        }
        else if (button == MapButton.Right && Kindred.IsKnown(number, learnt, player.Kindred))
        {
            foreach (var line in SkillClicks.Help(text.Name, text.Description, text.Attributes))
            {
                state.Chat.Add(line);
            }

            state.Chat.Add(skillShortcut.Toggle(number, text.Name, player.Kindred));
        }
    }

    private void ClickStat(StatRow row, bool raise, MapButton button)
    {
        if (state == null || row.Meta != null)
        {
            return;
        }

        if (button == MapButton.Right)
        {
            var help = raise ? StatPlan.RaiseHelp(row.Line.Name) : StatPlan.LowerHelp(row.Line.Name);
            state.Chat.Add(new LogLine(help, HintFont));
            return;
        }

        if (button != MapButton.Left || (raise && !row.Line.CanRaise))
        {
            return;
        }

        ChangePlan(row.Slot, raise);
    }

    // Shift raises or lowers 10 at once, Ctrl or Alt 90, in the HUD and the planner alike.
    private void ChangePlan(int slot, bool raise)
    {
        if (state == null)
        {
            return;
        }

        var ctrlOrAlt = Input.IsKeyPressed(Key.Ctrl) || Input.IsKeyPressed(Key.Alt);
        var times = StatPlan.ClickTimes(Input.IsKeyPressed(Key.Shift), ctrlOrAlt);
        var values = StatSlots.Values(state.Player, slot);
        if (raise)
        {
            statPlan.Raise(slot, values, state.Player.Points, times);
        }
        else
        {
            statPlan.Lower(slot, values, times);
        }

        ShowStats(state.Player);
        ShowPlanner();
    }

    private void ClickUpdate(MapButton button)
    {
        if (button == MapButton.Right)
        {
            state?.Chat.Add(new LogLine(StatPlan.UpdateHelp, HintFont));
            return;
        }

        if (button != MapButton.Left || state == null)
        {
            return;
        }

        CommitPlan();
    }

    private void CommitPlan()
    {
        if (state == null)
        {
            return;
        }

        foreach (var packet in statPlan.Commit())
        {
            sendCommand(packet);
        }

        ShowStats(state.Player);
        ShowPlanner();
    }

    // The planner's own Commit also drops the focus, as the numbers it worked on are about to change.
    private void CommitPlannerPlan()
    {
        gameSession?.PlannerWindow.ClearFocus();
        CommitPlan();
    }

    private void ToggleFocus(PlannerFocus focus)
    {
        gameSession?.PlannerWindow.ToggleFocus(focus);
        ShowPlanner();
    }

    private void PlanFocus()
    {
        using var timing = Timings.Measure("planner.planfocus");
        if (state == null || gameSession == null || PlannerSheet() is not { } sheet)
        {
            return;
        }

        var panel = FocusPanel.For(sheet, gameSession.PlannerWindow.Focus, focusCache);
        var player = state.Player;
        if (panel.Affordable
            && statPlan.RaiseAll(
                panel.Plan.Select(raise => (raise.RaiseIndex, raise.Points)).ToArray(),
                slot => StatSlots.Values(player, slot),
                player.Points))
        {
            ShowStats(player);
            ShowPlanner();
        }
    }

    private void ResetPlan()
    {
        statPlan.Reset();
        if (state != null)
        {
            ShowStats(state.Player);
        }

        ShowPlanner();
    }

    private void ClickPlannerButton(MapButton button)
    {
        if (button == MapButton.Left)
        {
            TogglePlanner();
        }
    }

    // The button and its hotkey do nothing while the option is off.
    private void TogglePlanner()
    {
        if (gameSession == null || !PlannerEnabled)
        {
            return;
        }

        if (gameSession.PlannerWindow.Toggle())
        {
            AskForMetaText();
        }

        ShowPlanner();
    }

    private void ShowSessionStats(bool open)
    {
        if (gameSession != null)
        {
            gameSession.SessionStatsOpen = open;
        }

        RefreshSessionStats();
    }

    private void ResetSessionStats()
    {
        if (state != null && gameSession != null)
        {
            gameSession.SessionCurrencies.Reset(state.Player);
        }
    }

    private void RefreshSessionStats()
    {
        if (gameSession != null)
        {
            var currencies = gameSession.SessionCurrencies;
            sessionStatsWindow?.ShowWindow(gameSession.SessionStatsOpen, currencies.Tallies, currencies.Elapsed);
        }
    }

    private void ClosePlanner()
    {
        gameSession?.PlannerWindow.Close();
        ShowPlanner();
    }

    private void OpenPlannerAdvanced()
    {
        if (gameSession != null)
        {
            gameSession.PlannerWindow.AdvancedOpen = true;
        }

        ShowPlanner();
    }

    private void ClosePlannerAdvanced()
    {
        if (gameSession != null)
        {
            gameSession.PlannerWindow.AdvancedOpen = false;
        }

        ShowPlanner();
    }

    private void ShowPlanner()
    {
        using var timing = Timings.Measure("planner.refresh");
        var enabled = PlannerEnabled;
        var buttonShown = enabled && !GetNode<SkillList>("%SkillRows").Panel.ShowsMetaStats;
        foreach (var name in (string[])["%PlannerBox", "%PlannerLabel", "%PlannerButton"])
        {
            GetNode<CanvasItem>(name).Visible = buttonShown;
        }

        GetNode<PlannerButtonBox>("%PlannerBox").DarkMode = DarkMode;
        if (state == null || gameSession == null || plannerWindow == null)
        {
            return;
        }

        var shown = enabled && gameSession.PlannerWindow.IsOpen;
        var sheet = shown ? PlannerSheet() : null;
        plannerWindow.ShowWindow(
            shown,
            DarkMode,
            state.Player.Points - statPlan.PointsUsed,
            statPlan.PointsUsed,
            sheet);
        if (sheet == null || PlannerChoices == null)
        {
            if (plannerAdvanced != null)
            {
                plannerAdvanced.Visible = false;
            }

            return;
        }

        plannerAdvanced?.ShowWindow(
            gameSession.PlannerWindow.AdvancedOpen,
            DarkMode,
            PlannerChoices.Choices,
            AdvancedSheet.For(sheet));

        var panel = FocusPanel.For(sheet, gameSession.PlannerWindow.Focus, focusCache);
        if (panel.LostFocus)
        {
            gameSession.PlannerWindow.ClearFocus();
        }

        plannerWindow.ShowFocus(gameSession.PlannerWindow.Focus, panel);
    }

    private void ChangePlannerChoices(Action<PlannerChoices> change)
    {
        if (PlannerChoices == null)
        {
            return;
        }

        change(PlannerChoices.Choices);
        PlannerChoices.Save();
        ShowPlanner();
    }

    private PlannerSheet? PlannerSheet()
    {
        if (state == null || PlannerChoices == null)
        {
            return null;
        }

        var hardMode = CharacterRace.FromNumber(state.Player.Race).GameMode == GameMode.Hard;
        var game = PlannerGame.From(
            state.Player,
            state.Terminology,
            statPlan,
            PlannerChoices.Choices,
            state.BuffWatch,
            hardMode);
        var sheet = Planner.PlannerSheet.For(game);
        if (sheet.ChoicesChanged)
        {
            PlannerChoices.Save();
        }

        return sheet;
    }

    private void ShowLook(CharacterLook look)
    {
        lookPanel.Show(look);
        Refresh(selectedCharacter);
    }

    private void ShowPortrait(PlayerState player, int sprite)
    {
        ShowBar("HitPointBar", player.ActiveHitPoints, player.HitPoints);
        ShowBar("EnduranceBar", player.ActiveEndurance, player.Endurance);
        ShowBar("ManaBar", player.ActiveMana, player.Mana);
        GetNode<CanvasItem>("%OwnName").Visible = true;
        SetText("OwnName", player.Name);
        ShowCharacter(player.Name, player.PointsTotal, sprite, SpriteTint.None);
        if (state is { OpenShop: not (null or ShopKind.WeaponSmith or ShopKind.ArmourSmith) })
        {
            var shop = state.Shop;
            ShowCharacter(shop.Name, shop.Points, shop.Sprite, SpriteTint.None);
        }

        ShowEquipment(player);
    }

    private void ShowPortrait(CharacterLook look)
    {
        ShowLookBar("HitPointBar", look.ActiveHitPoints, look.HitPoints);
        ShowLookBar("EnduranceBar", look.ActiveEndurance, look.Endurance);
        ShowLookBar("ManaBar", look.ActiveMana, look.Mana);
        GetNode<CanvasItem>("%OwnName").Visible = false;
        ShowCharacter(look.Name, look.Points, look.Sprite, LookPanel.PortraitTint(look.Flags));
        GetNode<CanvasItem>("%SecondCharmFrame").Visible = look.Worn[(int)WornSlot.SecondCharm] != 0;
        for (var slot = WornSlot.Head; slot <= WornSlot.SecondCharm; slot++)
        {
            var slotView = GetNode<ItemSlotView>("%" + slot);
            slotView.Show(new ItemSlot(look.Worn[(int)slot], 0, 0, 0), [], false);
            slotView.ShowHotbarKey(null);
        }
    }

    private void ShowCharacter(string name, int points, int sprite, SpriteTint tint)
    {
        SetText("PortraitName", name);
        SetText("RankName", Ranks.Name(points));
        SetSprite("RankBadge", RankBadges.Sprite(points));
        SetSprite("Portrait", sprite);
        var material = (ShaderMaterial)GetNode<CanvasItem>("%Portrait").Material;
        material.SetShaderParameter("grey", tint.HasFlag(SpriteTint.Grey));
        material.SetShaderParameter("infrared", tint.HasFlag(SpriteTint.Infrared));
        material.SetShaderParameter("bloody", tint.HasFlag(SpriteTint.Bloody));
    }

    private void ShowEquipment(PlayerState player)
    {
        var blocked = Equipment.BlockedSlots(player.HeldItem, player.Worn);
        var secondCharmWorn = player.Worn[(int)WornSlot.SecondCharm].Sprite != 0;
        GetNode<CanvasItem>("%SecondCharmFrame").Visible = secondCharmWorn;
        for (var slot = WornSlot.Head; slot <= WornSlot.SecondCharm; slot++)
        {
            var item = player.Worn[(int)slot];
            var showBlocked = blocked.Contains(slot) && (slot != WornSlot.SecondCharm || secondCharmWorn);
            var slotView = GetNode<ItemSlotView>("%" + slot);
            slotView.Show(item, ItemOverlays.Worn(item), showBlocked);
            var hotbarKey = item.Sprite != 0 ? HotbarButtons.GearKeyButton(Settings.HotbarActions, slot) : null;
            slotView.ShowHotbarKey(hotbarKey);
        }
    }

    private void ShowPool(string name, int current, int maximum)
    {
        SetText(name, Number(current));
        SetText("Max" + name, Number(maximum));
    }

    private void ShowBar(string name, int current, int[] values) =>
        GetNode<PortraitBar>("%" + name)
            .Show(PortraitBars.For(current, values[MaximumSlot], values[ReservedSlot]), false);

    private void ShowLookBar(string name, int current, int maximum) =>
        GetNode<PortraitBar>("%" + name).Show(PortraitBars.For(current, maximum, 0), true);

    private void ShowPoints(string name, string shownName, int points)
    {
        SetText(name + "Name", Currencies.RowName(shownName, points));
        SetText(name, Number(points));
    }

    private void SetText(string name, string text) => GetNode<Label>("%" + name).Text = text;

    private void SetSprite(string name, int spriteNumber) =>
        GetNode<ArchiveSprite>("%" + name).SpriteNumber = spriteNumber;
}
