using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Session;

namespace TheLastGate.Hud;

[Tool]
public partial class SessionStatsWindowView : CenterContainer, IHudContextUser
{
    private const string SessionGroup = "This session";
    private const string HourGroup = "Per hour";
    private const int CloseSprite = 18042;
    private const float NameWidth = 1.3f;

    private static readonly Rect2 CloseRegion = new(283, 0, 14, 14);
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(10);

    private static readonly TableColumn[] Columns =
    [
        new("Currency", NameWidth),
        new("Start", Group: SessionGroup),
        new("Earned", Group: SessionGroup),
        new("Spent", Group: SessionGroup),
        new("Net", Group: SessionGroup),
        new("Earned", Group: HourGroup),
        new("Net", Group: HourGroup),
    ];

    private static readonly CurrencyTally[] PreviewTallies =
    [
        new(Currency.Experience, 1200, 84000, 60000),
        new(Currency.Gold, 52000, 9100, 2500),
        new(Currency.BlackStronghold, 340, 25, 0),
        new(Currency.Osiris, 12, 0, 0),
        new(Currency.Tokens, 0, 3, 0),
    ];

    private TimeSpan? shownElapsed;
    public SessionStatsWindowView() => AddToGroup(HudContext.Group);

    private DataTable Table => GetNode<DataTable>("Window/Frame/Layout/Body/Table");

    public void ShowContext(HudContext context)
    {
        if (context.Sprites?.GetTexture(CloseSprite) is { } sheet)
        {
            GetNode<TextureButton>("Window/Close").TextureNormal =
                new AtlasTexture { Atlas = sheet, Region = CloseRegion };
        }
    }

    public event Action? ResetPressed;

    public event Action? ClosePressed;

    public override void _Ready()
    {
        Table.SetColumns(Columns);
        if (Engine.IsEditorHint())
        {
            ShowTallies(PreviewTallies, TimeSpan.FromMinutes(90));
            return;
        }

        GetNode<Button>("Window/Frame/Layout/Footer/Row/Reset").Pressed += () => ResetPressed?.Invoke();
        GetNode<TextureButton>("Window/Close").Pressed += () => ClosePressed?.Invoke();
    }

    public void ShowWindow(bool shown, IEnumerable<CurrencyTally> tallies, TimeSpan elapsed)
    {
        Visible = shown;
        if (!shown)
        {
            shownElapsed = null;
            return;
        }

        var wasReset = elapsed < shownElapsed;
        if (shownElapsed == null || wasReset || elapsed - shownElapsed >= UpdateInterval)
        {
            shownElapsed = elapsed;
            ShowTallies(tallies, elapsed);
        }
    }

    private void ShowTallies(IEnumerable<CurrencyTally> tallies, TimeSpan elapsed)
    {
        GetNode<Label>("Window/Frame/Layout/Footer/Row/Elapsed").Text = SessionStatsText.Elapsed(elapsed);
        Table.ShowRows(tallies.Select(tally => Row(tally, elapsed)).ToArray());
    }

    private static TableRow Row(CurrencyTally tally, TimeSpan elapsed)
    {
        var currency = tally.Currency;
        var netPerHour = (long)tally.NetPerHour(elapsed);
        return new TableRow(
            [
                new TableCell(CurrencyName(currency)),
                new TableCell(SessionStatsText.Amount(currency, tally.Start)),
                new TableCell(SessionStatsText.Amount(currency, tally.Earned)),
                new TableCell(SessionStatsText.Amount(currency, tally.Spent)),
                new TableCell(SessionStatsText.Change(currency, tally.Net), ChangeTone(tally.Net)),
                new TableCell(SessionStatsText.Amount(currency, (long)tally.EarnedPerHour(elapsed)), CellTone.Dim),
                new TableCell(SessionStatsText.Change(currency, netPerHour), ChangeTone(netPerHour)),
            ],
            Swatch(currency));
    }

    private static CellTone ChangeTone(long change) => Math.Sign(change) switch
    {
        > 0 => CellTone.Positive,
        < 0 => CellTone.Negative,
        _ => CellTone.Zero,
    };

    private static string CurrencyName(Currency currency) => currency switch
    {
        Currency.Experience => "Experience",
        Currency.Gold => "Gold",
        Currency.BlackStronghold => "Stronghold",
        Currency.Osiris => "Contract",
        Currency.Tokens => "Casino",
        _ => throw new UnreachableException(),
    };

    // The swatches are the HUD font sheets' colours.
    private static Color Swatch(Currency currency) => currency switch
    {
        Currency.Experience => Color.Color8(63, 189, 226),
        Currency.Gold => Color.Color8(243, 219, 106),
        Currency.BlackStronghold => Color.Color8(255, 118, 111),
        Currency.Osiris => Color.Color8(188, 113, 249),
        Currency.Tokens => Color.Color8(70, 226, 73),
        _ => throw new UnreachableException(),
    };
}
