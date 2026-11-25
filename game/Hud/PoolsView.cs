using System.Globalization;
using Godot;

namespace TheLastGate.Hud;

public partial class PoolsView : Control
{
    public void ShowPool(string pool, int current, int maximum)
    {
        GetNode<Label>("%" + pool).Text = current.ToString(CultureInfo.InvariantCulture);
        GetNode<Label>("%Max" + pool).Text = maximum.ToString(CultureInfo.InvariantCulture);
    }
}
