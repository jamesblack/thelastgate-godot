using System;
using System.Collections.Generic;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Hud;
using TheLastGate.World;

namespace TheLastGate.Procedural;

// Expects a Page panel as its child, with one child of Page per title; it adds a tab per title. The tabs sit in a row
// over the page's top ring. Tree order does the layering: the closed tabs come before the page, so its top ring runs
// unbroken over their bottoms, and the open tab comes after it, so it opens into the page.
[Tool]
public partial class ProceduralTabs : Container
{
    private const string TabScenePath = "res://Procedural/tab.tscn";
    private const int PageRing = 3;
    private const int TabGap = 1;

    private readonly List<ProceduralPanel> tabs = [];
    private int open = -1;

    [Export] public string[] Titles { get; set; } = [];

    private Control Page => GetNode<Control>("Page");

    public event Action<int>? TabChosen;

    public override void _Ready()
    {
        var tabScene = GD.Load<PackedScene>(TabScenePath);
        for (var index = 0; index < Titles.Length; index++)
        {
            var tab = tabScene.Instantiate<ProceduralPanel>();
            tab.Name = $"Tab{index}";
            tab.GetNode<Label>("Inside/Label").Text = Titles[index];
            tabs.Add(tab);
            AddChild(tab);
            if (tab.GetNode("Click") is ClickArea click)
            {
                var tabIndex = index;
                click.ShadeChanged += shade =>
                {
                    if (tabIndex != open)
                    {
                        tab.Modulate = shade;
                    }
                };
                click.Released += button =>
                {
                    if (button == MapButton.Left && tabIndex != open)
                    {
                        TabChosen?.Invoke(tabIndex);
                    }
                };
            }
        }

        var chosen = Math.Max(open, 0);
        open = -1;
        Select(chosen);
    }

    public void Select(int index)
    {
        if (index == open)
        {
            return;
        }

        open = index;
        MoveChild(Page, -1);
        var closed = 0;
        for (var tab = 0; tab < tabs.Count; tab++)
        {
            ShowTab(tabs[tab], tab == index, tab == 0);
            MoveChild(tabs[tab], tab == index ? -1 : closed++);
        }

        for (var page = 0; page < Page.GetChildCount(); page++)
        {
            Page.GetChild<CanvasItem>(page).Visible = page == index;
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        var page = Page.GetCombinedMinimumSize();
        var row = 0f;
        foreach (var tab in tabs)
        {
            row += tab.GetCombinedMinimumSize().X + TabGap;
        }

        return new Vector2(Mathf.Max(page.X, row - TabGap), TabHeight() - PageRing + page.Y);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
        {
            return;
        }

        var height = TabHeight();
        var left = 0f;
        foreach (var tab in tabs)
        {
            var width = tab.GetCombinedMinimumSize().X;
            FitChildInRect(tab, new Rect2(left, 0, width, height));
            left += width + TabGap;
        }

        var top = Mathf.Max(height - PageRing, 0);
        FitChildInRect(Page, new Rect2(0, top, Size.X, Size.Y - top));
    }

    private float TabHeight()
    {
        var height = 0f;
        foreach (var tab in tabs)
        {
            height = Mathf.Max(height, tab.GetCombinedMinimumSize().Y);
        }

        return height;
    }

    // The open tab's sides turn into the page's top ring at its bottom corners, except the first tab's left side,
    // which runs straight down the page's own left edge.
    private static void ShowTab(ProceduralPanel tab, bool isOpen, bool isFirst)
    {
        tab.Fill = isOpen ? ProceduralFill.Red : ProceduralFill.Blue;
        tab.GetNode<HudLabel>("Inside/Label").Colour = isOpen ? TextColour.Yellow : TextColour.Grey;
        tab.GetNode<CanvasItem>("Click/JoinLeft").Visible = isOpen && !isFirst;
        tab.GetNode<CanvasItem>("Click/JoinRight").Visible = isOpen;
        if (isOpen)
        {
            tab.Modulate = Colors.White;
        }
    }
}
