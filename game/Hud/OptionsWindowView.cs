using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Hotkeys;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Hud;

// The current client's Options window: a Dear ImGui window with game-sprite buttons, rebuilt as themed controls.
[Tool]
public partial class OptionsWindowView : Panel, IHudContextUser
{
    private const int CloseSprite = 18042;
    private const string OptionRowScenePath = "res://Hud/option_row.tscn";
    private const string SliderRowScenePath = "res://Hud/slider_row.tscn";
    private const string KeybindSectionScenePath = "res://Hud/keybind_section.tscn";
    private const string KeybindRowScenePath = "res://Hud/keybind_row.tscn";
    private const string WaitingText = "Press key...";
    private const string WaitingHint = "ESC to cancel. DEL to un-bind.";
    private const string KeybindsHint = "Click a key, then press the new one.";
    private const string ResetText = "Reset {0} to Defaults";
    private const string ConfirmResetText = "Confirm?";
    private const string WarningMark = "!";
    private const string RowVariation = "TableRow";
    private const string HoveredRowVariation = "TableRowHover";
    private const string ScrollBarVariation = "OptionsScrollBar";

    private static readonly Rect2 CloseRegion = new(283, 0, 14, 14);

    private readonly Dictionary<string, Button> keyButtons = [];
    private readonly Dictionary<string, Label> keyWarnings = [];
    private string? capturing;
    private IReadOnlyList<KeybindRow> shownKeybindRows = [];
    private SpriteLoader? sprites;

    public OptionsWindowView() => AddToGroup(HudContext.Group);

    private CheckBox SoundEnabled => GetNode<CheckBox>("%SoundEnabled");

    private HSlider Volume => GetNode<HSlider>("%Volume");

    private HSlider CursorScale => GetNode<HSlider>("%CursorScale");

    private Node KeybindSections => GetNode("%Sections");

    private HSlider ScreenGamma => GetNode<HSlider>("%ScreenGamma");

    private HSlider WorldGamma => GetNode<HSlider>("%WorldGamma");

    private Button Reset => GetNode<Button>("%Reset");

    private Label TypingWarning => OptionRow(GeneralOption.EnterToTalk).GetNode<Label>("Line/Warning");

    public void ShowContext(HudContext context)
    {
        sprites = context.Sprites;
        ApplySprites();
    }

    public event Action<OptionsTab>? TabChosen;

    public event Action? CloseChosen;

    public event Action<bool>? SoundEnabledChanged;

    public event Action<int>? SoundVolumeChanged;

    public event Action<GeneralOption, bool>? OptionChanged;

    public event Action<float>? CursorScaleChanged;

    public event Action<string>? CaptureChosen;

    public event Action<KeyChord>? KeyCaptured;

    public event Action? CaptureCancelled;

    public event Action? CaptureUnbound;

    public event Action<string>? BindingResetChosen;

    public event Action? ResetChosen;

    public event Action<float>? ScreenGammaChanged;

    public event Action<float>? WorldGammaChanged;

    public event Action<DisplaySwap, bool>? SwapChanged;

    public event Action<WindowBackground, int>? BackgroundOpacityChanged;

    public override void _Ready()
    {
        ApplySprites();
        StyleScrollBars();
        AddOptionRows();
        ShowKeybinds(
            KeybindsTab.Sections(new CharacterSettings()),
            BindingActions.Current(),
            false,
            new OptionsWindow());
        if (Engine.IsEditorHint())
        {
            return;
        }

        foreach (var tab in Enum.GetValues<OptionsTab>())
        {
            TabButton(tab).Pressed += () => TabChosen?.Invoke(tab);
        }

        foreach (var row in GeneralOptions.Rows)
        {
            OptionBox(row.Option).Toggled += isOn => OptionChanged?.Invoke(row.Option, isOn);
        }

        foreach (var row in GetNode("%Pages").FindChildren("*Row", nameof(PanelContainer), true, false))
        {
            ShowHoverBand((Control)row);
        }

        GetNode<BaseButton>("%CloseButton").Pressed += () => CloseChosen?.Invoke();
        SoundEnabled.Toggled += enabled => SoundEnabledChanged?.Invoke(enabled);
        Volume.ValueChanged += volume =>
        {
            ShowValue(Volume, ((int)volume).ToString(CultureInfo.InvariantCulture));
            SoundVolumeChanged?.Invoke((int)volume);
        };
        CursorScale.ValueChanged += scale =>
        {
            ShowCursorScale((float)scale);
            CursorScaleChanged?.Invoke((float)scale);
        };
        Reset.Pressed += () => ResetChosen?.Invoke();
        ScreenGamma.ValueChanged += gamma =>
        {
            ShowValue(ScreenGamma, Gamma.Text((float)gamma));
            ScreenGammaChanged?.Invoke((float)gamma);
        };
        WorldGamma.ValueChanged += gamma =>
        {
            ShowValue(WorldGamma, Gamma.Text((float)gamma));
            WorldGammaChanged?.Invoke((float)gamma);
        };
        foreach (var row in DisplaySwaps.Rows)
        {
            SwapBox(row.Swap).Toggled += isOn => SwapChanged?.Invoke(row.Swap, isOn);
        }

        foreach (var row in WindowBackgrounds.Rows)
        {
            var slider = BackgroundSlider(row.Window);
            slider.ValueChanged += opacity =>
            {
                ShowValue(slider, WindowBackgrounds.Text((int)opacity));
                BackgroundOpacityChanged?.Invoke(row.Window, (int)opacity);
            };
        }
    }

    // Runs before the chat line and the hotkeys see the key, so the key being bound does nothing else.
    public override void _Input(InputEvent inputEvent)
    {
        if (capturing == null || inputEvent is not InputEventKey key)
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        if (!key.Pressed)
        {
            return;
        }

        if (key.Keycode == Key.Escape)
        {
            CaptureCancelled?.Invoke();
        }
        else if (key.Keycode == Key.Delete)
        {
            CaptureUnbound?.Invoke();
        }
        else if (BindingActions.ToChord(key) is { } chord)
        {
            KeyCaptured?.Invoke(chord);
        }
    }

    public void ShowOptions(OptionsWindow window, ClientSettings? settings, CharacterSettings characterSettings)
    {
        capturing = window.Capturing;
        Visible = window.IsOpen;
        if (!Visible || settings == null)
        {
            return;
        }

        TabButton(window.Tab).SetPressedNoSignal(true);
        GetNode<TabContainer>("%Pages").CurrentTab = (int)window.Tab;
        foreach (var row in GeneralOptions.Rows)
        {
            OptionBox(row.Option).SetPressedNoSignal(settings[row.Option]);
            OptionRow(row.Option).Visible = GeneralOptions.IsShown(row, option => settings[option]);
        }

        CursorScale.SetValueNoSignal(settings.CursorScale);
        ShowCursorScale(settings.CursorScale);
        SoundEnabled.SetPressedNoSignal(settings.SoundEnabled);
        Volume.SetValueNoSignal(settings.SoundVolume);
        ShowValue(Volume, settings.SoundVolume.ToString(CultureInfo.InvariantCulture));
        ScreenGamma.SetValueNoSignal(settings.ScreenGamma);
        ShowValue(ScreenGamma, Gamma.Text(settings.ScreenGamma));
        WorldGamma.SetValueNoSignal(settings.WorldGamma);
        ShowValue(WorldGamma, Gamma.Text(settings.WorldGamma));
        foreach (var row in DisplaySwaps.Rows)
        {
            SwapBox(row.Swap).SetPressedNoSignal(settings.SwapsOn.Contains(row.Swap));
        }

        foreach (var row in WindowBackgrounds.Rows)
        {
            var opacity = settings.BackgroundOpacity[row.Window];
            BackgroundSlider(row.Window).SetValueNoSignal(opacity);
            ShowValue(BackgroundSlider(row.Window), WindowBackgrounds.Text(opacity));
        }

        var bindings = BindingActions.Current();
        ShowKeybinds(KeybindsTab.Sections(characterSettings), bindings, settings.EnterToTalk, window);
        ShowTypingWarning(BindingRules.TypingHotkeysWarning(bindings, settings.EnterToTalk));
        ShowFooter(window);
    }

    // A ScrollContainer makes its own bars, so they can't be given a theme variation in the scene.
    private void StyleScrollBars()
    {
        foreach (var scroll in GetNode("%Pages").FindChildren("*", nameof(ScrollContainer), true, false))
        {
            ((ScrollContainer)scroll).GetVScrollBar().ThemeTypeVariation = ScrollBarVariation;
        }
    }

    private void ApplySprites()
    {
        if (Engine.IsEditorHint() || Theme == null || sprites == null)
        {
            return;
        }

        ThemeSprites.Apply(Theme, sprites);
        if (sprites.GetTexture(CloseSprite) is { } closeSheet)
        {
            GetNode<TextureButton>("%CloseButton").TextureNormal =
                new AtlasTexture { Atlas = closeSheet, Region = CloseRegion };
        }
    }

    // The rows come from the Core table so their wording is kept in one place; they are never saved into the scene.
    private void AddOptionRows()
    {
        var rowScene = GD.Load<PackedScene>(OptionRowScenePath);
        foreach (var row in GeneralOptions.Rows)
        {
            var rowNode = CheckRow(rowScene, row.Option.ToString(), row.Label);
            rowNode.GetNode<Control>("Line/Indent").Visible = row.Under != null;
            if (row.Option == GeneralOption.EnterToTalk)
            {
                rowNode.GetNode<Label>("Line/Warning").Visible = true;
            }

            GetNode(row.GodotOnly ? "%GodotOptions" : "%ClientOptions").AddChild(rowNode);
        }

        foreach (var row in DisplaySwaps.Rows)
        {
            GetNode("%SwapOptions").AddChild(CheckRow(rowScene, row.Swap.ToString(), row.Label));
        }

        var sliderScene = GD.Load<PackedScene>(SliderRowScenePath);
        foreach (var row in WindowBackgrounds.Rows)
        {
            var rowNode = sliderScene.Instantiate<Control>();
            rowNode.Name = row.Window.ToString();
            rowNode.GetNode<Label>("Line/Text").Text = row.Label;
            if (!Engine.IsEditorHint())
            {
                ShowHoverBand(rowNode);
            }

            GetNode("%BackgroundOptions").AddChild(rowNode);
        }
    }

    private static Control CheckRow(PackedScene rowScene, string name, string label)
    {
        var rowNode = rowScene.Instantiate<Control>();
        rowNode.Name = name;
        rowNode.GetNode<Label>("Line/Text").Text = label;
        var box = rowNode.GetNode<CheckBox>("Line/Box");
        rowNode.GuiInput += inputEvent =>
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            {
                box.ButtonPressed = !box.ButtonPressed;
            }
        };
        if (!Engine.IsEditorHint())
        {
            ShowHoverBand(rowNode);
        }

        return rowNode;
    }

    private void ShowKeybinds(
        IReadOnlyList<KeybindSection> sections,
        IReadOnlyDictionary<string, KeyChord?> bindings,
        bool enterToTalk,
        OptionsWindow window)
    {
        var rows = sections.SelectMany(section => section.Rows).ToList();
        if (!rows.SequenceEqual(shownKeybindRows))
        {
            AddKeybindSections(sections);
            shownKeybindRows = rows;
        }

        foreach (var row in rows)
        {
            var waiting = row.Id == window.Capturing;
            var keyButton = keyButtons[row.Id];
            keyButton.Text = waiting ? WaitingText : BindingRules.LongName(bindings[row.Id]);
            keyButton.TooltipText = waiting ? WaitingHint : "";
            var warning = BindingRules.TypingHotkeyWarning(bindings, row.Id, enterToTalk);
            keyWarnings[row.Id].Visible = warning != null;
            keyWarnings[row.Id].TooltipText = warning ?? "";
        }
    }

    // Spell rows come and go with the hotbar, so the sections are rebuilt whenever the list of rows changes.
    private void AddKeybindSections(IReadOnlyList<KeybindSection> sections)
    {
        foreach (var oldSection in KeybindSections.GetChildren())
        {
            KeybindSections.RemoveChild(oldSection);
            oldSection.QueueFree();
        }

        keyButtons.Clear();
        keyWarnings.Clear();
        var sectionScene = GD.Load<PackedScene>(KeybindSectionScenePath);
        var rowScene = GD.Load<PackedScene>(KeybindRowScenePath);
        foreach (var section in sections)
        {
            var sectionNode = sectionScene.Instantiate<Control>();
            sectionNode.GetNode<Label>("Heading").Text = section.Heading;
            foreach (var row in section.Rows)
            {
                var rowNode = rowScene.Instantiate<Control>();
                rowNode.Name = row.Id;
                rowNode.GetNode<Label>("Line/Text").Text = row.Label;
                var keyButton = rowNode.GetNode<Button>("Line/Key");
                keyButton.Pressed += () => CaptureChosen?.Invoke(row.Id);
                keyButtons[row.Id] = keyButton;
                keyWarnings[row.Id] = rowNode.GetNode<Label>("Line/Warning");
                rowNode.GetNode<Button>("Line/Reset").Pressed += () => BindingResetChosen?.Invoke(row.Id);
                if (!Engine.IsEditorHint())
                {
                    ShowHoverBand(rowNode);
                }

                sectionNode.GetNode("Rows").AddChild(rowNode);
            }

            KeybindSections.AddChild(sectionNode);
        }
    }

    private void ShowFooter(OptionsWindow window)
    {
        var onKeybinds = window.Tab == OptionsTab.Keybinds;
        Reset.Text = window.ConfirmingReset
            ? ConfirmResetText
            : string.Format(CultureInfo.InvariantCulture, ResetText, window.Tab);
        GetNode<Label>("%FooterHint").Text = !onKeybinds ? ""
            : window.Capturing != null ? WaitingHint
            : KeybindsHint;
    }

    private void ShowTypingWarning(string? warning)
    {
        TypingWarning.Text = warning != null ? WarningMark : "";
        TypingWarning.TooltipText = warning ?? "";
    }

    // Moving onto a button inside the row also counts as leaving it, so the band only clears once the pointer is out.
    private static void ShowHoverBand(Control row)
    {
        row.MouseEntered += () => row.ThemeTypeVariation = HoveredRowVariation;
        row.MouseExited += () =>
        {
            if (!row.GetGlobalRect().HasPoint(row.GetGlobalMousePosition()))
            {
                row.ThemeTypeVariation = RowVariation;
            }
        };
    }

    private Control OptionRow(GeneralOption option)
    {
        var row = GeneralOptions.Rows.First(row => row.Option == option);
        return GetNode(row.GodotOnly ? "%GodotOptions" : "%ClientOptions").GetNode<Control>(option.ToString());
    }

    private CheckBox OptionBox(GeneralOption option) => OptionRow(option).GetNode<CheckBox>("Line/Box");

    private CheckBox SwapBox(DisplaySwap swap) => GetNode("%SwapOptions").GetNode<CheckBox>($"{swap}/Line/Box");

    private HSlider BackgroundSlider(WindowBackground window) =>
        GetNode("%BackgroundOptions").GetNode<HSlider>($"{window}/Line/Slider");

    private Button TabButton(OptionsTab tab) => GetNode("%Tabs").GetNode<Button>($"{tab}Tab");

    private void ShowCursorScale(float scale) =>
        ShowValue(CursorScale, scale.ToString("0.0", CultureInfo.InvariantCulture) + "x");

    private static void ShowValue(HSlider slider, string text) =>
        slider.GetParent().GetNode<Label>("Value").Text = text;
}
