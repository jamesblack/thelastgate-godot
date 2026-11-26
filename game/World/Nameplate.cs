using Godot;
using TheLastGate.Hud;

namespace TheLastGate.World;

// The name and health bar over a character. The scene's layout and colours start as the current client's and can be
// restyled in the editor, as long as the five named parts stay, and any font can be swapped in. Only the player's own
// bar has a reserved part, as the server sends other characters' health as a bare percent.
public partial class Nameplate : Node2D
{
    private Control bar = null!;
    private ColorRect barBackground = null!;
    private ColorRect barBorder = null!;
    private ColorRect barFill = null!;
    private ColorRect barReserved = null!;
    private float fullBarWidth;
    private Label nameLabel = null!;
    private Color shadowColour;

    private (string Text, NameplateFont Font, int HealthPercent, int ReservedPercent, bool ShowText, bool ShowBar)
        shown;

    public override void _Ready()
    {
        nameLabel = GetNode<Label>("%NameLabel");
        shadowColour = nameLabel.GetThemeColor("font_shadow_color");
        bar = GetNode<Control>("%HealthBar");
        barBorder = GetNode<ColorRect>("%BarBorder");
        barBackground = GetNode<ColorRect>("%BarBackground");
        barFill = GetNode<ColorRect>("%BarFill");
        barReserved = GetNode<ColorRect>("%BarReserved");
        fullBarWidth = barFill.Size.X;
    }

    public void Display(
        string text,
        NameplateFont font,
        int healthPercent,
        int reservedPercent,
        bool showText,
        bool showBar)
    {
        if (shown == (text, font, healthPercent, reservedPercent, showText, showBar))
        {
            return;
        }

        shown = (text, font, healthPercent, reservedPercent, showText, showBar);
        nameLabel.Visible = showText && text.Length > 0;
        if (nameLabel.Visible)
        {
            nameLabel.Text = text;
            nameLabel.AddThemeFontOverride("font", font.Font);
            nameLabel.AddThemeFontSizeOverride("font_size", font.Size);
            nameLabel.AddThemeColorOverride("font_color", font.Colour);

            // The current client's letters carry their own black outline in the font sheet.
            nameLabel.AddThemeColorOverride(
                "font_shadow_color",
                font.CentresLikeTheCurrentClient ? Colors.Transparent : shadowColour);
            var left = font.CentresLikeTheCurrentClient
                ? NameplateText.LeftOffset(text)
                : -Mathf.RoundToInt(font.Font.GetStringSize(text, fontSize: font.Size).X / 2);
            nameLabel.Position = new Vector2(left, nameLabel.Position.Y);
        }

        bar.Visible = showBar;
        barBorder.Visible = healthPercent != 0;
        barBackground.Visible = healthPercent != 0;
        barFill.Size = new Vector2((int)(fullBarWidth * (healthPercent / 100f)), barFill.Size.Y);
        var reservedWidth = ReservedBar.Width((int)fullBarWidth, reservedPercent);
        barReserved.Visible = healthPercent != 0 && reservedWidth > 0;
        barReserved.Position = new Vector2(fullBarWidth - reservedWidth, barReserved.Position.Y);
        barReserved.Size = new Vector2(reservedWidth, barReserved.Size.Y);
    }
}
