using System.Globalization;
using Godot;
using TheLastGate.State;

namespace TheLastGate.Hud;

public partial class CurrenciesView : Control
{
    private const string StrongholdName = "Stronghold";
    private const string CasinoName = "Casino";
    private const string ContractName = "Contract";

    public void Show(PlayerState player)
    {
        GetNode<Label>("%Money").Text = Currencies.MoneyText(player.Gold);
        ShowPoints("Stronghold", StrongholdName, player.BlackStrongholdPoints);
        ShowPoints("Casino", CasinoName, player.Tokens);
        ShowPoints("Contract", ContractName, player.OsirisPoints);
    }

    private void ShowPoints(string row, string shownName, int points)
    {
        GetNode<Label>($"%{row}Name").Text = Currencies.RowName(shownName, points);
        GetNode<Label>($"%{row}").Text = points.ToString(CultureInfo.InvariantCulture);
    }
}
