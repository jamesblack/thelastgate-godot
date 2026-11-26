using System.Collections.Generic;

namespace TheLastGate.Windows;

public sealed record WorldMapHelpPage(string Title, IReadOnlyList<string> Lines);

// The current client's help pages, word for word, except that the corner readout no longer shows a colour.
public static class WorldMapHelp
{
    public static IReadOnlyList<WorldMapHelpPage> Pages { get; } =
    [
        new(
            "Getting around",
            [
                "Open the map with the World Map hotkey. Bind it under Keybindings in the options.",
                "Scroll the mouse wheel to zoom in and out around the cursor.",
                "Hold the right or middle mouse button and drag to pan, or use the Pan tool and drag with the left button.",
                "Tick Follow player to keep the map centred on you as you walk.",
                "The bottom right corner shows the tile under the cursor.",
                "Drag the title bar to move the window and the bottom right corner to resize it. Escape closes it.",
            ]),
        new(
            "Categories",
            [
                "Everything you draw lives in a category, listed on the left.",
                "Click a category to make it current. New pins, labels and paths go into the current category.",
                "+ New makes a top-level category. Click the arrow beside a category to open or close it.",
                "Categories can hold subcategories up to 8 levels deep.",
            ]),
        new(
            "Category menu",
            [
                "Right-click a category to change it.",
                "Show on map and Show labels hide or show everything inside it, subcategories included.",
                "New subcategory, Rename and Colour do what they say.",
                "Send to moves it under another category. Move up and Move down change its place in the list.",
                "Export copies it and its subcategories as a share code.",
                "Delete category asks first. Empty or move its subcategories before deleting it.",
            ]),
        new(
            "Pins and labels",
            [
                "Pick a tool from the bottom bar. Select lets you click annotations to highlight them.",
                "Pin: click the map to drop a marker, then give it a name. Names show beside pins when zoomed in, "
                + "and on hover when zoomed out.",
                "Text: click the map to place a label. Text labels always need some text.",
                "Clicking an entry in the category list selects it and centres the map on it.",
            ]),
        new(
            "Drawing paths",
            [
                "Pick the Line tool, then drag from one point to another to draw each segment.",
                "A new segment snaps to the ends of the path you are drawing, so routes join up cleanly.",
                "Press Enter or switch tools to finish the path and give it a name.",
                "Press Escape to throw away a path you have not finished.",
                "Arrows along a path point the way it was drawn, from the hollow square to the solid one.",
            ]),
        new(
            "Changing annotations",
            [
                "Right-click a pin, label or path on the map or in the list.",
                "Colour gives it its own colour. Use category colour sets it back.",
                "Send to moves it to another category.",
                "Delete segment removes only the piece of path under the cursor.",
                "Delete removes the whole annotation after asking first.",
            ]),
        new(
            "Edit mode",
            [
                "Choose Edit from the right-click menu to change an annotation.",
                "Type a new name in the box on the bottom bar.",
                "Pins and labels: drag them to a new spot.",
                "Paths: drag to add segments, or click a segment to remove it.",
                "Press Save to keep the changes or Cancel to undo them.",
            ]),
        new(
            "Sharing and the minimap",
            [
                "Export puts a share code on the clipboard. Paste it to friends, on Discord or on the wiki.",
                "Import reads a code from the clipboard and previews what it holds before you add it.",
                "Right-click a category and choose Import here to add a code inside it.",
                "If a category with the same name is already there, choose Merge or add it under a new name.",
                "Merge fills in every subcategory and skips pins you already have with the same name and spot.",
                "Very large codes may not fit in one Discord message but still work elsewhere.",
                "Tick Show on minimap to draw your annotations on the minimap. Hover a pin there to see its name.",
            ]),
    ];
}
