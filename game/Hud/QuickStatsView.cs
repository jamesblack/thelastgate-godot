using System.Globalization;
using Godot;

namespace TheLastGate.Hud;

public partial class QuickStatsView : Control
{
    public void ShowExpanded(bool expanded)
    {
        GetNode<CanvasItem>("%HitRow").Visible = expanded;
        GetNode<CanvasItem>("%ParryRow").Visible = expanded;
        GetNode<Control>("%Frame").ResetSize();
    }

    public void Show(int weapon, int armor, int hitScore, int parryScore, int experience, double rankProgress)
    {
        ShowNumber("%WeaponValue", weapon);
        ShowNumber("%ArmorValue", armor);
        ShowNumber("%HitScore", hitScore);
        ShowNumber("%ParryScore", parryScore);
        ShowNumber("%Experience", experience);
        GetNode<ProgressBar>("%ExperienceBar").Value = rankProgress;
    }

    private void ShowNumber(string name, int value) =>
        GetNode<Label>(name).Text = value.ToString(CultureInfo.InvariantCulture);
}
