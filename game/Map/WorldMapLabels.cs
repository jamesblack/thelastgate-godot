using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheLastGate.Notes;

namespace TheLastGate.Map;

// Where pins and text labels sit on the world map, in panel pixels. A pin is a diamond whose name goes in the first
// free spot beside it; a text label is its text centred on its tile on a dark plate.
public static class WorldMapLabels
{
    public const int PinRadius = 3;
    public const int PinMarkerExtent = PinRadius + 1;
    public const int PinSelectionRadius = 6;

    private const double NamesHiddenBelowPixelsPerTile = 2.68;
    private const float NameGap = 4;
    private const float TextPaddingX = 4;
    private const float TextPaddingY = 2;

    public static bool NamesHidden(WorldMapView view) => view.PixelsPerTile < NamesHiddenBelowPixelsPerTile;

    // Each shown pin's name, relative to the pin's centre. The names are laid out over the whole map at this zoom, not
    // the panel, so panning moves them with their pins rather than placing them again. Every marker and text plate is
    // reserved before any name is placed, so no name covers one, and the selected pin's name goes first so it gets the
    // best spot. A name with no room is left out; hovering the pin shows it instead. A note being edited is laid out
    // where the edit has put it.
    public static IReadOnlyDictionary<int, LabelRect> Layout(
        NoteSet notes,
        double pixelsPerTile,
        Func<string, (float Width, float Height)> measure,
        int selectedNoteId = 0,
        Note? edited = null)
    {
        var placed = new Dictionary<int, LabelRect>();
        if (pixelsPerTile < NamesHiddenBelowPixelsPerTile)
        {
            return placed;
        }

        var layout = new LabelLayout();
        var named = new List<(Note Pin, (float X, float Y) Centre)>();
        foreach (var category in notes.Categories)
        {
            if (!notes.IsShown(category))
            {
                continue;
            }

            var namesShown = notes.LabelsShown(category);
            foreach (var stored in category.Notes)
            {
                var note = edited?.Id == stored.Id ? edited : stored;
                var (mapX, mapY) = MapPixel(pixelsPerTile, note);
                switch (note.Kind)
                {
                    case NoteKind.Text:
                        layout.Reserve(Plate(mapX, mapY, measure(note.Label)).Plate);
                        break;
                    case NoteKind.Pin:
                        var centre = ((float)Math.Floor(mapX), (float)Math.Floor(mapY));
                        layout.Reserve(MarkerRect(centre));
                        if (namesShown && note.Label.Length > 0)
                        {
                            named.Add((note, centre));
                        }

                        break;
                    case NoteKind.Path:
                        break;
                    default:
                        throw new UnreachableException();
                }
            }
        }

        var selected = named.FindIndex(entry => entry.Pin.Id == selectedNoteId);
        if (selected >= 0)
        {
            PlaceName(named[selected]);
        }

        for (var index = 0; index < named.Count; index++)
        {
            if (index != selected)
            {
                PlaceName(named[index]);
            }
        }

        return placed;

        // The text's shadow sits one pixel right and below, so it is part of the name's footprint.
        void PlaceName((Note Pin, (float X, float Y) Centre) entry)
        {
            var (width, height) = measure(entry.Pin.Label);
            var spot = layout.PlaceBeside(
                entry.Centre.X,
                entry.Centre.Y,
                PinMarkerExtent + NameGap,
                MathF.Floor(width) + 1,
                MathF.Floor(height) + 1);
            if (spot is { } rect)
            {
                placed[entry.Pin.Id] = new LabelRect(
                    rect.Left - entry.Centre.X,
                    rect.Top - entry.Centre.Y,
                    rect.Right - entry.Centre.X,
                    rect.Bottom - entry.Centre.Y);
            }
        }
    }

    // The names of Layout, moved to where their pins are in the panel.
    public static IReadOnlyDictionary<int, LabelRect> Place(
        NoteSet notes,
        WorldMapView view,
        double panelWidth,
        double panelHeight,
        Func<string, (float Width, float Height)> measure,
        int selectedNoteId = 0,
        Note? edited = null)
    {
        var placed = new Dictionary<int, LabelRect>();
        foreach (var (id, name) in Layout(notes, view.PixelsPerTile, measure, selectedNoteId, edited))
        {
            var pin = edited?.Id == id ? edited : notes.FindNote(id)!;
            var (centreX, centreY) = PinCentre(view, panelWidth, panelHeight, pin);
            placed[id] = new LabelRect(
                centreX + name.Left,
                centreY + name.Top,
                centreX + name.Right,
                centreY + name.Bottom);
        }

        return placed;
    }

    public static bool PinOnPanel(
        WorldMapView view,
        double panelWidth,
        double panelHeight,
        Note pin,
        LabelRect? name)
    {
        var panel = new LabelRect(0, 0, (float)panelWidth, (float)panelHeight);
        var (centreX, centreY) = PinCentre(view, panelWidth, panelHeight, pin);
        var marker = new LabelRect(
            centreX - PinSelectionRadius,
            centreY - PinSelectionRadius,
            centreX + PinSelectionRadius + 1,
            centreY + PinSelectionRadius + 1);
        return marker.Overlaps(panel)
            || (name is { } offset
                && new LabelRect(
                    centreX + offset.Left,
                    centreY + offset.Top,
                    centreX + offset.Right,
                    centreY + offset.Bottom).Overlaps(panel));
    }

    public static (float X, float Y) PinCentre(WorldMapView view, double panelWidth, double panelHeight, Note pin)
    {
        var (panelX, panelY) = view.TileToPanel(panelWidth, panelHeight, pin.TileX + 0.5, pin.TileY + 0.5);
        return ((float)Math.Floor(panelX), (float)Math.Floor(panelY));
    }

    public static (LabelRect Plate, float TextX, float TextY) TextPlate(
        WorldMapView view,
        double panelWidth,
        double panelHeight,
        Note text,
        (float Width, float Height) textSize)
    {
        var (anchorX, anchorY) = view.TileToPanel(panelWidth, panelHeight, text.TileX + 0.5, text.TileY + 0.5);
        return Plate(anchorX, anchorY, textSize);
    }

    // The middle of a note's tile on the whole map at this zoom, laid out as the panel lays it out: across by tile Y,
    // down by tile X.
    private static (double X, double Y) MapPixel(double pixelsPerTile, Note note) =>
        ((note.TileY + 0.5) * pixelsPerTile, (note.TileX + 0.5) * pixelsPerTile);

    private static (LabelRect Plate, float TextX, float TextY) Plate(
        double anchorX,
        double anchorY,
        (float Width, float Height) textSize)
    {
        var textX = MathF.Floor((float)anchorX - textSize.Width / 2);
        var textY = MathF.Floor((float)anchorY - textSize.Height / 2);
        var plate = new LabelRect(
            textX - TextPaddingX,
            textY - TextPaddingY,
            textX + MathF.Floor(textSize.Width) + TextPaddingX,
            textY + MathF.Floor(textSize.Height) + TextPaddingY);
        return (plate, textX, textY);
    }

    // The outline reaches one pixel further right and down, where the centre pixel itself sits.
    private static LabelRect MarkerRect((float X, float Y) centre) =>
        new(
            centre.X - PinMarkerExtent,
            centre.Y - PinMarkerExtent,
            centre.X + PinMarkerExtent + 1,
            centre.Y + PinMarkerExtent + 1);
}
