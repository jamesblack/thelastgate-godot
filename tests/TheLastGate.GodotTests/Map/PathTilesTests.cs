using GdUnit4;
using TheLastGate.Map;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class PathTilesTests
{
    [TestCase]
    public void Walk_Horizontal()
    {
        AssertThat(PathTiles.Walk(new PathSegment(5, 5, 8, 5)).ToArray())
            .ContainsExactly((5, 5), (6, 5), (7, 5), (8, 5));
    }

    [TestCase]
    public void Walk_Diagonal_Backwards()
    {
        AssertThat(PathTiles.Walk(new PathSegment(10, 10, 7, 7)).ToArray())
            .ContainsExactly((10, 10), (9, 9), (8, 8), (7, 7));
    }

    [TestCase]
    public void Walk_Shallow()
    {
        var tiles = PathTiles.Walk(new PathSegment(0, 0, 4, 2)).ToArray();

        AssertThat(tiles.Length).IsEqual(5);
        AssertThat(tiles[0]).IsEqual((0, 0));
        AssertThat(tiles[^1]).IsEqual((4, 2));
        foreach (var (previous, next) in tiles.Zip(tiles.Skip(1)))
        {
            AssertThat(next.X - previous.X).IsEqual(1);
            AssertThat(next.Y - previous.Y).IsBetween(0, 1);
        }
    }

    [TestCase]
    public void Walk_Steep_StepsAlongY()
    {
        var tiles = PathTiles.Walk(new PathSegment(3, 9, 1, 3)).ToArray();

        AssertThat(tiles.Length).IsEqual(7);
        AssertThat(tiles[0]).IsEqual((3, 9));
        AssertThat(tiles[^1]).IsEqual((1, 3));
        foreach (var (previous, next) in tiles.Zip(tiles.Skip(1)))
        {
            AssertThat(next.Y - previous.Y).IsEqual(-1);
            AssertThat(next.X - previous.X).IsBetween(-1, 0);
        }
    }

    [TestCase]
    public void Walk_SinglePoint_GivesOneTile()
    {
        AssertThat(PathTiles.Walk(new PathSegment(3, 3, 3, 3)).ToArray()).ContainsExactly((3, 3));
    }

    [TestCase]
    public void Colours_MapsEveryTileToItsPathColour()
    {
        var notes = new NoteSet();
        var categoryId = notes.AddCategory("Routes", 2);
        var noteId = notes.AddPath(categoryId, [new PathSegment(1, 1, 3, 1)], "Gate");
        var colour = NoteSet.Rgb(notes.FindCategory(categoryId)!, notes.FindNote(noteId)!);

        var colours = PathTiles.Colours(notes);

        AssertThat(colours.Count).IsEqual(3);
        AssertThat(colours[(1, 1)]).IsEqual(colour);
        AssertThat(colours[(2, 1)]).IsEqual(colour);
        AssertThat(colours[(3, 1)]).IsEqual(colour);
    }

    [TestCase]
    public void Colours_LaterPathWinsWhereTheyCross()
    {
        var notes = new NoteSet();
        var categoryId = notes.AddCategory("Routes", 2);
        var firstId = notes.AddPath(categoryId, [new PathSegment(0, 2, 4, 2)], "Across");
        var secondId = notes.AddPath(categoryId, [new PathSegment(2, 0, 2, 4)], "Down");
        notes.SetNoteColour(firstId, 3);
        notes.SetNoteColour(secondId, 5);
        var category = notes.FindCategory(categoryId)!;

        var colours = PathTiles.Colours(notes);

        AssertThat(colours[(2, 2)]).IsEqual(NoteSet.Rgb(category, notes.FindNote(secondId)!));
        AssertThat(colours[(0, 2)]).IsEqual(NoteSet.Rgb(category, notes.FindNote(firstId)!));
    }

    [TestCase]
    public void HiddenParent_HidesChildPaths()
    {
        var notes = new NoteSet();
        var parentId = notes.AddCategory("Aston", 1);
        var childId = notes.AddCategory("Routes", 2, parentId);
        notes.AddPath(childId, [new PathSegment(1, 1, 3, 1)], "Gate");
        notes.FindCategory(parentId)!.Visible = false;

        AssertThat(PathTiles.Colours(notes).Count).IsEqual(0);
    }

    [TestCase]
    public void Colours_IgnoresPinsAndText()
    {
        var notes = new NoteSet();
        var categoryId = notes.AddCategory("Keys", 1);
        notes.AddPoint(categoryId, NoteKind.Pin, 4, 4, "Key");
        notes.AddPoint(categoryId, NoteKind.Text, 5, 5, "Beware");

        AssertThat(PathTiles.Colours(notes).Count).IsEqual(0);
    }
}
