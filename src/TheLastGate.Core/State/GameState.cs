using System;
using TheLastGate.Buffs;
using TheLastGate.Items;
using TheLastGate.Net;

namespace TheLastGate.State;

public sealed class GameState
{
    public const int ServerTickCycle = 200;

    private readonly SpectateWatch spectateWatch = new();

    public GameState()
    {
        ItemLooks = new ItemLooks(Chat);
        BuffWatch = new BuffWatch(Player);
        Chat.LineAdded += line => WatchSpectating(() => spectateWatch.LogLine(line.Text, ViewCentreCharacter));
    }

    public PlayerState Player { get; } = new();
    public WorldMap Map { get; } = new();
    public TextLog Chat { get; } = new();
    public TextLog MessageOfTheDay { get; } = new();
    public Terminology Terminology { get; } = new();
    public CharacterNames Names { get; } = new();
    public ItemLooks ItemLooks { get; }
    public BuffWatch BuffWatch { get; }
    public ShopRefresh ShopRefresh { get; } = new();
    public NameRequests NameRequests { get; } = new();
    public TickReports TickReports { get; } = new();
    public CharacterLook? Look { get; internal set; }
    public CharacterLook Shop { get; internal set; } = new();
    public ShopKind? OpenShop { get; internal set; }
    public int ServerTick { get; internal set; }
    public int ServerLoad { get; internal set; }

    internal CharacterLook PendingLook { get; } = new();

    public bool Spectating => spectateWatch.Active;

    private int ViewCentreCharacter => Map.Tiles[WorldMap.PlayerTile].CharacterNumber;

    public event Action<CharacterLook>? LookShown;
    public event Action<ShopKind>? ShopOpened;
    public event Action? ShopClosedByServer;
    public event Action<SoundRequest>? SoundRequested;
    public event Action<ServerExit>? ExitRequested;
    public event Action<UniqueId>? UniqueIdReceived;
    public event Action<WindowRequest>? WindowRequested;
    public event Action<MinimapBox>? MinimapBoxCleared;
    public event Action<string>? UnusualDataSeen;
    public event Action? TickEnded;
    public event Action<PlayerTarget, PlayerTarget>? TargetChanged;
    public event Action? HeldItemChanged;
    public event Action? WaypointsOffered;
    public event Action? SpectatingStarted;
    public event Action? SpectatingStopped;

    public bool CloseShop()
    {
        if (OpenShop == null)
        {
            return false;
        }

        OpenShop = null;
        return true;
    }

    internal void CloseShopForTheServer()
    {
        if (CloseShop())
        {
            ShopClosedByServer?.Invoke();
        }
    }

    internal void ReportLookShown(CharacterLook look) => LookShown?.Invoke(look);

    internal void ReportShopOpened(ShopKind kind) => ShopOpened?.Invoke(kind);

    internal void ReportSoundRequested(SoundRequest sound) => SoundRequested?.Invoke(sound);

    internal void ReportExitRequested(ServerExit exit) => ExitRequested?.Invoke(exit);

    internal void ReportUniqueIdReceived(UniqueId uniqueId) => UniqueIdReceived?.Invoke(uniqueId);

    internal void ReportWindowRequested(WindowRequest window) => WindowRequested?.Invoke(window);

    internal void ReportMinimapBoxCleared(MinimapBox box) => MinimapBoxCleared?.Invoke(box);

    internal void ReportUnusualData(string description) => UnusualDataSeen?.Invoke(description);

    internal void ReportTickEnded() => TickEnded?.Invoke();

    internal void ReportTargetChanged(PlayerTarget previous, PlayerTarget now) => TargetChanged?.Invoke(previous, now);

    internal void ReportHeldItemChanged() => HeldItemChanged?.Invoke();

    internal void ReportWaypointsOffered() => WaypointsOffered?.Invoke();

    internal void WatchViewCentre() => WatchSpectating(() => spectateWatch.ViewCentre(ViewCentreCharacter));

    private void WatchSpectating(Action note)
    {
        var wasSpectating = Spectating;
        note();
        if (Spectating != wasSpectating)
        {
            (Spectating ? SpectatingStarted : SpectatingStopped)?.Invoke();
        }
    }
}
