using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hotkeys;
using TheLastGate.Launcher;
using TheLastGate.Net;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Config;

public sealed class ClientSettings
{
    private const int MaxSoundVolume = 10;
    private const float DefaultCursorScale = 1f;
    private const float SmallestCursorScale = 0.5f;
    private const float LargestCursorScale = 8f;

    private const string FilePath = "user://settings.cfg";
    private const string DisplaySection = "display";
    private const string AudioSection = "audio";
    private const string LauncherSection = "launcher";
    private const string ServerSection = "server";
    private const string WorldSection = "world";
    private const string KeybindsSection = "keybinds";
    private const string GeneralSection = "general";
    private const string WindowBackgroundsSection = "window_backgrounds";
    private const string AccessibilitySection = "accessibility";

    private readonly ConfigFile file;

    private ClientSettings(ConfigFile file)
    {
        this.file = file;
        Windowed = file.GetValue(DisplaySection, "windowed", true).AsBool();
        Shadows = file.GetValue(DisplaySection, "shadows", true).AsBool();
        DarkMode = file.GetValue(DisplaySection, "dark_mode", false).AsBool();
        SoundEnabled = file.GetValue(AudioSection, "sound_enabled", true).AsBool();
        SoundVolume = Math.Clamp(
            file.GetValue(AudioSection, "sound_volume", MaxSoundVolume).AsInt32(),
            0,
            MaxSoundVolume);

        CharacterHistory = new CharacterHistory(
            file.HasSectionKey(LauncherSection, "character_files") ? SavedCharacters(file) : SdlClientCharacters());
        CharacterHistory.RemoveMissingFiles();

        // Never written by the client: a [server] section added by hand points it at another server.
        Server = new ServerAddress(
            file.GetValue(ServerSection, "host", ServerAddress.Live.Host).AsString(),
            file.GetValue(ServerSection, "proxy", ServerAddress.Live.Proxy).AsString(),
            file.GetValue(ServerSection, "port", ServerAddress.Live.Port).AsInt32());

        EscapeClosesWindowsFirst = file.GetValue(GeneralSection, "escape_closes_windows_first", true).AsBool();
        GiveMore = file.GetValue(GeneralSection, "give_more", false).AsBool();
        UseQueue = file.GetValue(GeneralSection, "use_queue", false).AsBool();
        EnterToTalk = file.GetValue(GeneralSection, "enter_to_talk", false).AsBool();
        ExpandedQuickStats = file.GetValue(GeneralSection, "expanded_quick_stats", true).AsBool();
        SkillPlanner = file.GetValue(GeneralSection, "skill_planner", false).AsBool();
        AdaptivePacing = file.GetValue(GeneralSection, "adaptive_tick_pacing", false).AsBool();
        ItemTooltips = file.GetValue(GeneralSection, "item_tooltips", true).AsBool();
        ItemTooltipsAlways = file.GetValue(GeneralSection, "item_tooltips_always", true).AsBool();
        PickTileDrawnUnderMouse = file.GetValue(WorldSection, "pick_tile_drawn_under_mouse", false).AsBool();
        GlideMovement = file.GetValue(WorldSection, "glide_movement", false).AsBool();
        StubbornUse = file.GetValue(GeneralSection, "stubborn_use", false).AsBool();
        LoadKeyBindings(file);
        CursorScale = Math.Clamp(
            file.GetValue(DisplaySection, "cursor_scale", DefaultCursorScale).AsSingle(),
            SmallestCursorScale,
            LargestCursorScale);
        ScreenGamma = Gamma.Clamp(file.GetValue(DisplaySection, "screen_gamma", Gamma.Normal).AsSingle());
        WorldGamma = Gamma.Clamp(file.GetValue(DisplaySection, "world_gamma", Gamma.Normal).AsSingle());
        FontId = file.GetValue(DisplaySection, "font", FontChoice.ClassicId).AsString();
        foreach (var row in AccessibilityOptions.Rows.Where(row =>
            file.GetValue(AccessibilitySection, row.SettingKey, false).AsBool()))
        {
            AccessibilityOn.Add(row.Option);
        }

        foreach (var row in WindowBackgrounds.Rows)
        {
            BackgroundOpacity[row.Window] = WindowBackgrounds.Clamp(
                file.GetValue(WindowBackgroundsSection, row.SettingKey, WindowBackgrounds.Solid).AsInt32());
        }
    }

    public bool Windowed { get; set; }
    public bool Shadows { get; set; }
    public bool DarkMode { get; set; }
    public bool SoundEnabled { get; set; }
    public int SoundVolume { get; set; }
    public CharacterHistory CharacterHistory { get; }
    public ServerAddress Server { get; }
    public bool EscapeClosesWindowsFirst { get; set; }
    public bool GiveMore { get; set; }
    public bool UseQueue { get; set; }
    public bool EnterToTalk { get; set; }
    public bool ExpandedQuickStats { get; set; }
    public bool SkillPlanner { get; set; }
    public bool AdaptivePacing { get; set; }
    public bool ItemTooltips { get; set; }
    public bool ItemTooltipsAlways { get; set; }
    public bool PickTileDrawnUnderMouse { get; set; }
    public bool GlideMovement { get; set; }
    public bool StubbornUse { get; set; }
    public float CursorScale { get; set; }
    public float ScreenGamma { get; set; }
    public float WorldGamma { get; set; }

    public string FontId { get; set; }
    public HashSet<AccessibilityOption> AccessibilityOn { get; } = [];
    public Dictionary<WindowBackground, int> BackgroundOpacity { get; } = [];

    public bool this[GeneralOption option]
    {
        get => option switch
        {
            GeneralOption.EscapeClosesWindowsFirst => EscapeClosesWindowsFirst,
            GeneralOption.GiveMore => GiveMore,
            GeneralOption.UseQueue => UseQueue,
            GeneralOption.EnterToTalk => EnterToTalk,
            GeneralOption.ExpandedQuickStats => ExpandedQuickStats,
            GeneralOption.SkillPlanner => SkillPlanner,
            GeneralOption.AdaptivePacing => AdaptivePacing,
            GeneralOption.ItemTooltips => ItemTooltips,
            GeneralOption.ItemTooltipsAlways => ItemTooltipsAlways,
            GeneralOption.GlideMovement => GlideMovement,
            GeneralOption.PickTileDrawnUnderMouse => PickTileDrawnUnderMouse,
            GeneralOption.StubbornUse => StubbornUse,
            _ => throw new UnreachableException(),
        };
        set
        {
            switch (option)
            {
                case GeneralOption.EscapeClosesWindowsFirst:
                    EscapeClosesWindowsFirst = value;
                    break;
                case GeneralOption.GiveMore:
                    GiveMore = value;
                    break;
                case GeneralOption.UseQueue:
                    UseQueue = value;
                    break;
                case GeneralOption.EnterToTalk:
                    EnterToTalk = value;
                    break;
                case GeneralOption.ExpandedQuickStats:
                    ExpandedQuickStats = value;
                    break;
                case GeneralOption.SkillPlanner:
                    SkillPlanner = value;
                    break;
                case GeneralOption.AdaptivePacing:
                    AdaptivePacing = value;
                    break;
                case GeneralOption.ItemTooltips:
                    ItemTooltips = value;
                    break;
                case GeneralOption.ItemTooltipsAlways:
                    ItemTooltipsAlways = value;
                    break;
                case GeneralOption.GlideMovement:
                    GlideMovement = value;
                    break;
                case GeneralOption.PickTileDrawnUnderMouse:
                    PickTileDrawnUnderMouse = value;
                    break;
                case GeneralOption.StubbornUse:
                    StubbornUse = value;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private void ResetRows(Func<OptionRow, bool> inTab)
    {
        foreach (var row in GeneralOptions.Rows.Where(inTab))
        {
            this[row.Option] = row.Default;
        }
    }

    public void Reset(OptionsTab tab)
    {
        switch (tab)
        {
            case OptionsTab.General:
                ResetRows(row => row.Section != OptionSection.Experimental);
                break;
            case OptionsTab.Audio:
                SoundEnabled = true;
                SoundVolume = MaxSoundVolume;
                break;
            case OptionsTab.Keybinds:
                BindingActions.ResetAll();
                break;
            case OptionsTab.Experimental:
                ResetRows(row => row.Section == OptionSection.Experimental);
                break;
            case OptionsTab.Display:
                CursorScale = DefaultCursorScale;
                ScreenGamma = Gamma.Normal;
                WorldGamma = Gamma.Normal;
                FontId = FontChoice.ClassicId;
                AccessibilityOn.Clear();
                foreach (var row in WindowBackgrounds.Rows)
                {
                    BackgroundOpacity[row.Window] = WindowBackgrounds.Solid;
                }

                break;
            default:
                throw new UnreachableException();
        }
    }

    public static ClientSettings Load()
    {
        var file = new ConfigFile();
        file.Load(FilePath);
        return new ClientSettings(file);
    }

    public Error Save()
    {
        file.SetValue(DisplaySection, "windowed", Windowed);
        file.SetValue(DisplaySection, "shadows", Shadows);
        file.SetValue(DisplaySection, "dark_mode", DarkMode);
        file.SetValue(AudioSection, "sound_enabled", SoundEnabled);
        file.SetValue(AudioSection, "sound_volume", SoundVolume);
        file.SetValue(DisplaySection, "cursor_scale", CursorScale);
        file.SetValue(DisplaySection, "screen_gamma", ScreenGamma);
        file.SetValue(DisplaySection, "world_gamma", WorldGamma);
        file.SetValue(DisplaySection, "font", FontId);
        foreach (var row in AccessibilityOptions.Rows)
        {
            file.SetValue(AccessibilitySection, row.SettingKey, AccessibilityOn.Contains(row.Option));
        }

        foreach (var row in WindowBackgrounds.Rows)
        {
            file.SetValue(WindowBackgroundsSection, row.SettingKey, BackgroundOpacity[row.Window]);
        }

        file.SetValue(GeneralSection, "escape_closes_windows_first", EscapeClosesWindowsFirst);
        file.SetValue(GeneralSection, "give_more", GiveMore);
        file.SetValue(GeneralSection, "use_queue", UseQueue);
        file.SetValue(GeneralSection, "enter_to_talk", EnterToTalk);
        file.SetValue(GeneralSection, "expanded_quick_stats", ExpandedQuickStats);
        file.SetValue(GeneralSection, "skill_planner", SkillPlanner);
        file.SetValue(GeneralSection, "adaptive_tick_pacing", AdaptivePacing);
        file.SetValue(GeneralSection, "item_tooltips", ItemTooltips);
        file.SetValue(GeneralSection, "item_tooltips_always", ItemTooltipsAlways);
        file.SetValue(WorldSection, "pick_tile_drawn_under_mouse", PickTileDrawnUnderMouse);
        file.SetValue(WorldSection, "glide_movement", GlideMovement);
        file.SetValue(GeneralSection, "stubborn_use", StubbornUse);
        file.SetValue(
            LauncherSection,
            "character_names",
            CharacterHistory.Entries.Select(entry => entry.Name).ToArray());
        file.SetValue(
            LauncherSection,
            "character_files",
            CharacterHistory.Entries.Select(entry => entry.FilePath).ToArray());
        // Only bindings that differ from the defaults are kept, so one put back to its default, or one this client no
        // longer has such as the old gamma key, comes out of the file.
        var savedBindings = BindingRules.Saved(BindingActions.Defaults(), BindingActions.Current());
        if (file.HasSection(KeybindsSection))
        {
            foreach (var oldId in file.GetSectionKeys(KeybindsSection).Where(id => !savedBindings.ContainsKey(id)))
            {
                file.EraseSectionKey(KeybindsSection, oldId);
            }
        }

        foreach (var (id, keys) in savedBindings)
        {
            file.SetValue(KeybindsSection, id, keys);
        }

        return file.Save(FilePath);
    }

    // The defaults are read before the saved keys go into the InputMap, while it still holds only the project's.
    private static void LoadKeyBindings(ConfigFile file)
    {
        var defaults = BindingActions.Defaults();
        var loaded = BindingRules.Load(defaults, SavedBindings(file));
        BindingActions.Apply(loaded.Select(binding => new BindingChange(binding.Key, binding.Value)));
    }

    private static Dictionary<string, string> SavedBindings(ConfigFile file) =>
        file.HasSection(KeybindsSection)
            ? file.GetSectionKeys(KeybindsSection)
                .ToDictionary(id => id, id => file.GetValue(KeybindsSection, id, "").AsString())
            : [];

    private static IEnumerable<PreviousCharacter> SavedCharacters(ConfigFile file)
    {
        var names = file.GetValue(LauncherSection, "character_names", Array.Empty<string>()).AsStringArray();
        var filePaths = file.GetValue(LauncherSection, "character_files", Array.Empty<string>()).AsStringArray();
        return filePaths.Select((filePath, entryIndex) => new PreviousCharacter(
            entryIndex < names.Length ? names[entryIndex] : "",
            filePath));
    }

    private static IEnumerable<PreviousCharacter> SdlClientCharacters()
    {
        var listPath = SdlClientCharacterList.FilePath();
        if (!File.Exists(listPath))
        {
            return [];
        }

        try
        {
            return SdlClientCharacterList.Parse(File.ReadAllBytes(listPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Couldn't read the SDL client's character list: {exception.Message}");
            return [];
        }
    }
}
