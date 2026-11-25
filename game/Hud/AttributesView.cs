using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheLastGate.Hud;

public partial class AttributesView : Control
{
    public IEnumerable<StatRow> Rows => GetNode("%Rows").GetChildren().Cast<StatRow>();

    public StatRow Row(int row) => GetNode("%Rows").GetChild<StatRow>(row);
}
