using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Config;

namespace TheLastGate.Tools;

public partial class SpriteViewer : Control
{
    private const long CacheBudgetBytes = 64L * 1024 * 1024;
    private const int GridColumns = 7;
    private const int GridRows = 5;
    private const int CellsPerPage = GridColumns * GridRows;
    private const int DetailPanelWidth = 380;
    private const int Spacing = 8;

    private static readonly Vector2 CellSize = new(122, 128);
    private static readonly Color BackgroundColor = new(0.12f, 0.12f, 0.14f);
    private static readonly Color SpriteBackdropColor = new(0.22f, 0.3f, 0.34f);

    private readonly List<Button> cellButtons = [];
    private readonly int[] cellSprites = new int[CellsPerPage];
    private int[] allSprites = [];
    private int[] browseList = [];
    private TextureRect detailTexture = null!;
    private Label detailTitle = null!;
    private SpriteLoader? loader;
    private SpinBox numberInput = null!;
    private Label pageLabel = null!;
    private int pageStart;
    private int[] presentSprites = [];

    public override void _Ready()
    {
        AddBackground();

        var dataPath = DataPaths.Root();
        try
        {
            loader = new SpriteLoader(dataPath, CacheBudgetBytes);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException
            or UnauthorizedAccessException)
        {
            ShowStartupError(
                $"Couldn't open the sprite archives in \"{dataPath}\".\n{exception.Message}\n\n"
                + "Check the data folder is there, and run git lfs pull.");
            return;
        }

        allSprites = Enumerable.Range(0, SpriteNumbers.MaxSprite).ToArray();
        presentSprites = allSprites.Where(loader.HasFrame).ToArray();
        browseList = presentSprites;

        BuildInterface();
        ShowPage(0);
    }

    public override void _ExitTree() => loader?.Dispose();

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("ui_page_down") || inputEvent.IsActionPressed("ui_right"))
        {
            ShowPage(pageStart + CellsPerPage);
        }
        else if (inputEvent.IsActionPressed("ui_page_up") || inputEvent.IsActionPressed("ui_left"))
        {
            ShowPage(pageStart - CellsPerPage);
        }
    }

    private void AddBackground()
    {
        var background = new ColorRect { Color = BackgroundColor };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);
    }

    private void ShowStartupError(string message)
    {
        GD.PushError(message);
        var errorLabel = new Label { Text = message, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        errorLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(errorLabel);
    }

    private void BuildInterface()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "top", "right", "bottom" })
        {
            margin.AddThemeConstantOverride($"margin_{side}", Spacing);
        }

        AddChild(margin);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", Spacing);
        margin.AddChild(columns);

        var browser = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddChild(browser);
        browser.AddChild(BuildToolbar());
        browser.AddChild(BuildGrid());

        columns.AddChild(BuildDetailPanel());
    }

    private HBoxContainer BuildToolbar()
    {
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", Spacing);

        toolbar.AddChild(new Label { Text = "Sprite" });
        numberInput = new SpinBox { MinValue = 0, MaxValue = SpriteNumbers.MaxSprite - 1, Step = 1 };
        numberInput.GetLineEdit().TextSubmitted += _ => JumpToNumber();
        toolbar.AddChild(numberInput);

        var goButton = new Button { Text = "Go" };
        goButton.Pressed += JumpToNumber;
        toolbar.AddChild(goButton);

        var previousButton = new Button { Text = "< Prev" };
        previousButton.Pressed += () => ShowPage(pageStart - CellsPerPage);
        toolbar.AddChild(previousButton);

        var nextButton = new Button { Text = "Next >" };
        nextButton.Pressed += () => ShowPage(pageStart + CellsPerPage);
        toolbar.AddChild(nextButton);

        var onlyExistingToggle = new CheckBox { Text = "Only existing", ButtonPressed = true };
        onlyExistingToggle.Toggled += OnOnlyExistingToggled;
        toolbar.AddChild(onlyExistingToggle);

        pageLabel = new Label();
        toolbar.AddChild(pageLabel);
        return toolbar;
    }

    private GridContainer BuildGrid()
    {
        var grid = new GridContainer { Columns = GridColumns };
        grid.AddThemeConstantOverride("h_separation", Spacing / 2);
        grid.AddThemeConstantOverride("v_separation", Spacing / 2);

        for (var cellIndex = 0; cellIndex < CellsPerPage; cellIndex++)
        {
            var button = new Button
            {
                CustomMinimumSize = CellSize,
                ExpandIcon = true,
                IconAlignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Top,
                ClipText = true,
            };
            var cell = cellIndex;
            button.Pressed += () => SelectSprite(cellSprites[cell]);
            grid.AddChild(button);
            cellButtons.Add(button);
        }

        return grid;
    }

    private VBoxContainer BuildDetailPanel()
    {
        var panel = new VBoxContainer { CustomMinimumSize = new Vector2(DetailPanelWidth, 0) };
        panel.AddThemeConstantOverride("separation", Spacing);

        detailTitle = new Label
        {
            Text = "Click a sprite to see it at 1:1",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        panel.AddChild(detailTitle);

        var backdrop = new ColorRect
        {
            Color = SpriteBackdropColor,
            CustomMinimumSize = new Vector2(DetailPanelWidth, DetailPanelWidth),
            ClipContents = true,
        };
        detailTexture = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
        };
        detailTexture.SetAnchorsPreset(LayoutPreset.FullRect);
        backdrop.AddChild(detailTexture);
        panel.AddChild(backdrop);
        return panel;
    }

    private void OnOnlyExistingToggled(bool onlyExisting)
    {
        var firstVisibleSprite =
            browseList.Length > 0 ? browseList[Math.Clamp(pageStart, 0, browseList.Length - 1)] : 0;
        browseList = onlyExisting ? presentSprites : allSprites;
        ShowPage(FirstPositionAtOrAfter(firstVisibleSprite));
    }

    private void JumpToNumber()
    {
        var spriteNumber = (int)numberInput.Value;
        ShowPage(FirstPositionAtOrAfter(spriteNumber));
        SelectSprite(spriteNumber);
    }

    private int FirstPositionAtOrAfter(int spriteNumber)
    {
        var position = Array.BinarySearch(browseList, spriteNumber);
        return position >= 0 ? position : ~position;
    }

    private void ShowPage(int requestedStart)
    {
        if (loader == null || browseList.Length == 0)
        {
            return;
        }

        pageStart = Math.Clamp(requestedStart, 0, Math.Max(0, browseList.Length - CellsPerPage));
        for (var cellIndex = 0; cellIndex < CellsPerPage; cellIndex++)
        {
            var position = pageStart + cellIndex;
            var button = cellButtons[cellIndex];
            button.Visible = position < browseList.Length;
            if (!button.Visible)
            {
                continue;
            }

            var spriteNumber = browseList[position];
            cellSprites[cellIndex] = spriteNumber;
            button.Icon = loader.GetTexture(spriteNumber);
            button.Text = spriteNumber.ToString();
        }

        var pageEnd = Math.Min(pageStart + CellsPerPage, browseList.Length);
        pageLabel.Text = $"{pageStart + 1}-{pageEnd} of {browseList.Length}";
    }

    private void SelectSprite(int spriteNumber)
    {
        if (loader == null)
        {
            return;
        }

        var texture = loader.GetTexture(spriteNumber);
        detailTexture.Texture = texture;

        var source = loader.FindSource(spriteNumber);
        var sizeText = texture == null ? "not loaded" : $"{texture.GetWidth()}x{texture.GetHeight()}";
        var sourceText = source == SpriteSource.None ? "missing" : source.ToString();
        detailTitle.Text = $"Sprite {spriteNumber}   {sizeText}   {sourceText}";
    }
}
