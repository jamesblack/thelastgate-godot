using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Procedural;
using TheLastGate.Session;

namespace TheLastGate.Hud;

[Tool]
public partial class SessionStatsWindowView : CenterContainer
{
    private const string SessionGroup = "This session";
    private const string HourGroup = "Per hour";
    private const float NameWidth = 1.3f;

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

    private DataTable Table => GetNode<DataTable>("%Table");

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

        GetNode<ProceduralButton>("%Reset").Pressed += () => ResetPressed?.Invoke();
        GetNode<ClickArea>("%Close").Released += _ => ClosePressed?.Invoke();
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
        GetNode<Label>("%Elapsed").Text = SessionStatsText.Elapsed(elapsed);
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
        ]);
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
}
