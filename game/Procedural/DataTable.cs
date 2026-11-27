using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;

namespace TheLastGate.Procedural;

// Each column takes its share of the width, the first holding the row's name and the rest right-aligned values, so
// every row's numbers sit under their headers. The group, header and body rows are inset alike, matching the body
// rows' padding.
[Tool]
public partial class DataTable : VBoxContainer
{
    private const int RowPadding = 2;
    private const int GroupRuleInset = 3;
    private const float LineMiddle = 0.5f;

    private static readonly TableColumn[] PreviewColumns =
    [
        new("Currency", 1.3f), new("Start", Group: "This session"), new("Net", Group: "This session"),
        new("Net", Group: "Per hour"),
    ];

    private static readonly TableRow[] PreviewRows =
    [
        new(
        [
            new TableCell("Gold"), new TableCell("52,000"), new TableCell("+6,600", CellTone.Positive),
            new TableCell("+4,400", CellTone.Positive),
        ]),
        new(
        [
            new TableCell("Stronghold"), new TableCell("340"), new TableCell("-35", CellTone.Negative),
            new TableCell("-23", CellTone.Negative),
        ]),
        new(
        [
            new TableCell("Contract"), new TableCell("12"), new TableCell("0", CellTone.Zero),
            new TableCell("0", CellTone.Zero),
        ]),
    ];

    private readonly List<SettingsRow> rows = [];
    private IReadOnlyList<TableColumn> columns = [];

    // For the style guide: fills the table with sample rows.
    [Export] public bool Preview { get; set; }

    public override void _Ready()
    {
        if (Preview)
        {
            SetColumns(PreviewColumns);
            ShowRows(PreviewRows);
        }
    }

    public void SetColumns(IReadOnlyList<TableColumn> shownColumns)
    {
        columns = shownColumns;
        foreach (var child in GetChildren())
        {
            child.Free();
        }

        rows.Clear();
        AddThemeConstantOverride("separation", 1);
        if (columns.Any(column => column.Group != null))
        {
            AddChild(GroupRow());
        }

        AddChild(HeaderRow());
        AddChild(Rule("HeaderRule", 0));
        var body = new VBoxContainer { Name = "Body", MouseFilter = MouseFilterEnum.Ignore };
        body.AddThemeConstantOverride("separation", SettingsRow.RowGap);
        AddChild(body);
    }

    public void ShowRows(IReadOnlyList<TableRow> shownRows)
    {
        var body = GetNode("Body");
        while (rows.Count < shownRows.Count)
        {
            var row = BodyRow(rows.Count > 0);
            rows.Add(row);
            body.AddChild(row);
        }

        for (var index = 0; index < rows.Count; index++)
        {
            rows[index].Visible = index < shownRows.Count;
            if (index < shownRows.Count)
            {
                Fill(rows[index], shownRows[index]);
            }
        }
    }

    private static void Fill(SettingsRow row, TableRow shown)
    {
        var cells = row.GetNode("Cells").GetChildren().OfType<HudLabel>().ToList();
        for (var column = 0; column < cells.Count && column < shown.Cells.Count; column++)
        {
            cells[column].Text = shown.Cells[column].Text;
            cells[column].Colour = Colour(shown.Cells[column].Tone);
        }
    }

    private static TextColour Colour(CellTone tone) => tone switch
    {
        CellTone.Normal => TextColour.Yellow,
        CellTone.Dim => TextColour.Grey,
        CellTone.Positive => TextColour.Green,
        CellTone.Negative => TextColour.Red,
        CellTone.Zero => TextColour.Dim,
        _ => throw new UnreachableException(),
    };

    private Control GroupRow()
    {
        var (row, groups) = InsetRow("Groups");
        foreach (var (group, width) in GroupRuns())
        {
            var cell = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = width,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            cell.AddThemeConstantOverride("separation", 1);
            if (group != null)
            {
                cell.AddChild(
                    new HudLabel
                    {
                        Text = group,
                        Colour = TextColour.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    });
                cell.AddChild(Rule("Rule", GroupRuleInset));
            }

            groups.AddChild(cell);
        }

        return row;
    }

    private IEnumerable<(string? Group, float Width)> GroupRuns()
    {
        var runs = new List<(string? Group, float Width)>();
        foreach (var column in columns)
        {
            if (runs.Count > 0 && runs[^1].Group == column.Group)
            {
                runs[^1] = (column.Group, runs[^1].Width + column.Width);
            }
            else
            {
                runs.Add((column.Group, column.Width));
            }
        }

        return runs;
    }

    private Control HeaderRow()
    {
        var (row, headers) = InsetRow("Headers");
        for (var column = 0; column < columns.Count; column++)
        {
            var header = CellLabel(column == 0, columns[column].Width);
            header.Text = columns[column].Header;
            header.Colour = TextColour.White;
            headers.AddChild(header);
        }

        return row;
    }

    private SettingsRow BodyRow(bool ruled)
    {
        var row = new SettingsRow { Padding = RowPadding, Fill = ProceduralFill.None };
        var cells = new HBoxContainer { Name = "Cells", MouseFilter = MouseFilterEnum.Ignore };
        cells.AddThemeConstantOverride("separation", 0);
        for (var column = 0; column < columns.Count; column++)
        {
            cells.AddChild(CellLabel(column == 0, columns[column].Width));
        }

        row.AddChild(cells);
        row.RuleAbove = ruled;
        return row;
    }

    // Padded like the body rows, so their columns round to the same widths.
    private static (Control Row, HBoxContainer Cells) InsetRow(string name)
    {
        var row = new ProceduralPanel { Name = name, Padding = RowPadding, Fill = ProceduralFill.None };
        var cells = new HBoxContainer { Name = "Cells", MouseFilter = MouseFilterEnum.Ignore };
        cells.AddThemeConstantOverride("separation", 0);
        row.AddChild(cells);
        return (row, cells);
    }

    private static Control Rule(string name, int inset)
    {
        var holder = new Control
        {
            Name = name,
            CustomMinimumSize = new Vector2(0, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var line = new ProceduralLine { Profile = "G", Width = 1 };
        holder.AddChild(line);
        holder.Resized += () => line.Points =
        [
            new Vector2(inset, LineMiddle),
            new Vector2(Mathf.Max(holder.Size.X - inset, inset), LineMiddle),
        ];
        return holder;
    }

    // A label that trims instead of growing keeps every row's columns at the same widths.
    private static HudLabel CellLabel(bool isName, float width) => new()
    {
        HorizontalAlignment = isName ? HorizontalAlignment.Left : HorizontalAlignment.Right,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsStretchRatio = width,
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
