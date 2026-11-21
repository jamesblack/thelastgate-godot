using TheLastGate.Session;
using TheLastGate.State;
using TheLastGate.Windows;
using TheLastGate.World;

namespace TheLastGate.Game;

public sealed class GameSession
{
    private readonly GameState state;

    public GameSession(GameState state)
    {
        this.state = state;
        TextWindows = new TextWindows(Pause);
        WaypointWindow = new WaypointWindow(Pause);
        state.MessageOfTheDay.LineAdded += MessageOfTheDayLines.Add;
        state.TickEnded += EndTheTick;
        state.TargetChanged += state.UseQueue.TargetChanged;
        state.HeldItemChanged += () => state.GiveMore.HeldItemChanged(state.Player);
        state.ShopOpened += _ => CloseTheWindowsAShopReplaces();
        state.ShopClosedByServer += Pause.Start;
        state.WaypointsOffered += WaypointWindow.Open;
        state.WindowRequested += TextWindows.Show;
    }

    public WindowPause Pause { get; } = new();

    public TextWindows TextWindows { get; }

    public WaypointWindow WaypointWindow { get; }

    public SkillTreeWindow SkillTreeWindow { get; } = new();

    public OptionsWindow OptionsWindow { get; } = new();

    public WorldMapWindow WorldMapWindow { get; } = new();

    public PlannerWindow PlannerWindow { get; } = new();

    public MessageOfTheDayLines MessageOfTheDayLines { get; } = new();

    public SessionCurrencies SessionCurrencies { get; } = new();

    public bool SessionStatsOpen { get; set; }

    public AutoWalk AutoWalk { get; } = new();

    public bool CloseShop()
    {
        if (!state.CloseShop())
        {
            return false;
        }

        Pause.Start();
        return true;
    }

    // Opening the tree quietly shuts the other windows: no pause, and the server isn't told the shop went away.
    public void ToggleSkillTree(MapKeys keys)
    {
        ShutWindowsBesideTheTree();
        SkillTreeWindow.Toggle(keys, state.Player.TreePoints, state.Player.OsirisTree);
    }

    // Escape closes a shop with the pause, as the close button does, but tells nobody about the text windows.
    public bool CloseWindowsByEscape()
    {
        var closedText = TextWindows.CloseByEscape();
        var closedWaypoints = WaypointWindow.Close();
        var closedTree = SkillTreeWindow.Close();
        var closedOptions = OptionsWindow.Close();
        var closedWorldMap = WorldMapWindow.CloseByEscape();
        var closedPlanner = PlannerWindow.Close();
        var closedShop = CloseShop();
        var closedSessionStats = SessionStatsOpen;
        SessionStatsOpen = false;
        return closedText || closedWaypoints || closedTree || closedOptions || closedWorldMap || closedPlanner
            || closedShop || closedSessionStats;
    }

    private void ShutWindows()
    {
        ShutWindowsBesideTheTree();
        SkillTreeWindow.Close();
    }

    private void ShutWindowsBesideTheTree()
    {
        TextWindows.CloseAll();
        WaypointWindow.Close();
        state.CloseShop();
    }

    private void CloseTheWindowsAShopReplaces()
    {
        TextWindows.CloseAll();
        WaypointWindow.Close();
        SkillTreeWindow.Close();
    }

    private void EndTheTick()
    {
        if (Pause.Active)
        {
            ShutWindows();
        }

        Pause.EndOfTick();
        AutoWalk.EndOfTick(state.Map);
    }
}
