using System.Collections.Generic;
using System.Linq;
using Godot;
using TheLastGate.Stats;

namespace TheLastGate.Hud;

public partial class StatModeButtonsView : Control
{
    public IEnumerable<StatModeButton> Buttons => GetNode("%Buttons").GetChildren().Cast<StatModeButton>();

    public void ShowMarked(StatPanel panel)
    {
        foreach (var button in Buttons)
        {
            button.ShowMarked(panel);
        }
    }
}
