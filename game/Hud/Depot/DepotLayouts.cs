using System.Collections.Generic;
using System.Linq;
using TheLastGate.Characters;

namespace TheLastGate.Hud.Depot;

public sealed class DepotLayouts
{
    private readonly Dictionary<Season, DepotLayout> bySeason = [];

    public bool Changed => bySeason.Values.Any(layout => layout.Changed);

    public DepotLayout For(Season season)
    {
        if (!bySeason.TryGetValue(season, out var layout))
        {
            layout = new DepotLayout();
            bySeason[season] = layout;
        }

        return layout;
    }

    public void Set(Season season, DepotLayout layout) => bySeason[season] = layout;

    public void ClearChanged()
    {
        foreach (var layout in bySeason.Values)
        {
            layout.Changed = false;
        }
    }
}
