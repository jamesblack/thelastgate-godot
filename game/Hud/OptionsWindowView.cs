using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Characters;
using TheLastGate.Config;
using TheLastGate.Hotkeys;
using TheLastGate.Procedural;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Hud;

// The Options window: its pages of settings rows, the key bindings, and the Reset button in its footer.
[Tool]
public partial class OptionsWindowView : Control
{
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

    private readonly Dictionary<string, KeyButton> keyButtons = [];
    private readonly Dictionary<string, Label> keyWarnings = [];
    private readonly Dictionary<ProceduralSlider, Label> valueLabels = [];
    private string? capturing;
    private IReadOnlyList<KeybindRow> shownKeybindRows = [];

    private ProceduralCheckBox SoundEnabled => GetNode<ProceduralCheckBox>("%SoundRow/Line/Box");

    private ProceduralSlider Volume => GetNode<ProceduralSlider>("%VolumeRow/Line/Slider");

    private ProceduralSlider CursorScale => GetNode<ProceduralSlider>("%CursorRow/Line/Slider");

    private Node KeybindSections => GetNode("%Sections");

    private ProceduralSlider ScreenGamma => GetNode<ProceduralSlider>("%ScreenGammaRow/Line/Slider");

    private ProceduralSlider WorldGamma => GetNode<ProceduralSlider>("%WorldGammaRow/Line/Slider");

    private ProceduralButton Reset => GetNode<ProceduralButton>("%Reset");

    private ProceduralTabs Pages => GetNode<ProceduralTabs>("%Pages");

    private Label TypingWarning => OptionRow(GeneralOption.EnterToTalk).GetNode<Label>("Line/Warning");

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

    public event Action<AccessibilityOption, bool>? AccessibilityChanged;

    public event Action<WindowBackground, int>? BackgroundOpacityChanged;

    public event Action<string>? FontChosen;

    public override void _Ready()
    {
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

        Pages.TabChosen += index => TabChosen?.Invoke((OptionsTab)index);

        foreach (var row in GeneralOptions.Rows)
        {
            OptionBox(row.Option).Toggled += isOn => OptionChanged?.Invoke(row.Option, isOn);
        }

        GetNode<ClickArea>("%CloseButton").Released += _ => CloseChosen?.Invoke();
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
        foreach (var row in AccessibilityOptions.Rows)
        {
            AccessibilityBox(row.Option).Toggled += isOn => AccessibilityChanged?.Invoke(row.Option, isOn);
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

        Pages.Select((int)window.Tab);
        foreach (var row in GeneralOptions.Rows)
        {
            OptionBox(row.Option).Checked = settings[row.Option];
            OptionRow(row.Option).Visible = GeneralOptions.IsShown(row, option => settings[option]);
        }

        CursorScale.SetValueNoSignal(settings.CursorScale);
        ShowCursorScale(settings.CursorScale);
        SoundEnabled.Checked = settings.SoundEnabled;
        Volume.SetValueNoSignal(settings.SoundVolume);
        ShowValue(Volume, settings.SoundVolume.ToString(CultureInfo.InvariantCulture));
        ScreenGamma.SetValueNoSignal(settings.ScreenGamma);
        ShowValue(ScreenGamma, Gamma.Text(settings.ScreenGamma));
        WorldGamma.SetValueNoSignal(settings.WorldGamma);
        ShowValue(WorldGamma, Gamma.Text(settings.WorldGamma));
        foreach (var row in AccessibilityOptions.Rows)
        {
            AccessibilityBox(row.Option).Checked = settings.AccessibilityOn.Contains(row.Option);
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

    public void ShowFontChoices(IReadOnlyList<FontOption> choices, string chosenId)
    {
        var rows = GetNode("%FontOptions");
        if (rows.GetChildCount() == 0)
        {
            var rowScene = GD.Load<PackedScene>(OptionRowScenePath);
            foreach (var choice in choices)
            {
                var row = CheckRow(rowScene, choice.Id, choice.Name);
                var box = row.GetNode<ProceduralCheckBox>("Line/Box");
                box.Round = true;
                box.Toggled += isOn => ChooseFont(rows, box, isOn, choice.Id);
                rows.AddChild(row);
            }

            SeparateRows(rows);
        }

        var shownId = choices.Any(choice => choice.Id == chosenId) ? chosenId : FontChoice.ClassicId;
        foreach (var row in rows.GetChildren())
        {
            row.GetNode<ProceduralCheckBox>("Line/Box").Checked = row.Name == shownId;
        }
    }

    // Choosing the chosen font keeps it chosen; choosing another clears the rest.
    private void ChooseFont(Node rows, ProceduralCheckBox box, bool isOn, string id)
    {
        if (!isOn)
        {
            box.Checked = true;
            return;
        }

        foreach (var row in rows.GetChildren())
        {
            var other = row.GetNode<ProceduralCheckBox>("Line/Box");
            other.Checked = other == box;
        }

        FontChosen?.Invoke(id);
    }

    // A ScrollContainer makes its own bars, so they are styled here rather than in the scene.
    private void StyleScrollBars()
    {
        foreach (var scroll in GetNode("%Pages").FindChildren("*", nameof(ScrollContainer), true, false))
        {
            ProceduralScrollBar.StyleScrollContainer((ScrollContainer)scroll);
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

            SectionRows(row.Section).AddChild(rowNode);
        }

        foreach (var row in AccessibilityOptions.Rows)
        {
            GetNode("%AccessibilityOptions").AddChild(CheckRow(rowScene, row.Option.ToString(), row.Label));
        }

        var sliderScene = GD.Load<PackedScene>(SliderRowScenePath);
        foreach (var row in WindowBackgrounds.Rows)
        {
            var rowNode = sliderScene.Instantiate<Control>();
            rowNode.Name = row.Window.ToString();
            rowNode.GetNode<Label>("Line/Text").Text = row.Label;
            GetNode("%BackgroundOptions").AddChild(rowNode);
        }

        GetNode("%AccessibilityOptions").MoveChild(GetNode("%CursorRow"), -1);
        foreach (var list in (string[])
            [
                "%NetworkingOptions", "%ChatOptions", "%InformationalOptions", "%MiscellaneousOptions",
                "%ExperimentalOptions", "%ClassHelpersOptions", "%AccessibilityOptions", "%BackgroundOptions",
                "%SoundRows", "%GammaRows",
            ])
        {
            SeparateRows(GetNode(list));
        }
    }

    // Rows are ruled apart, except a linked setting, which hangs from the row above it on a tree line.
    private static void SeparateRows(Node list)
    {
        var rows = list.GetChildren().OfType<SettingsRow>().ToArray();
        for (var index = 0; index < rows.Length; index++)
        {
            var linked = IsLinked(rows[index]);
            rows[index].RuleAbove = index > 0 && !linked;
            rows[index].Tree = !linked ? SettingsTree.None
                : index + 1 < rows.Length && IsLinked(rows[index + 1]) ? SettingsTree.Branch
                : SettingsTree.Last;
        }
    }

    private static bool IsLinked(Node row) => row.GetNodeOrNull<CanvasItem>("Line/Indent")?.Visible == true;

    private static Control CheckRow(PackedScene rowScene, string name, string label)
    {
        var rowNode = rowScene.Instantiate<Control>();
        rowNode.Name = name;
        rowNode.GetNode<Label>("Line/Text").Text = label;
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
            keyButton.Caption = waiting ? WaitingText : BindingRules.LongName(bindings[row.Id]);
            keyButton.Waiting = waiting;
            keyButton.Tooltip = waiting ? WaitingHint : "";
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
            sectionNode.GetNode<RuledHeading>("Heading").Text = section.Heading;
            foreach (var row in section.Rows)
            {
                var rowNode = rowScene.Instantiate<Control>();
                rowNode.Name = row.Id;
                rowNode.GetNode<Label>("Line/Text").Text = row.Label;
                var keyButton = rowNode.GetNode<KeyButton>("Line/Key");
                keyButton.Pressed += () => CaptureChosen?.Invoke(row.Id);
                keyButtons[row.Id] = keyButton;
                keyWarnings[row.Id] = rowNode.GetNode<Label>("Line/Warning");
                rowNode.GetNode<ProceduralButton>("Line/Reset").Pressed += () => BindingResetChosen?.Invoke(row.Id);

                sectionNode.GetNode("Rows").AddChild(rowNode);
            }

            SeparateRows(sectionNode.GetNode("Rows"));

            KeybindSections.AddChild(sectionNode);
        }
    }

    private void ShowFooter(OptionsWindow window)
    {
        var onKeybinds = window.Tab == OptionsTab.Keybinds;
        Reset.Warning = window.ConfirmingReset;
        Reset.Caption = window.ConfirmingReset
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

    private Control OptionRow(GeneralOption option)
    {
        var row = GeneralOptions.Rows.First(row => row.Option == option);
        return SectionRows(row.Section).GetNode<Control>(option.ToString());
    }

    private Node SectionRows(OptionSection section) => section switch
    {
        OptionSection.Networking => GetNode("%NetworkingOptions"),
        OptionSection.Chat => GetNode("%ChatOptions"),
        OptionSection.Informational => GetNode("%InformationalOptions"),
        OptionSection.Miscellaneous => GetNode("%MiscellaneousOptions"),
        OptionSection.Experimental => GetNode("%ExperimentalOptions"),
        OptionSection.ClassHelpers => GetNode("%ClassHelpersOptions"),
        _ => throw new UnreachableException(),
    };

    private ProceduralCheckBox OptionBox(GeneralOption option) =>
        OptionRow(option).GetNode<ProceduralCheckBox>("Line/Box");

    private ProceduralCheckBox AccessibilityBox(AccessibilityOption option) =>
        GetNode("%AccessibilityOptions").GetNode<ProceduralCheckBox>($"{option}/Line/Box");

    private ProceduralSlider BackgroundSlider(WindowBackground window) =>
        GetNode("%BackgroundOptions").GetNode<ProceduralSlider>($"{window}/Line/Slider");

    private void ShowCursorScale(float scale) =>
        ShowValue(CursorScale, scale.ToString("0.0", CultureInfo.InvariantCulture) + "x");

    private void ShowValue(ProceduralSlider slider, string text) => ValueLabel(slider).Text = text;

    // Each slider shares a line with the label that shows its value.
    private Label ValueLabel(ProceduralSlider slider)
    {
        if (!valueLabels.TryGetValue(slider, out var label))
        {
            var line = FindChildren("Line", "", true, false).First(line => line.IsAncestorOf(slider));
            label = line.GetNode<Label>("Value");
            valueLabels[slider] = label;
        }

        return label;
    }
}
