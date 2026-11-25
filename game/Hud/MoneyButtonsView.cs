using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TheLastGate.Hud;

public partial class MoneyButtonsView : Control
{
    public IEnumerable<MoneyButton> Buttons => GetNode("%Buttons").GetChildren().OfType<MoneyButton>();

    public ClickArea TrashCan => GetNode<ClickArea>("%TrashCan");
}
